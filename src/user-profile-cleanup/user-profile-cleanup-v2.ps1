<#
    Script Name     : user-profile-cleanup-v2.ps1
    Description     : Deep current user profile cleanup tool for Technification Toolbox
    Author          : Dean John Weiniger
    Version         : 2.1
    Type            : PowerShell 7
    Date            : 2026-03-14
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path (Split-Path $PSScriptRoot -Parent) 'shared\menu-core.ps1')

function Write-Info($Message) { Write-Host "[INFO]  $Message" -ForegroundColor Cyan }
function Write-Good($Message) { Write-Host "[GOOD]  $Message" -ForegroundColor Green }
function Write-Bad($Message) { Write-Host "[FAIL]  $Message" -ForegroundColor Red }
function Write-Warn($Message) { Write-Host "[WARN]  $Message" -ForegroundColor Yellow }

function Format-Size {
    param([Parameter(Mandatory)][Int64]$Bytes)
    if ($Bytes -ge 1GB) { return ('{0:N2} GB' -f ($Bytes / 1GB)) }
    if ($Bytes -ge 1MB) { return ('{0:N2} MB' -f ($Bytes / 1MB)) }
    if ($Bytes -ge 1KB) { return ('{0:N2} KB' -f ($Bytes / 1KB)) }
    return ("{0} B" -f $Bytes)
}

$script:ExcludedPatterns = @('*.gitkeep')
$script:LastLockedItems = @()

function Get-CleanupTargets {
    $userProfile = [Environment]::GetFolderPath('UserProfile')
    @(
        [pscustomobject]@{ Name = 'Local Temp'; Category = 'Temp'; Path = Join-Path $userProfile 'AppData\Local\Temp'; Filter = '*'; Mode = 'Contents' }
        [pscustomobject]@{ Name = 'INetCache'; Category = 'Browser Cache'; Path = Join-Path $userProfile 'AppData\Local\Microsoft\Windows\INetCache'; Filter = '*'; Mode = 'Contents' }
        [pscustomobject]@{ Name = 'WebCache'; Category = 'Browser Cache'; Path = Join-Path $userProfile 'AppData\Local\Microsoft\Windows\WebCache'; Filter = '*'; Mode = 'Contents' }
        [pscustomobject]@{ Name = 'Explorer Thumbcache'; Category = 'Cache'; Path = Join-Path $userProfile 'AppData\Local\Microsoft\Windows\Explorer'; Filter = 'thumbcache_*'; Mode = 'FilteredFiles' }
        [pscustomobject]@{ Name = 'Crash Dumps'; Category = 'Crash Data'; Path = Join-Path $userProfile 'AppData\Local\CrashDumps'; Filter = '*'; Mode = 'Contents' }
        [pscustomobject]@{ Name = 'D3D Shader Cache'; Category = 'Cache'; Path = Join-Path $userProfile 'AppData\Local\D3DSCache'; Filter = '*'; Mode = 'Contents' }
        [pscustomobject]@{ Name = 'WER Reports'; Category = 'Crash Data'; Path = Join-Path $userProfile 'AppData\Local\Microsoft\Windows\WER'; Filter = '*'; Mode = 'Contents' }
        [pscustomobject]@{ Name = 'Packages LocalCache'; Category = 'App Cache'; Path = Join-Path $userProfile 'AppData\Local\Packages'; Filter = 'LocalCache'; Mode = 'PackageChildFolders' }
        [pscustomobject]@{ Name = 'Packages TempState'; Category = 'App Cache'; Path = Join-Path $userProfile 'AppData\Local\Packages'; Filter = 'TempState'; Mode = 'PackageChildFolders' }
        [pscustomobject]@{ Name = 'Downloads TMP Files'; Category = 'Temp'; Path = Join-Path $userProfile 'Downloads'; Filter = '*.tmp'; Mode = 'FilteredFiles' }
        [pscustomobject]@{ Name = 'Downloads LOG Files'; Category = 'Logs'; Path = Join-Path $userProfile 'Downloads'; Filter = '*.log'; Mode = 'FilteredFiles' }
    )
}

function Test-IsExcluded {
    param([Parameter(Mandatory)][string]$Name)
    foreach ($pattern in $script:ExcludedPatterns) {
        if ($Name -like $pattern) { return $true }
    }
    return $false
}

