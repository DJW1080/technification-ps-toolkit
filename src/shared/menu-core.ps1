Set-StrictMode -Version Latest

function New-MenuItem {
    param(
        [Parameter(Mandatory)][string]$Key,
        [Parameter(Mandatory)][string]$Label,
        [scriptblock]$Action,
        [string]$Description = '',
        [string]$Color = 'Green',
        [bool]$PauseAfter = $false,
        [bool]$IsEnabled = $true,
        [string]$Status = ''
    )

    [pscustomobject]@{
        Key = $Key
        Label = $Label
        Action = $Action
        Description = $Description
        Color = $Color
        PauseAfter = $PauseAfter
        IsEnabled = $IsEnabled
        Status = $Status
    }
}

function Show-MenuPage {
    param(
        [Parameter(Mandatory)][string]$Title,
        [Parameter(Mandatory)][object[]]$Items,
        [string[]]$HeaderLines = @(),
        [string[]]$FooterLines = @(),
        [bool]$ClearScreen = $true
    )

    if ($ClearScreen) { Clear-Host }
    foreach ($line in $HeaderLines) {
        Write-Host $line
    }

    Write-Host $Title -ForegroundColor White
    Write-Host ''

    foreach ($item in $Items) {
        $color = if ($item.IsEnabled) { $item.Color } else { 'DarkGray' }
        $statusText = if ([string]::IsNullOrWhiteSpace($item.Status)) { '' } else { " [$($item.Status)]" }
        Write-Host (" [{0}] - {1}{2}" -f $item.Key, $item.Label, $statusText) -ForegroundColor $color
        if (-not [string]::IsNullOrWhiteSpace($item.Description)) {
            Write-Host ("       {0}" -f $item.Description) -ForegroundColor DarkGray
        }
    }

    if ($FooterLines.Count -gt 0) {
        Write-Host ''
        foreach ($line in $FooterLines) {
            Write-Host $line
        }
    }
}

function Invoke-MenuLoop {
    param(
        [Parameter(Mandatory)][scriptblock]$Render,
        [Parameter(Mandatory)][scriptblock]$GetItems,
        [string]$Prompt = '  -----> Select an option'
    )

    $shouldExit = $false
    while (-not $shouldExit) {
        & $Render
        $items = @(& $GetItems)
        $choice = Read-Host $Prompt
        $selected = $items | Where-Object { $_.Key -eq $choice } | Select-Object -First 1

        if (-not $selected) {
            Write-Host '[FAIL]  Invalid selection.' -ForegroundColor Red
            Pause
            continue
        }

        if (-not $selected.IsEnabled) {
            Write-Host '[WARN]  This option is not available.' -ForegroundColor Yellow
            Pause
            continue
        }

        if ($selected.Action) {
            $result = & $selected.Action
            if ($result -eq '__EXIT_MENU__') {
                $shouldExit = $true
                continue
            }
        }

        if ($selected.PauseAfter) {
            Pause
        }
    }
}
