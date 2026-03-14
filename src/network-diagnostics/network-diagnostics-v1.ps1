<#
    Script Name     : network-diagnostics-v1.ps1
    Description     : Network Diagnostics Suite for Technification Toolbox
    Author          : Dean John Weiniger
    Version         : 1.1
    Type            : PowerShell 7
    Date            : 2026-03-14
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path (Split-Path $PSScriptRoot -Parent) 'shared\menu-core.ps1')

function Write-Info($Message) { Write-Host "[INFO]  $Message" -ForegroundColor Cyan }
function Write-Good($Message) { Write-Host "[GOOD]  $Message" -ForegroundColor Green }
function Write-Bad($Message) { Write-Host "[FAIL]  $Message" -ForegroundColor Red }
function Write-Warn($Message) { Write-Host "[WARN]  $Message" -ForegroundColor Yellow }

function Format-Latency {
    param($Value)
    if ($null -eq $Value) { return 'N/A' }
    "{0} ms" -f $Value
}

function Show-NetworkSummary {
    Write-Host ''
    Write-Host '================ NETWORK SUMMARY ================' -ForegroundColor White
    try {
        $adapters = Get-NetIPConfiguration | Where-Object { $_.IPv4Address -or $_.IPv6Address }
        foreach ($adapter in $adapters) {
            Write-Host ("Adapter      : {0}" -f $adapter.InterfaceAlias) -ForegroundColor Cyan
            Write-Host ("IPv4 Address : {0}" -f ($(if ($adapter.IPv4Address) { ($adapter.IPv4Address | Select-Object -ExpandProperty IPAddress) -join ', ' } else { 'None' }))) -ForegroundColor DarkGray
            Write-Host ("Gateway      : {0}" -f ($(if ($adapter.IPv4DefaultGateway) { ($adapter.IPv4DefaultGateway | Select-Object -ExpandProperty NextHop) -join ', ' } else { 'None' }))) -ForegroundColor DarkGray
            Write-Host ("DNS          : {0}" -f ($(if ($adapter.DNSServer.ServerAddresses) { $adapter.DNSServer.ServerAddresses -join ', ' } else { 'None' }))) -ForegroundColor DarkGray
            Write-Host ''
        }
    }
    catch {
        Write-Bad $_.Exception.Message
    }
}

function Test-GatewayReachability {
    Write-Host ''
    Write-Host '================ GATEWAY TEST ===================' -ForegroundColor White
    try {
        $gateways = Get-NetRoute -DestinationPrefix '0.0.0.0/0' -ErrorAction SilentlyContinue |
            Sort-Object RouteMetric |
            Select-Object -ExpandProperty NextHop -Unique
        if (-not $gateways) {
            Write-Warn 'No default gateway found.'
            return
        }
        foreach ($gateway in $gateways) {
            $ping = Test-Connection -ComputerName $gateway -Count 2 -ErrorAction SilentlyContinue
            if ($ping) {
                $avg = [math]::Round((($ping | Measure-Object -Property Latency -Average).Average), 2)
                Write-Good ("Gateway {0} reachable ({1})" -f $gateway, (Format-Latency -Value $avg))
            }
            else {
                Write-Bad "Gateway $gateway is not responding."
            }
        }
    }
    catch {
        Write-Bad $_.Exception.Message
    }
}

function Test-DnsResolution {
    $target = Read-Host 'Enter hostname to resolve [default: openai.com]'
    if ([string]::IsNullOrWhiteSpace($target)) { $target = 'openai.com' }
    Write-Host ''
    Write-Host '================ DNS RESOLUTION ==================' -ForegroundColor White
    try {
        $result = Resolve-DnsName -Name $target -ErrorAction Stop
        $addresses = $result | Where-Object { $_.IPAddress } | Select-Object -ExpandProperty IPAddress -Unique
        if ($addresses) { Write-Good ("Resolved {0} -> {1}" -f $target, ($addresses -join ', ')) }
        else { Write-Warn "No address records returned for $target." }
    }
    catch {
        Write-Bad $_.Exception.Message
    }
}

