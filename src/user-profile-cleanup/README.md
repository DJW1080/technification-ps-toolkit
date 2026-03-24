# ⚡ User Profile Cleanup 🧰

![PowerShell 7](https://img.shields.io/badge/Code-PowerShell-012456?logo=powershell "100% PowerShell")
![Windows OS](https://img.shields.io/badge/OS-Windows-0079d4?logo=windows "Runs on Windows")
![Intel](https://img.shields.io/badge/CPU-Intel-31c5f3?logo=intel "Intel Compatible")
![AMD](https://img.shields.io/badge/CPU-AMD-00a774?logo=amd "AMD Compatible")
![Made in Australia](https://img.shields.io/badge/Made%20In-Australia-blue?logo=australia "Made in Australia")
![Difficulty: Beginner](https://img.shields.io/badge/Difficulty-Beginners-1f883d?logo=beginners "Difficulty: Beginner")
![Version 2.2](https://img.shields.io/badge/Version-2.2-yellow?logo=version "Version 2.2")

User Profile Cleanup - Cleans up temp files in the user profile.

## 📦 Run Script Directly

```powershell
pwsh -File .\user-profile-cleanup-v2.ps1
```

## 📋 Current Menu

```text
[1]  Deep Scan Current User Profile
[2]  Show Cleanup Categories
[3]  Clean Temp Category
[4]  Clean Browser Cache Category
[5]  Clean App Cache Category
[6]  Clean Crash Data Category
[7]  Clean Logs Category
[8]  Show Locked/Skipped Files
[9]  About This Tool
[10] Add Exclusion Pattern
[11] Show Exclusions
[0]  Return To Toolbox
```

## 🕹️ What It Does

- Runs a deeper size-based scan of the current user profile
- Cleans by category instead of one broad delete pass
- Tracks locked or skipped files
- Supports exclusion patterns

## 📋 Cleanup Categories

- Temp
- Browser Cache
- App Cache
- Crash Data
- Logs

## 📂 Directory Layout

```text
user-profile-cleanup/
├── user-profile-cleanup-v2.ps1     Current user profile cleanup submenu.
└── README.md                       Module documentation.
```

Report and Logs output is written to:

```text
C:\ProgramData\Technification\
├── Logs
└── Report
```

## 👨‍💻 Requirements

- Windows 11
- PowerShell 7
- Administrator

## 📋 Notes

- This module only targets the current user profile.
- Locked files may require closing the related app before rerunning cleanup.
