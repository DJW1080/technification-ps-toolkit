# 💻 Windows Automatic Repair Tool

![PowerShell 7](https://img.shields.io/badge/Code-PowerShell-012456?logo=powershell "100% PowerShell")
![Windows OS](https://img.shields.io/badge/OS-Windows-0079d4?logo=windows "Runs on Windows")
![Intel](https://img.shields.io/badge/CPU-Intel-31c5f3?logo=intel "Intel Compatible")
![AMD](https://img.shields.io/badge/CPU-AMD-00a774?logo=amd "AMD Compatible")
![Made in Australia](https://img.shields.io/badge/Made%20In-Australia-blue?logo=australia "Made in Australia")
![Difficulty: Beginner](https://img.shields.io/badge/Difficulty-Beginners-1f883d?logo=beginners "Difficulty: Beginner")
![Version 1.0](https://img.shields.io/badge/Version-1.0-yellow?logo=version "Version 1.0")  

## 🔎 About

This **PowerShell Script** automates Windows maintenance and repair tasks using **DISM** and **SFC**.  
Using a **Menu-Driven Interface** with extended repair options and logging.  
Designed for **Users and IT Professionals**, this tool provides a streamlined, colour-coded repair workflow with summary.

## 🛠️ Functions

- DISM & SFC Automation  
- Temporary File Remover
- CheckHealth, ScanHealth, & RestoreHealth  
- Component Cleanup & ResetBase  
- Network Stack Reset  
- Windows Update Repair  
- System Restore Point Option  
- System Diagnostics
- System File Checker  

## ⚙️ Windows Full Repair - Option [1]

- `DISM /CheckHealth`
- `DISM /ScanHealth`
- `DISM /RestoreHealth`
- `SFC /Scannow` (twice)
- `DISM /AnalyzeComponentStore`
- `DISM /StartComponentCleanup`
- `DISM /ResetBase`

## ✅ Requirements

- Windows 10 or 11  
- PowerShell 7  
- Run as Administrator

## 📦 Installation

```powershell
https://github.com/DJW1080/technification-ps-toolkit.git
.\win-auto-repair-v1.ps1
```

## 📋 Interactive Menu

```text
 [1]  - Start Windows Full Repair
 [2]  - Remove Temporary Files
 [3]  - DISM CheckHealth
 [4]  - DISM ScanHealth 
 [5]  - DISM RestoreHealth 
 [6]  - Component Cleanup
 [7]  - Component Cleanup + ResetBase
 [8]  - Repair Network Stack 
 [9]  - Repair Windows Update
 [10] - New System Restore Point
 [11] - Start Diagnostics
 [12] - Start SFC 
 [0]  - Exit
```

## 👉 Tip

Run option [1] for full repair,  
Then option [2] to delete temporary files.

## ⚡ Best Practices

- **Run after major updates**  
  Perform a full repair after large Windows Updates.

- **Always create a restore point first**  
  Use option [10] before running repairs.

- **Use Full Repair Mode for routine maintenance**  
  Option [1] to run all checks and fixes in one go. Ideal for routine maintenance.

- **Check logs for details**  
  Review `%LOCALAPPDATA%\Win-Auto-Repair\Win-Auto-Repair.log` and `C:\Windows\Logs\CBS\CBS.log`.

- **Safe Mode for stubborn issues**  
  If SFC or DISM fail, reboot into Safe Mode and rerun the commands.

- **Network & Update Repairs**  
  Use options [8] and [9] if you are experiencing connectivity issues or Windows Update failures.

## 📝 Credits

Created by **Dean John Weiniger**  
Part of the **Technification PowerShell Toolkit**  
Contributions welcome  

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

### _Last updated: 17-11-2025_