function Test-InternetConnectivity {
    $target = Read-Host 'Enter host to ping [default: 1.1.1.1]'
    if ([string]::IsNullOrWhiteSpace($target)) { $target = '1.1.1.1' }
    Write-Host ''
    Write-Host '=============== INTERNET CONNECTIVITY ============' -ForegroundColor White
    try {
        $ping = Test-Connection -ComputerName $target -Count 4 -ErrorAction SilentlyContinue
        if ($ping) {
            $avg = [math]::Round((($ping | Measure-Object -Property Latency -Average).Average), 2)
            Write-Good ("{0} responded ({1})" -f $target, (Format-Latency -Value $avg))
        }
        else {
            Write-Bad "$target did not respond to ping."
        }
    }
    catch {
        Write-Bad $_.Exception.Message
    }
}

function Show-ActiveConnections {
    Write-Host ''
    Write-Host '================ ACTIVE TCP CONNECTIONS ==========' -ForegroundColor White
    try {
        Get-NetTCPConnection -State Listen, Established -ErrorAction Stop |
            Sort-Object LocalPort, RemotePort |
            Select-Object -First 40 |
            Format-Table State, LocalAddress, LocalPort, RemoteAddress, RemotePort, OwningProcess -AutoSize
    }
    catch {
        Write-Bad $_.Exception.Message
    }
}

function Invoke-PortScan {
    param(
        [Parameter(Mandatory)][string]$Target,
        [Parameter(Mandatory)][int[]]$Ports
    )

    Write-Host ''
    Write-Host '================ TCP PORT SCAN ===================' -ForegroundColor White
    Write-Host ("Target: {0}" -f $Target) -ForegroundColor Cyan

    $results = foreach ($port in $Ports) {
        try {
            $test = Test-NetConnection -ComputerName $Target -Port $port -WarningAction SilentlyContinue -InformationLevel Quiet
            [pscustomobject]@{
                Port = $port
                Status = if ($test) { 'Open' } else { 'Closed/Filtered' }
            }
        }
        catch {
            [pscustomobject]@{
                Port = $port
                Status = 'Error'
            }
        }
    }

    foreach ($result in $results) {
        $color = switch ($result.Status) {
            'Open' { 'Green' }
            'Closed/Filtered' { 'Yellow' }
            default { 'Red' }
        }
        Write-Host ("Port {0,-5} : {1}" -f $result.Port, $result.Status) -ForegroundColor $color
    }

    $openPorts = @($results | Where-Object Status -eq 'Open' | Select-Object -ExpandProperty Port)
    Write-Host ''
    if ($openPorts.Count -gt 0) { Write-Good ("Open ports found: {0}" -f ($openPorts -join ', ')) }
    else { Write-Warn 'No open ports found in the selected set.' }
}

function Start-CommonPortScan {
    $target = Read-Host 'Enter host or IP to scan [default: localhost]'
    if ([string]::IsNullOrWhiteSpace($target)) { $target = 'localhost' }
    Invoke-PortScan -Target $target -Ports @(20, 21, 22, 23, 25, 53, 80, 110, 123, 135, 139, 143, 443, 445, 3389, 5985, 5986, 8080)
}

function Start-CustomPortScan {
    $target = Read-Host 'Enter host or IP to scan'
    if ([string]::IsNullOrWhiteSpace($target)) { Write-Warn 'Target is required.'; return }
    $portInput = Read-Host 'Enter ports separated by commas (example: 22,80,443,3389)'
    if ([string]::IsNullOrWhiteSpace($portInput)) { Write-Warn 'At least one port is required.'; return }

    $ports = @()
    foreach ($part in ($portInput -split ',')) {
        $trimmed = $part.Trim()
        if ($trimmed -match '^\d+$') {
            $port = [int]$trimmed
            if ($port -ge 1 -and $port -le 65535) { $ports += $port }
        }
    }
    $ports = @($ports | Select-Object -Unique)
    if (-not $ports) { Write-Warn 'No valid ports were provided.'; return }
    Invoke-PortScan -Target $target -Ports $ports
}

