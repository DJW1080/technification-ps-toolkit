<#
    Technification PS Toolkit
    Tool: Open PowerShell Here as Administrator
    Version: 1.0
    Description:
        Adds a context menu entry to open PowerShell in the current directory with elevated privileges.
#>

# Requires admin
if (-not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error "This script must be run as Administrator."
    exit 1
}

$menu = 'Open PowerShell Here (Admin)'
$command = "$PSHOME\pwsh.exe -NoExit -NoProfile -Command ""Set-Location '%V'"""

'directory', 'directory\background', 'drive' | ForEach-Object {
    New-Item -Path "Registry::HKEY_CLASSES_ROOT\$_\shell" -Name runas\command -Force |
    Set-ItemProperty -Name '(default)' -Value $command -PassThru |
    Set-ItemProperty -Path {$_.PSParentPath} -Name '(default)' -Value $menu -PassThru |
    Set-ItemProperty -Name HasLUAShield -Value ''
}

Write-information "Open PowerShell Here (Admin) - Context Menu entry added successfully."
