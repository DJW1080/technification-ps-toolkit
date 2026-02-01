# Module Loader for technification-ps-toolkit

$publicPath  = Join-Path $PSScriptRoot 'Public'
$privatePath = Join-Path $PSScriptRoot 'Private'

# Import Public functions
if (Test-Path $publicPath) {
    Get-ChildItem -Path $publicPath -Filter *.ps1 -Recurse -ErrorAction Stop | ForEach-Object {
        . $_.FullName
    }
}

# Import Private helpers
if (Test-Path $privatePath) {
    Get-ChildItem -Path $privatePath -Filter *.ps1 -Recurse -ErrorAction Stop | ForEach-Object {
        . $_.FullName
    }
}

# Export only Public functions
if (Test-Path $publicPath) {
    $publicFunctions = Get-ChildItem -Path $publicPath -Filter *.ps1 -Recurse |
        Select-Object -ExpandProperty BaseName

    Export-ModuleMember -Function $publicFunctions
}
