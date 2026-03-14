# Network Diagnostics Suite

![PowerShell 7](https://img.shields.io/badge/Code-PowerShell-012456?logo=powershell "100% PowerShell")
![Windows OS](https://img.shields.io/badge/OS-Windows-0079d4?logo=windows "Runs on Windows")
![Intel](https://img.shields.io/badge/CPU-Intel-31c5f3?logo=intel "Intel Compatible")
![AMD](https://img.shields.io/badge/CPU-AMD-00a774?logo=amd "AMD Compatible")
![Made in Australia](https://img.shields.io/badge/Made%20In-Australia-blue?logo=australia "Made in Australia")
![Difficulty: Beginner](https://img.shields.io/badge/Difficulty-Beginners-1f883d?logo=beginners "Difficulty: Beginner")
![Version 1.0](https://img.shields.io/badge/Version-1.1-yellow?logo=version "Version 1.1")  

This module provides menu-driven network diagnostics and TCP port scanning.

## Script

- `network-diagnostics-v1.ps1`

## Directory Layout

```text
network-diagnostics/
├── network-diagnostics-v1.ps1      Network diagnostics submenu.
└── README.md                       Module documentation.
```

## Current Menu

```text
[1] Show Network Summary
[2] Test Default Gateway Reachability
[3] Test DNS Resolution
[4] Test Internet Connectivity
[5] Show Active TCP Connections
[6] Scan Common TCP Ports
[7] Scan Custom TCP Ports
[8] Export Network Report
[9] Run Quick Health Check
[0] Return To Toolbox
```

## What It Does

- Shows adapter and IP summary information
- Tests gateway, DNS, and internet connectivity
- Lists active TCP connections
- Scans common and custom TCP ports
- Exports a network report
- Runs a quick health check across the main diagnostics

## Run Directly

```powershell
pwsh -File .\src\network-diagnostics\network-diagnostics-v1.ps1
```

## Requirements

- Windows 10 or Windows 11
- PowerShell 7
- Administrator rights recommended for complete visibility
