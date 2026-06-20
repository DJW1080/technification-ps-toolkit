# ⚡ Technification PowerShell Toolkit 🧰

**_Observing patterns in systems, code, and culture_**

![PowerShell 7](https://img.shields.io/badge/Code-PowerShell-012456?logo=powershell "100% PowerShell")
![Windows OS](https://img.shields.io/badge/OS-Windows-0079d4?logo=windows "Runs on Windows")
![Intel](https://img.shields.io/badge/CPU-Intel-31c5f3?logo=intel "Intel Compatible")
![AMD](https://img.shields.io/badge/CPU-AMD-00a774?logo=amd "AMD Compatible")
![Made in Australia](https://img.shields.io/badge/Made%20In-Australia-blue?logo=australia "Made in Australia")
![Difficulty: Beginner](https://img.shields.io/badge/Difficulty-Beginners-1f883d?logo=beginners "Difficulty: Beginner")
![Version 1.9](https://img.shields.io/badge/Version-1.9-yellow?logo=version "Version 1.9")

Technification PowerShell Toolkit is a collection of PowerShell utilities.  
Designed for **Beginners** and **Professionals** it features a single launcher and module submenus.  
Automating Windows maintenance and repair tasks using a **Menu-driven Interface**.  

![Technification Powershell Toolkit](/technification-ps-toolkit.png)  

## 🕹️ Current Modules

- `Windows Auto Repair` (`v1.3`)
- `User Profile Cleanup` (`v2.2`)
- `Event Log Cleaner` (`v2.2`)
- `Windows Enhancements` (`v1.5`)
- `Network Diagnostics Suite` (`v1.2`)
- `Winget Maintenance` (`v1.0`)

## 📦 Run Script Directly

```powershell
git clone https://github.com/DJW1080/technification-ps-toolkit.git
cd technification-ps-toolkit
.\technification-toolbox.ps1
```

## 📂 Directory Layout

```text
technification-ps-toolkit/
├── technification-toolbox.ps1           Main toolbox launcher.
├── src/
│   ├── shared/
│   │   ├── menu-core.ps1                Shared menu framework.
│   │   └── logging-core.ps1             Shared logging and report helpers.
│   ├── windows-auto-repair/
│   │   ├── win-auto-repair-v1.ps1       Windows repair submenu.
│   │   └── README.md                    Module documentation.
│   ├── user-profile-cleanup/
│   │   ├── user-profile-cleanup-v2.ps1  Current user cleanup submenu.
│   │   └── README.md                    Module documentation.
│   ├── event-log-cleaner/
│   │   ├── event-log-cleaner-v2.ps1     Event log cleaner submenu.
│   │   └── README.md                    Module documentation.
│   ├── windows-enhancements/
│   │   ├── win-enhancements-v1.ps1      Enhancements submenu.
│   │   ├── open-ps-admin-here-v1.ps1    Explorer context-menu helper.
│   │   ├── check-hibernation-v1.ps1     Hibernation control helper.
│   │   ├── disk-clean-up-v2.ps1         Disk cleanup workflow.
│   │   └── README.md                    Module documentation.
│   ├── winget-maintenance/
│   │   ├── winget-maintenance-v1.ps1    Winget maintenance submenu.
│   │   └── README.md                    Module documentation.
│   └── network-diagnostics/
│       ├── network-diagnostics-v1.ps1   Network diagnostics submenu.
│       └── README.md                    Module documentation.
└── README.md                            Project overview.
```

Report and Logs output is written to:

```text
C:\ProgramData\Technification\
├── Logs
└── Reports
```

## 👨‍💻 Requirements

- Windows 11
- PowerShell 7
- Administrator

## 📝 Notes

- Menus use a shared framework in `src\shared\menu-core.ps1`.
- Shared logs and reports use `src\shared\logging-core.ps1`.
- Most modules can be launched directly, but the toolbox is the intended entry point.
- If `ProgramData` is unavailable, the shared logging helper falls back to `%LOCALAPPDATA%\Technification`.

## 📝 Credits

Created by **Dean John Weiniger**.  

## 📜 Licence

This work is dedicated to the public domain under the **Creative Commons CC0 1.0 Universal License**.  
[![CC0 1.0](https://img.shields.io/badge/License-CC0%201.0-lightgrey?logo=creativecommons&logoColor=white)](https://creativecommons.org/publicdomain/zero/1.0/)  

**You are free to:**  
✅ **Share** – Copy and redistribute the material in any medium or format.  
✅ **Adapt** – Remix, transform, and build upon the material for any purpose, even commercially.  
✅ **Use without attribution** – No credit required, though it’s appreciated.

**No conditions apply:**  
🚫 No attribution required.  
🚫 No restrictions on use.  
**Full licence text:** [CC0 1.0 Universal](https://creativecommons.org/publicdomain/zero/1.0/)  

---

### updated: 20-06-2026
