# ⚡ Windows Auto Repair 🧰

![PowerShell 7](https://img.shields.io/badge/Code-PowerShell-012456?logo=powershell "100% PowerShell")
![Windows OS](https://img.shields.io/badge/OS-Windows-0079d4?logo=windows "Runs on Windows")
![Intel](https://img.shields.io/badge/CPU-Intel-31c5f3?logo=intel "Intel Compatible")
![AMD](https://img.shields.io/badge/CPU-AMD-00a774?logo=amd "AMD Compatible")
![Made in Australia](https://img.shields.io/badge/Made%20In-Australia-blue?logo=australia "Made in Australia")
![Difficulty: Beginner](https://img.shields.io/badge/Difficulty-Beginners-1f883d?logo=beginners "Difficulty: Beginner")
![Version 1.3](https://img.shields.io/badge/Version-1.3-yellow?logo=version "Version 1.3")

Windows Auto Repair is the repair and maintenance module.

## 📦 Run Script Directly

```powershell
pwsh -file .\win-auto-repair-v1.ps1
```

## 📋 Current Menu

```text
[1]  Start Windows Full Repair
[2]  Remove Temporary Files
[3]  DISM CheckHealth
[4]  DISM ScanHealth
[5]  DISM RestoreHealth
[6]  Component Cleanup
[7]  Component Cleanup + ResetBase
[8]  Repair Network Stack
[9]  Repair Windows Update
[10] New System Restore Point
[11] Start Diagnostics
[12] Start SFC
[13] Recommended Maintenance Sequence
[14] Flush DNS Cache
[15] Start CHKDSK Online Scan
[16] Export System Health Report
[0]  Return To Toolbox
```

## 🕹️ What It Does

- Runs DISM and SFC repair actions
- Clears temporary files
- Repairs network and Windows Update components
- Creates a restore point
- Runs diagnostics and exports a health report

## 📂 Storage Layout

Shared runtime output is written to:

```text
C:\ProgramData\Technification\
├── Logs
└── Reports
```

This module also creates a separate transcript log during a session.

## 📂 Directory Layout

```text
windows-auto-repair/
├── win-auto-repair-v1.ps1          Windows repair and maintenance submenu.
└── README.md                       Module documentation.
```

## ⚡ Run Directly

```powershell
pwsh -File .\win-auto-repair-v1.ps1
```

## 👨‍💻 Requirements

- Windows 10 or Windows 11
- PowerShell 7
- Administrator rights recommended

## 📝 Notes

- Option `1` runs the full repair sequence.
- Option `13` runs a broader maintenance sequence including restore point creation, repair, temp cleanup, and DNS cache flush.
