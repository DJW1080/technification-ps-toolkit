# ⚡Windows Enhancements 🧰

![PowerShell 7](https://img.shields.io/badge/Code-PowerShell-012456?logo=powershell "100% PowerShell")
![Windows OS](https://img.shields.io/badge/OS-Windows-0079d4?logo=windows "Runs on Windows")
![Intel](https://img.shields.io/badge/CPU-Intel-31c5f3?logo=intel "Intel Compatible")
![AMD](https://img.shields.io/badge/CPU-AMD-00a774?logo=amd "AMD Compatible")
![Made in Australia](https://img.shields.io/badge/Made%20In-Australia-blue?logo=australia "Made in Australia")
![Difficulty: Beginner](https://img.shields.io/badge/Difficulty-Beginners-1f883d?logo=beginners "Difficulty: Beginner")
![Version 1.5](https://img.shields.io/badge/Version-1.5-yellow?logo=version "Version 1.5")

Windows Enhancements is the convenience and tweaks module.

## 📦 Run Script Directly

```powershell
pwsh -File  .\win-enhancements-v1.ps1
```

## 📋 Current Menu

```text
[1] Install "Open PowerShell Here (Admin)" Context Menu
[2] Enable / Disable Hibernation
[3] Disk Cleanup Tool
[9] About This Menu
[0] Return To Toolbox
```

## 🕹️ What It Does

- Installs the Explorer context menu entry for elevated PowerShell
- Checks and changes hibernation state
- Runs the disk cleanup workflow

## 📂 Storage Layout

Shared runtime output is written to:

```text
C:\ProgramData\Technification\
├── Logs
└── Reports
```

## 📂 Directory Layout

```text
windows-enhancements/
├── win-enhancements-v1.ps1         Enhancements submenu.
├── open-ps-admin-here-v1.ps1       Explorer context-menu helper.
├── check-hibernation-v1.ps1        Hibernation control helper.
├── disk-clean-up-v2.ps1            Disk cleanup workflow.
└── README.md                       Module documentation.
```

## ⚡ Run Directly

```powershell
pwsh -File .\win-enhancements-v1.ps1
```

## 👨‍💻 Requirements

- Windows 10 or Windows 11
- PowerShell 7
- Administrator rights

## 📝 Notes

- The PowerShell context-menu option uses the standalone helper script in this folder.  
- The Disk Cleanup Tool focuses on temp data, caches, logs, and update leftovers.
