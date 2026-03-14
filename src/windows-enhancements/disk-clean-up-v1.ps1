# Deep-Disk-Cleanup-Win11.ps1
# Run as Admin

# Auto-elevate if needed
$elevated = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole("Administrator")
if (-not $elevated) {
    $powershellPath = "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe"
    Start-Process $powershellPath "-ExecutionPolicy Bypass -File `"$PSCommandPath`"" -Verb RunAs
    exit
}

# Optional: Create restore point for rollback
Write-Host "Creating restore point..." -ForegroundColor Cyan
Checkpoint-Computer -Description "Pre-DeepCleanup $(Get-Date -Format s)" -RestorePointType "Modify_Settings"

# 1️⃣ Windows built‑in Disk Cleanup (requires sageset pre-configured)
Write-Host "Running Disk Cleanup profile 1..." -ForegroundColor Cyan
Start-Process cleanmgr -ArgumentList "/sagerun:1" -NoNewWindow -Wait

# 2️⃣ Temp folders
Write-Host "Clearing temp folders..." -ForegroundColor Cyan
Remove-Item "$env:TEMP\*" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "C:\Windows\Temp\*" -Recurse -Force -ErrorAction SilentlyContinue

# 3️⃣ Delivery Optimization files
Write-Host "Clearing Delivery Optimization cache..." -ForegroundColor Cyan
Stop-Service -Name dosvc -Force
Remove-Item "C:\Windows\SoftwareDistribution\DeliveryOptimization\*" -Recurse -Force -ErrorAction SilentlyContinue
Start-Service -Name dosvc

# 4️⃣ Windows Update cache
Write-Host "Cleaning Windows Update cache..." -ForegroundColor Cyan
Stop-Service -Name wuauserv -Force
Remove-Item "C:\Windows\SoftwareDistribution\Download\*" -Recurse -Force -ErrorAction SilentlyContinue
Start-Service -Name wuauserv

# 5️⃣ Old driver store files
Write-Host "Removing unused device driver packages..." -ForegroundColor Cyan
pnputil /enum-drivers | ForEach-Object {
    if ($_ -match "Published Name : (oem\d+.inf)") {
        $driver = $matches[1]
        try { pnputil /delete-driver $driver /uninstall /force /reboot } catch {}
    }
}

# 6️⃣ Recycle Bin
Write-Host "Emptying Recycle Bin..." -ForegroundColor Cyan
Clear-RecycleBin -Force -ErrorAction SilentlyContinue

# 7️⃣ Storage Sense run (if configured)
Write-Host "Triggering Storage Sense cleanup..." -ForegroundColor Cyan
$storageSense = Get-ScheduledTask | Where-Object {$_.TaskName -like "*StartStorageSense*"}
if ($storageSense) { Start-ScheduledTask -TaskName $storageSense.TaskName }

Write-Host "Deep cleanup complete ✅" -ForegroundColor Green