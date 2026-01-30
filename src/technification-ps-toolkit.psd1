@{
    # General metadata
    RootModule        = 'technification-ps-toolkit.psm1'
    ModuleVersion     = '0.1.0'
    GUID              = 'a635402b-e732-4826-a8f1-be03c043c3eb'   # Randomly generated New-Guid
    Author            = 'Dean John Weiniger'
    CompanyName       = 'Technification'
    Copyright         = '(c) 2026 Dean John Weiniger. All rights reserved.'
    Description       = 'Enterprise-grade PowerShell toolkit for Windows maintenance and automation.'

    # Functions to export (we’ll fill these in later)
    FunctionsToExport = @()
    CmdletsToExport   = @()
    VariablesToExport = @()
    AliasesToExport   = @()

    # Requirements
    PowerShellVersion = '5.1'
    CompatiblePSEditions = @('Desktop','Core')

    # Optional metadata
    Tags              = @('PowerShell','Automation','Windows','Technification')
    LicenseUri        = 'https://opensource.org/licenses/MIT'
    ProjectUri        = 'https://github.com/djw1080/technification-ps-toolkit'
    ReleaseNotes      = 'Initial scaffold for technification-ps-toolkit.'
}
