# Module Loader for technification-ps-toolkit

# Import Public functions
Get-ChildItem -Path $PSScriptRoot/Public -Filter *.ps1 -Recurse | ForEach-Object {
    . $_.FullName
}

# Import Private helpers
Get-ChildItem -Path $PSScriptRoot/Private -Filter *.ps1 -Recurse | ForEach-Object {
    . $_.FullName
}

# Export only Public functions
Export-ModuleMember -Function (Get-ChildItem -Path $PSScriptRoot/Public -Filter *.ps1 -Recurse |
    ForEach-Object { $_.BaseName })