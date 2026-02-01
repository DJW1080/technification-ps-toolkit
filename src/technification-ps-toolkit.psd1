@{
    RootModule = 'technification-ps-toolkit.psm1'
    ModuleVersion = '0.1.0'
    GUID = 'a635402b-e732-4826-a8f1-be03c043c3eb'
    Author = 'Dean John Weiniger'
    CompanyName = 'Technification'
    Copyright = '(c) 2026 Dean John Weiniger. All rights reserved.'
    Description = 'Enterprise-grade PowerShell toolkit for Windows maintenance and automation.'

    # Require PowerShell 7+
    PowerShellVersion = '7.0'
    CompatiblePSEditions = @('Core')

    FunctionsToExport = '*'
    CmdletsToExport   = @()
    VariablesToExport = @()
    AliasesToExport   = @()

    PrivateData = @{
        PSData = @{
            Tags         = @('PowerShell','Automation','Windows','Technification')
            LicenseUri   = 'https://opensource.org/licenses/MIT'
            ProjectUri   = 'https://github.com/djw1080/technification-ps-toolkit'
            ReleaseNotes = 'Initial scaffold for technification-ps-toolkit.'
        }
    }
}
