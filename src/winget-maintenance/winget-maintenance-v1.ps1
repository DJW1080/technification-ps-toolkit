<#
    Script Name     : winget-maintenance-v1.ps1
    Description     : Winget maintenance submenu for Technification Toolbox
    Author          : Dean John Weiniger
    Version         : 1.1
    Type            : PowerShell 7
    Date            : 2026-03-23
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path (Split-Path $PSScriptRoot -Parent) 'shared\menu-core.ps1')
. (Join-Path (Split-Path $PSScriptRoot -Parent) 'shared\logging-core.ps1')

$script:ModuleName = 'winget-maintenance'
$script:SessionLog = New-TechnificationLogFile -ModuleName $script:ModuleName -Prefix 'session'
Write-TechnificationLog -Path $script:SessionLog -Level 'INFO' -Message 'Winget Maintenance session started.'

function Write-Info($Message) { Write-Host "[INFO]  $Message" -ForegroundColor Cyan }
function Write-Good($Message) { Write-Host "[GOOD]  $Message" -ForegroundColor Green }
function Write-Bad($Message) { Write-Host "[FAIL]  $Message" -ForegroundColor Red }
function Write-Warn($Message) { Write-Host "[WARN]  $Message" -ForegroundColor Yellow }

function Write-ModuleLog {
    param(
        [Parameter(Mandatory)][string]$Level,
        [Parameter(Mandatory)][string]$Message
    )

    Write-TechnificationLog -Path $script:SessionLog -Level $Level -Message $Message
}

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Assert-Administrator {
    if (-not (Test-IsAdministrator)) {
        Write-Warn 'Some Winget maintenance actions work best when run as Administrator.'
        Write-ModuleLog -Level 'WARN' -Message 'Session started without elevation.'
    }
}

function Get-WingetCommand {
    $command = Get-Command -Name 'winget.exe' -ErrorAction SilentlyContinue
    if (-not $command) {
        $command = Get-Command -Name 'winget' -ErrorAction SilentlyContinue
    }

    return $command
}

function Test-WingetAvailable {
    return $null -ne (Get-WingetCommand)
}

function Assert-WingetAvailable {
    if (Test-WingetAvailable) {
        return $true
    }

    Write-Bad 'Winget is not available on this system. Install App Installer from Microsoft Store, then try again.'
    Write-ModuleLog -Level 'ERROR' -Message 'Winget executable was not found in PATH.'
    return $false
}

function ConvertFrom-WingetTableText {
    param(
        [Parameter(Mandatory)][AllowEmptyString()][string[]]$Lines
    )

    $headerIndex = -1
    for ($index = 0; $index -lt $Lines.Count; $index++) {
        $line = $Lines[$index]
        if ($line -match '\bName\b' -and $line -match '\bId\b') {
            $headerIndex = $index
            break
        }
    }

    if ($headerIndex -lt 0 -or ($headerIndex + 1) -ge $Lines.Count) {
        return @()
    }

    $headerLine = $Lines[$headerIndex]
    $dividerLine = $Lines[$headerIndex + 1]
    if ($dividerLine -notmatch '^-{3,}') {
        return @()
    }

    $columnStarts = [ordered]@{}
    foreach ($columnName in @('Name', 'Id', 'Version', 'Available', 'Source')) {
        $startIndex = $headerLine.IndexOf($columnName)
        if ($startIndex -ge 0) {
            $columnStarts[$columnName] = $startIndex
        }
    }

    if ($columnStarts.Count -lt 3 -or -not $columnStarts.Contains('Id')) {
        return @()
    }

    $orderedColumns = @($columnStarts.GetEnumerator() | Sort-Object Value)
    $rows = @()

    for ($index = $headerIndex + 2; $index -lt $Lines.Count; $index++) {
        $line = $Lines[$index]
        if ([string]::IsNullOrWhiteSpace($line)) {
            continue
        }
        if ($line -match '^[\\/\-\| ]+$') {
            continue
        }

        $row = [ordered]@{}
        for ($columnIndex = 0; $columnIndex -lt $orderedColumns.Count; $columnIndex++) {
            $column = $orderedColumns[$columnIndex]
            $startIndex = $column.Value
            $endIndex = if ($columnIndex -lt ($orderedColumns.Count - 1)) { $orderedColumns[$columnIndex + 1].Value } else { $line.Length }

            if ($startIndex -ge $line.Length) {
                $value = ''
            }
            else {
                $length = [Math]::Max([Math]::Min($endIndex, $line.Length) - $startIndex, 0)
                $value = $line.Substring($startIndex, $length).Trim()
            }

            $row[$column.Key] = $value
        }

        if (-not [string]::IsNullOrWhiteSpace($row.Name)) {
            $rows += [pscustomobject]$row
        }
    }

    return $rows
}

