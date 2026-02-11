<#
    Script Name : win-auto-repair.ps1
    Description : Windows Automated Image Health Check and Repair Tool
                  Main functions:
                  - Runs DISM and SFC commands
                  - Cleans temporary files
    Author      : Dean John Weiniger
    Version     : 1.0
    Date        : 2025-11-14
#>

# ---------------------------
# Global Setup
# ---------------------------
$LogRoot = Join-Path $env:LOCALAPPDATA "Win-Auto-Repair"
if (!(Test-Path $LogRoot)) { New-Item -ItemType Directory -Path $LogRoot | Out-Null }
$SessionLog = Join-Path $LogRoot ("session-" + (Get-Date -Format "yyyyMMdd-HHmmss") + ".log")
Start-Transcript -Path $SessionLog -Force | Out-Null

# ---------------------------
# Logging Helpers
# ---------------------------
function Write-Info($msg) { Write-Host "[INFO]  $msg" -ForegroundColor Cyan }
function Write-Good($msg) { Write-Host "[GOOD]  $msg" -ForegroundColor Green }
function Write-Bad($msg) { Write-Host "[FAIL]  $msg" -ForegroundColor Red }

# ---------------------------
# Spinner Wrapper
# ---------------------------
function Invoke-WithSpinner {
    param(
        [Parameter(Mandatory)][string]$Description,
        [Parameter(Mandatory)][string]$Command
    )

    $ActionLog = Join-Path $LogRoot ("action-" + (Get-Date -Format "yyyyMMdd-HHmmss") + ".log")

    Write-Info "$Description started..."
    Write-Info "Log: $ActionLog"

    # Start background process
    $ps = Start-Process powershell -ArgumentList "-NoLogo", "-Command", "$Command | Out-File -FilePath '$ActionLog' -Append -Encoding utf8" -PassThru -WindowStyle Hidden

    # Spinner frames
    $frames = @("|", "/", "-", "\")
    $i = 0

    while (!$ps.HasExited) {
        $frame = $frames[$i % $frames.Length]
        Write-Host -NoNewline "`r[$frame] $Description..."
        Start-Sleep -Milliseconds 120
        $i++
    }

    Write-Host "`r    " -NoNewline

    if ($ps.ExitCode -eq 0) {
        Write-Good "$Description completed."
    } else {
        Write-Bad "$Description failed. Check log for details: $ActionLog"
    }
}

function Invoke-SystemRepairTask {
    param(
        [string]$Command,
        [string[]]$Arguments,
        [string]$Label,
        [ref]$Results
    )

    Write-Host "Running $Label..." -ForegroundColor Cyan
    $p = Start-Process -FilePath $Command -ArgumentList $Arguments -NoNewWindow -Wait -PassThru

    if ($p.ExitCode -eq 0) {
         $status = "✅ $Label completed successfully."
         Write-Host $status -ForegroundColor Green
    } else {
         $status = "❌ $Label failed with exit code $($p.ExitCode)."
         Write-Host $status -ForegroundColor Red
    }

    $Results.Value += $status
}

# ---------------------------
# Native-progress tasks (SFC & DISM)
# ---------------------------
function Run-SFC {
         Write-Info "Running System File Checker..."
         sfc /scannow
}

function Run-DISM-CheckHealth { 
         Write-Info "Running Image Check Health..."
         DISM /Online /Cleanup-Image /CheckHealth
}

function Run-DISM-ScanHealth  {
         Write-Info "Running Image Scan Health..."
         DISM /Online /Cleanup-Image /ScanHealth
}
	
function Run-DISM-RestoreHealth {
         Write-Info "Running Image Restore Health..."
         DISM /Online /Cleanup-Image /RestoreHealth 
}

function Run-ComponentCleanup {
         Write-Info "Running Component Cleanup..."
         DISM /Online /Cleanup-Image /StartComponentCleanup
}

function Run-ComponentCleanup-ResetBase {
         Write-Info "Running Component Reset Base..."	
         DISM /Online /Cleanup-Image /StartComponentCleanup /ResetBase
}

function Cleanup-TempFiles {
    Invoke-WithSpinner -Description "Temporary File Cleanup" -Command `
        "Remove-Item '$env:TEMP\*' -Recurse -Force -ErrorAction SilentlyContinue;
         Remove-Item 'C:\Windows\Temp\*' -Recurse -Force -ErrorAction SilentlyContinue;
         Remove-Item 'C:\Windows\SoftwareDistribution\Download\*' -Recurse -Force -ErrorAction SilentlyContinue;
         Remove-Item 'C:\Windows\Prefetch\*' -Recurse -Force -ErrorAction SilentlyContinue"
}

function Repair-Network {
    Invoke-WithSpinner -Description "Network Stack Reset" -Command `
        "netsh winsock reset; netsh int ip reset"
}

function Repair-WindowsUpdate {
    Invoke-WithSpinner -Description "Windows Update Repair" -Command `
        "Stop-Service wuauserv -Force;
         Stop-Service bits -Force;
         Remove-Item 'C:\Windows\SoftwareDistribution' -Recurse -Force -EA SilentlyContinue;
         Remove-Item 'C:\Windows\System32\catroot2' -Recurse -Force -EA SilentlyContinue;
         Start-Service wuauserv;
         Start-Service bits"
}

function Create-RestorePoint {
    Invoke-WithSpinner -Description "Creating Restore Point" -Command `
        "Checkpoint-Computer -Description 'WinRepairPro' -RestorePointType MODIFY_SETTINGS"
}

function Run-Diagnostics {
    Invoke-WithSpinner -Description "System Diagnostics" -Command `
        "systeminfo; Get-EventLog -LogName System -Newest 20"
}

function Run-Win-Repair {
    $summary = @()

    Invoke-SystemRepairTask "dism.exe" "/Online","/Cleanup-Image","/CheckHealth" "Image Check Health" ([ref]$summary)
    Invoke-SystemRepairTask "dism.exe" "/Online","/Cleanup-Image","/ScanHealth" "Image Scan Health" ([ref]$summary)
    Invoke-SystemRepairTask "dism.exe" "/Online","/Cleanup-Image","/RestoreHealth" "Image Restore Health" ([ref]$summary)
    Invoke-SystemRepairTask "sfc.exe" "/scannow" "System File Checker" ([ref]$summary)
    Invoke-SystemRepairTask "sfc.exe" "/scannow" "System File Checker (Second Run)" ([ref]$summary)
    Invoke-SystemRepairTask "dism.exe" "/Online","/Cleanup-Image","/AnalyzeComponentStore" "Analyze Component Store" ([ref]$summary)
    Invoke-SystemRepairTask "dism.exe" "/Online","/Cleanup-Image","/StartComponentCleanup" "Component Cleanup" ([ref]$summary)
    Invoke-SystemRepairTask "dism.exe" "/Online","/Cleanup-Image","/StartComponentCleanup","/ResetBase" "Component Reset Base" ([ref]$summary)

    Write-Host ""
    Write-Host "========================= SUMMARY =========================" -ForegroundColor Yellow
    foreach ($line in $summary) {
        if ($line -like "✅*") {
            Write-Host $line -ForegroundColor Green
        } elseif ($line -like "❌*") {
            Write-Host $line -ForegroundColor Red
        } else {
            Write-Host $line
        }
    }
    Write-Host "===========================================================" -ForegroundColor Yellow
}

function End-Program {
    Stop-Transcript
    Write-Host "Exiting Program..." -ForegroundColor Blue
    exit
}

# ---------------------------
# Menu
# ---------------------------
function Show-Menu {
    Clear-Host
    Write-Host "===========================================================" -ForegroundColor Yellow
    Write-Host "===                 Win-Auto-Repair Pro                 ===" -ForegroundColor Yellow
    Write-Host "===                                                     ===" -ForegroundColor Yellow
    Write-Host "===                        V 1.0                        ===" -ForegroundColor Yellow
    Write-Host "===========================================================" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  [1]  - Run Windows Full Repair" -ForegroundColor Green
    Write-Host "  [2]  - Cleanup Temporary Files" -ForegroundColor Green
    Write-Host "  [3]  - DISM CheckHealth" -ForegroundColor DarkGray 
    Write-Host "  [4]  - DISM ScanHealth" -ForegroundColor DarkGray 
    Write-Host "  [5]  - DISM RestoreHealth" -ForegroundColor DarkGray 
    Write-Host "  [6]  - Component Cleanup" -ForegroundColor DarkGray 
    Write-Host "  [7]  - Component Cleanup + ResetBase" -ForegroundColor DarkGray 
    Write-Host "  [8]  - Repair Network Stack" -ForegroundColor Magenta  
    Write-Host "  [9]  - Repair Windows Update" -ForegroundColor Magenta 
    Write-Host "  [10] - Create System Restore Point" -ForegroundColor DarkYellow 
    Write-Host "  [11] - Run Diagnostics" -ForegroundColor cyan 
    Write-Host "  [12] - Run SFC" -ForegroundColor DarkGreen 
    Write-Host "  [0]  - Exit" -ForegroundColor Blue
    Write-Host ""
}

# ---------------------------
# Execution Loop
# ---------------------------
while ($true) {
    Show-Menu
    $choice = Read-Host "  -----> Select an option"

    switch ($choice) {
        1 { Run-Win-Repair }
        2 { Cleanup-TempFiles }
        3 { Run-DISM-CheckHealth }
        4 { Run-DISM-ScanHealth }
        5 { Run-DISM-RestoreHealth }
        6 { Run-ComponentCleanup }
        7 { Run-ComponentCleanup-ResetBase }
        8 { Repair-Network }
        9 { Repair-WindowsUpdate }
        10 { Create-RestorePoint }
        11 { Run-Diagnostics }
        12 { Run-SFC }
        0 { End-Program }
        default { Write-Bad "Invalid selection." }
    }
    Pause
}

Stop-Transcript