function Get-FilesForTarget {
    param([Parameter(Mandatory)]$Target)
    if (-not (Test-Path -LiteralPath $Target.Path)) { return @() }
    switch ($Target.Mode) {
        'FilteredFiles' {
            return @(Get-ChildItem -LiteralPath $Target.Path -Force -ErrorAction SilentlyContinue -File | Where-Object { $_.Name -like $Target.Filter -and -not (Test-IsExcluded -Name $_.Name) })
        }
        'PackageChildFolders' {
            $folders = @(Get-ChildItem -LiteralPath $Target.Path -Directory -Force -ErrorAction SilentlyContinue)
            $items = @()
            foreach ($folder in $folders) {
                $childPath = Join-Path $folder.FullName $Target.Filter
                if (Test-Path -LiteralPath $childPath) {
                    $items += @(Get-ChildItem -LiteralPath $childPath -Recurse -Force -ErrorAction SilentlyContinue -File | Where-Object { -not (Test-IsExcluded -Name $_.Name) })
                }
            }
            return $items
        }
        default {
            return @(Get-ChildItem -LiteralPath $Target.Path -Recurse -Force -ErrorAction SilentlyContinue -File | Where-Object { -not (Test-IsExcluded -Name $_.Name) })
        }
    }
}

function Get-TargetStats {
    param([Parameter(Mandatory)]$Target)
    $files = @(Get-FilesForTarget -Target $Target)
    $bytes = [int64]0
    foreach ($file in $files) {
        if ($null -ne $file.Length) { $bytes += [int64]$file.Length }
    }
    [pscustomobject]@{
        Name = $Target.Name
        Category = $Target.Category
        Path = $Target.Path
        Files = $files.Count
        Size = $bytes
    }
}

function Get-DeepScan {
    $results = @()
    foreach ($target in (Get-CleanupTargets)) {
        $results += Get-TargetStats -Target $target
    }
    return $results
}

function Show-DeepScanReport {
    $results = @(Get-DeepScan | Sort-Object Size -Descending)
    $totalFiles = 0
    $totalBytes = [int64]0
    Write-Host ''
    Write-Host '================ USER PROFILE DEEP SCAN =================' -ForegroundColor White
    foreach ($result in $results) {
        Write-Host ("{0} [{1}]" -f $result.Name, $result.Category) -ForegroundColor Cyan
        Write-Host ("  Path  : {0}" -f $result.Path) -ForegroundColor DarkGray
        Write-Host ("  Files : {0}" -f $result.Files) -ForegroundColor DarkGray
        Write-Host ("  Size  : {0}" -f (Format-Size -Bytes $result.Size)) -ForegroundColor DarkGray
        $totalFiles += $result.Files
        $totalBytes += $result.Size
    }
    Write-Host '---------------------------------------------------------' -ForegroundColor White
    Write-Host ("Total files : {0}" -f $totalFiles) -ForegroundColor Cyan
    Write-Host ("Total size  : {0}" -f (Format-Size -Bytes $totalBytes)) -ForegroundColor Cyan
    Write-Host ''
}

function Show-CategorySummary {
    $groups = @(Get-DeepScan) | Group-Object Category
    Write-Host ''
    Write-Host '================ CLEANUP CATEGORIES =================' -ForegroundColor White
    foreach ($group in $groups) {
        $files = 0
        $bytes = [int64]0
        foreach ($item in $group.Group) {
            $files += $item.Files
            $bytes += $item.Size
        }
        Write-Host $group.Name -ForegroundColor Cyan
        Write-Host ("  Files : {0}" -f $files) -ForegroundColor DarkGray
        Write-Host ("  Size  : {0}" -f (Format-Size -Bytes $bytes)) -ForegroundColor DarkGray
    }
    Write-Host ''
}

function Remove-CategoryItems {
    param([Parameter(Mandatory)][string]$Category)
    $targets = @(Get-CleanupTargets | Where-Object Category -eq $Category)
    if (-not $targets) { Write-Warn 'No targets matched that category.'; return }
    $before = foreach ($target in $targets) { Get-TargetStats -Target $target }
    $script:LastLockedItems = @()
    foreach ($target in $targets) {
        foreach ($file in @(Get-FilesForTarget -Target $target)) {
            try { Remove-Item -LiteralPath $file.FullName -Force -ErrorAction Stop }
            catch {
                $script:LastLockedItems += [pscustomobject]@{
                    Target = $target.Name
                    Path = $file.FullName
                    Reason = $_.Exception.Message
                }
            }
        }
    }
    $after = foreach ($target in $targets) { Get-TargetStats -Target $target }
    $totalFiles = 0
    $totalBytes = [int64]0
    Write-Host ''
    Write-Host ("================ {0} CLEANUP SUMMARY ================" -f $Category.ToUpper()) -ForegroundColor White
    for ($i = 0; $i -lt $before.Count; $i++) {
        $removedFiles = [Math]::Max(0, $before[$i].Files - $after[$i].Files)
        $recoveredBytes = [Math]::Max([int64]0, [int64]($before[$i].Size - $after[$i].Size))
        $totalFiles += $removedFiles
        $totalBytes += $recoveredBytes
        Write-Host $before[$i].Name -ForegroundColor Cyan
        Write-Host ("  Files removed   : {0}" -f $removedFiles) -ForegroundColor DarkGray
        Write-Host ("  Space recovered : {0}" -f (Format-Size -Bytes $recoveredBytes)) -ForegroundColor DarkGray
    }
    Write-Host '-----------------------------------------------------' -ForegroundColor White
    Write-Host ("Total files removed   : {0}" -f $totalFiles) -ForegroundColor Cyan
    Write-Host ("Total space recovered : {0}" -f (Format-Size -Bytes $totalBytes)) -ForegroundColor Cyan
    Write-Host ("Locked/skipped files  : {0}" -f $script:LastLockedItems.Count) -ForegroundColor Cyan
    Write-Host ''
}