function Get-WingetReportText {
    param(
        [Parameter(Mandatory)][object[]]$Output
    )

    $lines = @($Output | ForEach-Object { $_.ToString() })
    $tableRows = @(ConvertFrom-WingetTableText -Lines $lines)
    if ($tableRows.Count -eq 0) {
        return (($lines -join [Environment]::NewLine).TrimEnd())
    }

    $propertyNames = @('Name', 'Version', 'Available', 'Source') | Where-Object { $tableRows[0].PSObject.Properties.Name -contains $_ }
    $displayRows = @($tableRows | Select-Object -Property $propertyNames)
    return (($displayRows | Format-Table -AutoSize | Out-String -Width 240).TrimEnd())
}

function Invoke-WingetCommand {
    param(
        [Parameter(Mandatory)][string]$Label,
        [Parameter(Mandatory)][string[]]$Arguments,
        [switch]$CaptureReport
    )

    if (-not (Assert-WingetAvailable)) {
        return $false
    }

    $wingetCommand = Get-WingetCommand
    $wingetExecutable = $wingetCommand.Definition
    $reportPath = $null
    if ($CaptureReport) {
        $reportPath = New-TechnificationReportFile -ModuleName $script:ModuleName -Prefix (($Label -replace '[^A-Za-z0-9]+', '-').Trim('-').ToLowerInvariant())
    }

    Write-Info "$Label..."
    Write-ModuleLog -Level 'INFO' -Message ("Starting '{0}' with arguments: {1}" -f $Label, ($Arguments -join ' '))
    Write-Host ("Command : {0} {1}" -f $wingetExecutable, ($Arguments -join ' ')) -ForegroundColor DarkGray

    try {
        $output = & $wingetExecutable @Arguments 2>&1
        if ($output) {
            $output | ForEach-Object { Write-Host $_ }
        }
        if (-not $output) {
            Write-Warn 'Winget returned no console output.'
        }

        $exitCode = $LASTEXITCODE
        Write-Host ("Exit Code : {0}" -f $exitCode) -ForegroundColor DarkGray
        if ($CaptureReport -and $reportPath) {
            $reportText = Get-WingetReportText -Output @($output)
            $reportText | Out-File -FilePath $reportPath -Encoding UTF8
            Write-Info "Report saved to $reportPath"
            Write-ModuleLog -Level 'INFO' -Message ("Report written to '{0}'." -f $reportPath)
        }

        if ($exitCode -ne 0) {
            Write-Bad "$Label failed with exit code $exitCode."
            Write-ModuleLog -Level 'ERROR' -Message ("'{0}' failed with exit code {1}." -f $Label, $exitCode)
            return $false
        }

        Write-Good "$Label completed."
        Write-ModuleLog -Level 'INFO' -Message ("'{0}' completed successfully." -f $Label)
        return $true
    }
    catch {
        Write-Bad "$Label failed: $($_.Exception.Message)"
        Write-ModuleLog -Level 'ERROR' -Message ("'{0}' failed: {1}" -f $Label, $_.Exception.Message)
        return $false
    }
}

function Show-WingetVersion {
    Invoke-WingetCommand -Label 'Checking Winget version' -Arguments @('--version')
}

function Update-WingetSources {
    Invoke-WingetCommand -Label 'Refreshing Winget sources' -Arguments @('source', 'update')
}

function List-AvailableWingetUpgrades {
    Invoke-WingetCommand -Label 'Listing available package upgrades' -Arguments @('upgrade', '--accept-source-agreements') -CaptureReport
}

function Start-WingetUpgradeAll {
    Invoke-WingetCommand -Label 'Upgrading all Winget packages' -Arguments @('upgrade', '--all', '--accept-source-agreements', '--accept-package-agreements', '--include-unknown')
}

function Reset-WingetSources {
    Write-Warn 'This will reset Winget sources to their defaults.'
    $confirmation = Read-Host 'Type YES to continue'
    if ($confirmation -cne 'YES') {
        Write-Warn 'Winget source reset cancelled.'
        Write-ModuleLog -Level 'WARN' -Message 'Winget source reset cancelled by user.'
        return
    }

    Invoke-WingetCommand -Label 'Resetting Winget sources' -Arguments @('source', 'reset', '--force')
}

function Export-WingetPackageInventory {
    Invoke-WingetCommand -Label 'Exporting Winget package inventory' -Arguments @('list', '--accept-source-agreements') -CaptureReport
}

