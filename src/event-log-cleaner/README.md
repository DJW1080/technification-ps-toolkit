# ⚡ Event Log Cleaner 🧰

![PowerShell 7](https://img.shields.io/badge/Code-PowerShell-012456?logo=powershell "100% PowerShell")
![Windows OS](https://img.shields.io/badge/OS-Windows-0079d4?logo=windows "Runs on Windows")
![Intel](https://img.shields.io/badge/CPU-Intel-31c5f3?logo=intel "Intel Compatible")
![AMD](https://img.shields.io/badge/CPU-AMD-00a774?logo=amd "AMD Compatible")
![Made in Australia](https://img.shields.io/badge/Made%20In-Australia-blue?logo=australia "Made in Australia")
![Difficulty: Beginner](https://img.shields.io/badge/Difficulty-Beginners-1f883d?logo=beginners "Difficulty: Beginner")
![Version 2.2](https://img.shields.io/badge/Version-2.2-yellow?logo=version "Version 2.2")

Event Log Cleaner is the event-log maintenance module.

## 📦 Run Script Directly

```osps
powerps
       Zh -File .\event-log-cleaner-v2.ps1`

## 📋 Current Menu

```text
[1]   Show current exclusions / settings
[2]   Add exact exclusion
[3]   Add wildcard exclusion
[4]   Remove exact exclusion
[5]   Remove wildcard exclusion
[6]   Start Log Cleaner
[7]   Toggle WhatIf (dry run)
[8]   Toggle SkipCounts
[9]   Load exclusions from config
[10]  Save exclusions to config
[11]  Reset exclusions to defaults
[0]   Return To Toolbox
```

## 🕹️ What It Does

- Lists current exclusions and settings
- Supports exact and wildcard exclusions
- Supports dry run mode and live clearing
- Saves and reloads exclusion settings
- Resets exclusions to defaults

## 📂 Storage Layout

Shared logs are written to:

```text
C:\ProgramData\Technification\
├── Logs
└── Reports
```

This module also stores exclusions config under:

```text
C:\ProgramData\Technification\EventLogCleaner\config.json
```

## 📂 Directory Layout

```text
event-log-cleaner/
├── event-log-cleaner-v2.ps1        Event log cleaner submenu.
└── README.md                       Module documentation.
```

## 👨‍💻 Requirements

- Windows 10 or Windows 11
- PowerShell 7
- Administrator rights for live log clearing

## 📝 Notes

- The script supports both an interactive submenu and `-Auto` mode.
- Live `-Auto` runs require `-Force` unless dry run mode is enabled.
