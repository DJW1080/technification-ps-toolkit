# Technification PowerShell Toolkit

**technification-ps-toolkit** is the root repository for all PowerShell scripts developed under the *Technification* banner.  
It is built on these principles: **safety, usability, and transparency**.

---

## Philosophy
Every script in this toolkit is designed with:
- **Transparency** – every action is echoed and logged.
- **Safety** – backups before deletion, reversible processes.
- **Auditability** – timestamped logs for traceability.
- **User Control** – prompts and modular design for customization.

---

## 🔧 Windows toolbox
- Windows Auto Repair Tool Pro
- Windows Event Log Cleaner Tool Pro

---

## Directory Map  
```
technification-ps-toolkit/
│
├── src/
│   ├── Public/
│   │   ├── <PublicFunction1>.ps1
│   │   ├── <PublicFunction2>.ps1
│   │   └── ...
│   │
│   ├── Private/
│   │   ├── <PrivateHelper1>.ps1
│   │   ├── <PrivateHelper2>.ps1
│   │   └── ...
│   │
│   ├── technification-ps-toolkit.psd1        # Module manifest
│   └── technification-ps-toolkit.psm1        # Module loader (imports Public/Private)
│
├── tests/
│   ├── Unit/
│   │   ├── <Function1>.Tests.ps1
│   │   ├── <Function2>.Tests.ps1
│   │   └── ...
│   │
│   ├── Integration/
│   │   └── <IntegrationTests>.ps1
│   │
│   └── Pester.psd1                           # Pester configuration
│
├── docs/
│   ├── <Function1>.md
│   ├── <Function2>.md
│   └── ...
│
├── build/
│   ├── build.ps1                             # Build script (packaging, manifest update)
│   ├── version.json                          # Versioning metadata
│   └── changelog-template.md                 # Auto-release notes template
│
├── output/
│   └── (auto-generated release artifacts)
│
├── .github/
│   └── workflows/
│       ├── test.yml                          # CI: ScriptAnalyzer + Pester + multi-version
│       ├── release.yml                       # CD: Auto-release on tag push
│       └── docs.yml                          # Auto-generate documentation
│
├── .config/
│   ├── ScriptAnalyzerSettings.psd1           # Enterprise-grade linting rules
│   └── CodeSigning.json                      # Optional: signing profile
│
├── .vscode/
│   ├── settings.json                         # Formatter, linting, Pester integration
│   └── extensions.json                       # Recommended extensions
│
├── CHANGELOG.md
├── README.md
├── LICENSE
└── CONTRIBUTING.md
```

---

## Getting Started
1. Clone the repo:
   ```bash
   git clone https://github.com/DJW1080/technification-ps-toolkit.git

2. Navigate to the script you want to run.
3. Review the script header for usage notes.
4. Run in PowerShell with appropriate permissions.

---

## 📜 License
This project is licensed under the MIT License – see the LICENSE file for details.

---

## Author  
### Dean John Weiniger – blending decades of hands-on engineering with meticulous PowerShell scripting.

---

_last Update: 30-01-2026_