function Start-RecommendedWingetMaintenance {
    Write-Info 'Running recommended Winget maintenance sequence...'
    Write-ModuleLog -Level 'INFO' -Message 'Recommended Winget maintenance sequence started.'

    $steps = @(
        @{ Label = 'Checking Winget version'; Arguments = @('--version'); CaptureReport = $false }
        @{ Label = 'Refreshing Winget sources'; Arguments = @('source', 'update'); CaptureReport = $false }
        @{ Label = 'Listing available package upgrades'; Arguments = @('upgrade', '--accept-source-agreements'); CaptureReport = $true }
        @{ Label = 'Upgrading all Winget packages'; Arguments = @('upgrade', '--all', '--accept-source-agreements', '--accept-package-agreements', '--include-unknown'); CaptureReport = $false }
        @{ Label = 'Exporting Winget package inventory'; Arguments = @('list', '--accept-source-agreements'); CaptureReport = $true }
    )

    foreach ($step in $steps) {
        $completed = Invoke-WingetCommand -Label $step.Label -Arguments $step.Arguments -CaptureReport:$step.CaptureReport
        if (-not $completed) {
            Write-Warn 'Recommended maintenance sequence stopped because a step failed.'
            Write-ModuleLog -Level 'WARN' -Message ("Recommended sequence stopped at step '{0}'." -f $step.Label)
            return
        }
    }

    Write-Good 'Recommended Winget maintenance sequence completed.'
    Write-ModuleLog -Level 'INFO' -Message 'Recommended Winget maintenance sequence completed.'
}

function Show-About {
    Write-Host ''
    Write-Host 'Winget Maintenance automates common package source refresh, upgrade, and inventory actions.' -ForegroundColor Cyan
    Write-Host 'Use the recommended sequence to refresh sources, review upgrades, install updates, and export the final package list.'
    Write-Host ("Logs    : {0}" -f (Get-TechnificationLogsPath)) -ForegroundColor DarkGray
    Write-Host ("Reports : {0}" -f (Get-TechnificationReportsPath)) -ForegroundColor DarkGray
    Write-Host ''
    Write-ModuleLog -Level 'INFO' -Message 'About page displayed.'
    Pause
}

function Show-WingetMaintenancePage {
    $wingetStatus = if (Test-WingetAvailable) { 'Available' } else { 'Not Installed' }
    $header = New-MenuHeader -Name 'Winget Maintenance' -Version '1.1' -InfoLines @(
        ("Winget  : {0}" -f $wingetStatus),
        ("Logs    : {0}" -f (Get-TechnificationLogsPath)),
        ("Reports : {0}" -f (Get-TechnificationReportsPath))
    )
    Show-MenuPage -Title 'Winget Maintenance Menu' -Items (& $script:GetWingetItems) -HeaderLines $header
}

$script:GetWingetItems = {
    $wingetReady = Test-WingetAvailable

    @(
        (New-MenuItem -Key '1' -Label 'Recommended Maintenance Sequence' -Description 'Version check, source refresh, upgrade scan, upgrade all, and inventory export' -Action { Start-RecommendedWingetMaintenance } -PauseAfter $true -IsEnabled $wingetReady -Status ($(if ($wingetReady) { '' } else { 'Unavailable' })))
        (New-MenuItem -Key '2' -Label 'Check Winget Version' -Action { Show-WingetVersion } -PauseAfter $true -IsEnabled $wingetReady -Status ($(if ($wingetReady) { '' } else { 'Unavailable' })))
        (New-MenuItem -Key '3' -Label 'Refresh Winget Sources' -Action { Update-WingetSources } -PauseAfter $true -IsEnabled $wingetReady -Status ($(if ($wingetReady) { '' } else { 'Unavailable' })))
        (New-MenuItem -Key '4' -Label 'List Available Upgrades' -Description 'Saves the upgrade list to the shared Reports folder' -Action { List-AvailableWingetUpgrades } -PauseAfter $true -IsEnabled $wingetReady -Status ($(if ($wingetReady) { '' } else { 'Unavailable' })))
        (New-MenuItem -Key '5' -Label 'Upgrade All Packages' -Description 'Uses agreement flags and includes packages with unknown versions' -Action { Start-WingetUpgradeAll } -PauseAfter $true -IsEnabled $wingetReady -Status ($(if ($wingetReady) { '' } else { 'Unavailable' })))
        (New-MenuItem -Key '6' -Label 'Export Installed Package Inventory' -Description 'Saves the current Winget package list to the shared Reports folder' -Action { Export-WingetPackageInventory } -PauseAfter $true -IsEnabled $wingetReady -Status ($(if ($wingetReady) { '' } else { 'Unavailable' })))
        (New-MenuItem -Key '7' -Label 'Reset Winget Sources' -Description 'Restores default source configuration after explicit confirmation' -Action { Reset-WingetSources } -PauseAfter $true -IsEnabled $wingetReady -Status ($(if ($wingetReady) { '' } else { 'Unavailable' })))
        (New-MenuItem -Key '9' -Label 'About This Menu' -Action { Show-About })
        (New-MenuItem -Key '0' -Label 'Return To Toolbox' -Action { Write-Host 'Returning to Technification Toolbox...' -ForegroundColor Blue; return '__EXIT_MENU__' } -Color 'Blue')
    )
}

Assert-Administrator

try {
    Invoke-MenuLoop -Render { Show-WingetMaintenancePage } -GetItems $script:GetWingetItems
}
finally {
    Write-ModuleLog -Level 'INFO' -Message 'Winget Maintenance session ended.'
}





