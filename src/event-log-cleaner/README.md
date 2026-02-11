# 💻 Windows Event Log Cleaner Tool

![PowerShell 7](https://img.shields.io/badge/Code-PowerShell-012456?logo=powershell "100% PowerShell")
![Windows OS](https://img.shields.io/badge/OS-Windows-0079d4?logo=windows "Runs on Windows")
![Intel](https://img.shields.io/badge/CPU-Intel-31c5f3?logo=intel "Intel Compatible")
![AMD](https://img.shields.io/badge/CPU-AMD-00a774?logo=amd "AMD Compatible")
![Made in Australia](https://img.shields.io/badge/Made%20In-Australia-blue?logo=australia "Made in Australia")
![Difficulty: Beginner](https://img.shields.io/badge/Difficulty-Beginners-1f883d?logo=beginners "Difficulty: Beginner")
![Version 1.0](https://img.shields.io/badge/Version-1.0-yellow?logo=version "Version 1.0")  

## 🔎 About

This **PowerShell Script** automates Windows maintenance and Event Log clearing.  
Using a **Menu-driven Interface** with extended clearing options and logging.  
Designed for **Users and IT Professionals**, this tool provides a streamlined, colour-coded workflow with summary.

## 🛠️ Functions

- Show current exclusions
- Add exact exclusion
- Add wildcard exclusion
- Remove exact exclusion
- Remove wildcard exclusion
- Start Log Cleaner
- Toggle WhatIf (dry run)

## ⚙️ Main Function

- Windows Log Cleaner - Option [6]

## ✅ Requirements

- Windows 11  
- PowerShell 7  
- Run as Administrator

## 📦 Installation

```powershell
git clone https://github.com/DJW1080/technification-ps-toolkit.git
cd .\technification-ps-toolkit\src\event-log-cleaner\
.\event-log-cleaner-v1.ps1
```

## 📋 Interactive Menu

```text
 [1] - Show current exclusions
 [2] - Add exact exclusion
 [3] - Add wildcard exclusion
 [4] - Remove exact exclusion
 [5] - Remove wildcard exclusion
 [6] - Start Log Cleaner
 [7] - Toggle WhatIf (dry run)  [Currently: False]
 [0] - Exit
```

## ⚡ Best Practices

- **Run before major updates and image creation**  
- **Clear all the event logs before creating a new image of Windows**

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

### _Last updated: 21-01-2026_