function Export-NetworkReport {
    $reportPath = Join-Path $env:LOCALAPPDATA ('Technification-Network-Report-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.txt')
    Write-Info "Creating network report: $reportPath"
    @(
        '================ NETWORK DIAGNOSTICS REPORT ================',
        ('Generated: {0}' -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss')),
        ('Computer : {0}' -f $env:COMPUTERNAME),
        ''
    ) | Set-Content -Path $reportPath
    Add-Content -Path $reportPath -Value '--- IPCONFIG /ALL ---'
    (ipconfig /all | Out-String) | Add-Content -Path $reportPath
    Add-Content -Path $reportPath -Value "`r`n--- ROUTES ---"
    (route print | Out-String) | Add-Content -Path $reportPath
    Add-Content -Path $reportPath -Value "`r`n--- NET ADAPTERS ---"
    (Get-NetAdapter | Format-Table Name, Status, LinkSpeed, MacAddress -AutoSize | Out-String) | Add-Content -Path $reportPath
    Add-Content -Path $reportPath -Value "`r`n--- TCP CONNECTIONS ---"
    (Get-NetTCPConnection -ErrorAction SilentlyContinue | Select-Object -First 100 | Format-Table State, LocalAddress, LocalPort, RemoteAddress, RemotePort -AutoSize | Out-String) | Add-Content -Path $reportPath
    Write-Good "Network report saved to: $reportPath"
}

function Start-QuickHealthCheck {
    Show-NetworkSummary
    Test-GatewayReachability
    Test-DnsResolution
    Test-InternetConnectivity
}

function Show-NetworkDiagnosticsPage {
    $header = @(
        '============================================================',
        '================= NETWORK DIAGNOSTICS SUITE ================',
        '============================================================'
    )
    Show-MenuPage -Title 'Network Diagnostics Menu' -Items (& $script:GetNetworkItems) -HeaderLines $header
}

$script:GetNetworkItems = {
    @(
        (New-MenuItem -Key '1' -Label 'Show Network Summary' -Action { Show-NetworkSummary } -PauseAfter $true)
        (New-MenuItem -Key '2' -Label 'Test Default Gateway Reachability' -Action { Test-GatewayReachability } -PauseAfter $true)
        (New-MenuItem -Key '3' -Label 'Test DNS Resolution' -Action { Test-DnsResolution } -PauseAfter $true)
        (New-MenuItem -Key '4' -Label 'Test Internet Connectivity' -Action { Test-InternetConnectivity } -PauseAfter $true)
        (New-MenuItem -Key '5' -Label 'Show Active TCP Connections' -Action { Show-ActiveConnections } -PauseAfter $true)
        (New-MenuItem -Key '6' -Label 'Scan Common TCP Ports' -Action { Start-CommonPortScan } -PauseAfter $true -Color 'DarkYellow')
        (New-MenuItem -Key '7' -Label 'Scan Custom TCP Ports' -Action { Start-CustomPortScan } -PauseAfter $true -Color 'DarkYellow')
        (New-MenuItem -Key '8' -Label 'Export Network Report' -Action { Export-NetworkReport } -PauseAfter $true -Color 'Cyan')
        (New-MenuItem -Key '9' -Label 'Run Quick Health Check' -Action { Start-QuickHealthCheck } -PauseAfter $true -Color 'Cyan')
        (New-MenuItem -Key '0' -Label 'Return To Toolbox' -Action { Write-Host 'Returning to Technification Toolbox...' -ForegroundColor Blue; return '__EXIT_MENU__' } -Color 'Blue')
    )
}

Invoke-MenuLoop -Render { Show-NetworkDiagnosticsPage } -GetItems $script:GetNetworkItems