function Show-LockedItems {
    Write-Host ''
    Write-Host '================ LOCKED OR SKIPPED FILES ==============' -ForegroundColor White
    if (-not $script:LastLockedItems -or $script:LastLockedItems.Count -eq 0) {
        Write-Good 'No locked or skipped files recorded in the last cleanup run.'
        Write-Host ''
        return
    }
    foreach ($item in $script:LastLockedItems | Select-Object -First 50) {
        Write-Host $item.Target -ForegroundColor Cyan
        Write-Host ("  Path   : {0}" -f $item.Path) -ForegroundColor DarkGray
        Write-Host ("  Reason : {0}" -f $item.Reason) -ForegroundColor DarkGray
    }
    if ($script:LastLockedItems.Count -gt 50) { Write-Warn 'Only the first 50 entries are shown.' }
    Write-Host ''
}

function Add-ExclusionPattern {
    $pattern = Read-Host 'Enter a file pattern to exclude (example: *.log or keepme.tmp)'
    if ([string]::IsNullOrWhiteSpace($pattern)) { return }
    if ($script:ExcludedPatterns -contains $pattern) { Write-Warn 'Pattern already exists.'; return }
    $script:ExcludedPatterns += $pattern
    Write-Good ("Added exclusion: {0}" -f $pattern)
}

function Show-Exclusions {
    Write-Host ''
    Write-Host '================ EXCLUSIONS =================' -ForegroundColor White
    foreach ($pattern in $script:ExcludedPatterns) { Write-Host $pattern -ForegroundColor Cyan }
    Write-Host ''
}

function Show-About {
    Write-Host ''
    Write-Host 'This tool performs a deeper current-user cleanup than v1.' -ForegroundColor Cyan
    Write-Host 'It supports category-based cleanup, exclusions, and locked-file reporting.'
    Write-Host ''
}

function Show-UserProfileCleanupPage {
    $header = @(
        '============================================================',
        '================= USER PROFILE CLEANUP V2 ==================',
        '============================================================'
    )
    Show-MenuPage -Title 'User Profile Cleanup Menu' -Items (& $script:GetUserProfileCleanupItems) -HeaderLines $header
}

$script:GetUserProfileCleanupItems = {
    @(
        (New-MenuItem -Key '1' -Label 'Deep Scan Current User Profile' -Action { Show-DeepScanReport } -PauseAfter $true)
        (New-MenuItem -Key '2' -Label 'Show Cleanup Categories' -Action { Show-CategorySummary } -PauseAfter $true)
        (New-MenuItem -Key '3' -Label 'Clean Temp Category' -Action { Remove-CategoryItems -Category 'Temp' } -PauseAfter $true -Color 'DarkYellow')
        (New-MenuItem -Key '4' -Label 'Clean Browser Cache Category' -Action { Remove-CategoryItems -Category 'Browser Cache' } -PauseAfter $true -Color 'DarkYellow')
        (New-MenuItem -Key '5' -Label 'Clean App Cache Category' -Action { Remove-CategoryItems -Category 'App Cache' } -PauseAfter $true -Color 'DarkYellow')
        (New-MenuItem -Key '6' -Label 'Clean Crash Data Category' -Action { Remove-CategoryItems -Category 'Crash Data' } -PauseAfter $true -Color 'DarkYellow')
        (New-MenuItem -Key '7' -Label 'Clean Logs Category' -Action { Remove-CategoryItems -Category 'Logs' } -PauseAfter $true -Color 'DarkYellow')
        (New-MenuItem -Key '8' -Label 'Show Locked/Skipped Files' -Action { Show-LockedItems } -PauseAfter $true -Color 'Cyan')
        (New-MenuItem -Key '9' -Label 'About This Tool' -Action { Show-About } -PauseAfter $true -Color 'Cyan')
        (New-MenuItem -Key '10' -Label 'Add Exclusion Pattern' -Action { Add-ExclusionPattern } -PauseAfter $true -Color 'Cyan')
        (New-MenuItem -Key '11' -Label 'Show Exclusions' -Action { Show-Exclusions } -PauseAfter $true -Color 'Cyan')
        (New-MenuItem -Key '0' -Label 'Return To Toolbox' -Action { Write-Host 'Returning to Technification Toolbox...' -ForegroundColor Blue; return '__EXIT_MENU__' } -Color 'Blue')
    )
}

Invoke-MenuLoop -Render { Show-UserProfileCleanupPage } -GetItems $script:GetUserProfileCleanupItems
