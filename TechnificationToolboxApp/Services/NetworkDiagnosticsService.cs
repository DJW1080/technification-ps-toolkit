using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TechnificationToolboxApp.Models;

namespace TechnificationToolboxApp.Services;

public sealed class NetworkDiagnosticsService
{
    private static readonly int[] CommonPorts =
    {
        20, 21, 22, 23, 25, 53, 80, 110, 123, 135, 139, 143, 443, 445, 3389, 5985, 5986, 8080
    };

    private readonly string _logsPath;
    private readonly string _reportsPath;

    public NetworkDiagnosticsService(string reportsPath, string logsPath)
    {
        _reportsPath = reportsPath;
        _logsPath = logsPath;
    }

    public NetworkEnvironmentStatus GetStatus()
    {
        return new NetworkEnvironmentStatus(
            GetAdapterSnapshots().Count,
            _logsPath,
            _reportsPath,
            "openai.com",
            "1.1.1.1",
            "localhost");
    }

    public Task<NetworkDiagnosticsResult> ShowNetworkSummaryAsync(IProgress<string>? progress)
    {
        return Task.Run(() =>
        {
            const string actionName = "Show network summary";
            var output = new List<string>();

            void Write(string line)
            {
                output.Add(line);
                progress?.Report(line);
            }

            try
            {
                Write("[INFO] Gathering network adapter summary...");
                Write("================ NETWORK SUMMARY ================");

                List<AdapterSnapshot> adapters = GetAdapterSnapshots();
                if (adapters.Count == 0)
                {
                    Write("[WARN] No adapters with IP configuration were found.");
                    return CreateResult(actionName, false, "No configured adapters found.", output);
                }

                foreach (AdapterSnapshot adapter in adapters)
                {
                    Write("Adapter      : " + adapter.Name);
                    Write("Status       : " + adapter.Status);
                    Write("IPv4 Address : " + adapter.IPv4Addresses);
                    Write("IPv6 Address : " + adapter.IPv6Addresses);
                    Write("Gateway      : " + adapter.Gateways);
                    Write("DNS          : " + adapter.DnsServers);
                    Write(string.Empty);
                }

                Write("[GOOD] Network summary completed.");
                return CreateResult(actionName, true, adapters.Count + " adapter(s) with configured addresses.", output);
            }
            catch (Exception ex)
            {
                Write("[FAIL] " + ex.Message);
                return CreateResult(actionName, false, ex.Message, output);
            }
        });
    }

    public async Task<NetworkDiagnosticsResult> TestDefaultGatewayReachabilityAsync(IProgress<string>? progress)
    {
        const string actionName = "Test default gateway reachability";
        var output = new List<string>();

        void Write(string line)
        {
            output.Add(line);
            progress?.Report(line);
        }

        try
        {
            Write("[INFO] Testing default gateway reachability...");
            Write("================ GATEWAY TEST ===================");

            List<IPAddress> gateways = GetGatewayAddresses();
            if (gateways.Count == 0)
            {
                Write("[WARN] No default gateway found.");
                return CreateResult(actionName, false, "No default gateway found.", output);
            }

            int reachableCount = 0;
            foreach (IPAddress gateway in gateways)
            {
                IReadOnlyList<long> latencies = await PingTargetAsync(gateway.ToString(), 2, 2000);
                if (latencies.Count > 0)
                {
                    reachableCount++;
                    Write("[GOOD] Gateway " + gateway + " reachable (" + FormatLatency(latencies) + ")");
                }
                else
                {
                    Write("[FAIL] Gateway " + gateway + " is not responding.");
                }
            }

            bool succeeded = reachableCount == gateways.Count;
            string summary = reachableCount + " of " + gateways.Count + " gateway(s) responded.";
            return CreateResult(actionName, succeeded, summary, output);
        }
        catch (Exception ex)
        {
            Write("[FAIL] " + ex.Message);
            return CreateResult(actionName, false, ex.Message, output);
        }
    }

    public async Task<NetworkDiagnosticsResult> TestDnsResolutionAsync(string? target, IProgress<string>? progress)
    {
        const string actionName = "Test DNS resolution";
        var output = new List<string>();
        string host = string.IsNullOrWhiteSpace(target) ? "openai.com" : target.Trim();

        void Write(string line)
        {
            output.Add(line);
            progress?.Report(line);
        }

        try
        {
            Write("[INFO] Resolving DNS for " + host + "...");
            Write("================ DNS RESOLUTION =================");

            IPAddress[] addresses = await Dns.GetHostAddressesAsync(host);
            string[] distinctAddresses = addresses
                .Select(address => address.ToString())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (distinctAddresses.Length == 0)
            {
                Write("[WARN] No address records returned for " + host + ".");
                return CreateResult(actionName, false, "No DNS records returned for " + host + ".", output);
            }

            Write("[GOOD] Resolved " + host + " -> " + string.Join(", ", distinctAddresses));
            return CreateResult(actionName, true, distinctAddresses.Length + " address(es) returned.", output);
        }
        catch (Exception ex)
        {
            Write("[FAIL] " + ex.Message);
            return CreateResult(actionName, false, ex.Message, output);
        }
    }

    public async Task<NetworkDiagnosticsResult> TestInternetConnectivityAsync(string? target, IProgress<string>? progress)
    {
        const string actionName = "Test internet connectivity";
        var output = new List<string>();
        string host = string.IsNullOrWhiteSpace(target) ? "1.1.1.1" : target.Trim();

        void Write(string line)
        {
            output.Add(line);
            progress?.Report(line);
        }

        try
        {
            Write("[INFO] Testing connectivity to " + host + "...");
            Write("=============== INTERNET CONNECTIVITY ===========");

            IReadOnlyList<long> latencies = await PingTargetAsync(host, 4, 2500);
            if (latencies.Count == 0)
            {
                Write("[FAIL] " + host + " did not respond to ping.");
                return CreateResult(actionName, false, host + " did not respond to ping.", output);
            }

            Write("[GOOD] " + host + " responded (" + FormatLatency(latencies) + ")");
            return CreateResult(actionName, true, host + " responded to ping.", output);
        }
        catch (Exception ex)
        {
            Write("[FAIL] " + ex.Message);
            return CreateResult(actionName, false, ex.Message, output);
        }
    }

    public Task<NetworkDiagnosticsResult> ShowActiveConnectionsAsync(IProgress<string>? progress)
    {
        return Task.Run(() =>
        {
            const string actionName = "Show active TCP connections";
            var output = new List<string>();

            void Write(string line)
            {
                output.Add(line);
                progress?.Report(line);
            }

            try
            {
                Write("[INFO] Enumerating active TCP connections...");
                Write("============== ACTIVE TCP CONNECTIONS ===========");

                IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
                List<ConnectionSnapshot> rows = new List<ConnectionSnapshot>();

                foreach (IPEndPoint endpoint in properties.GetActiveTcpListeners())
                {
                    rows.Add(new ConnectionSnapshot("Listen", endpoint.Address.ToString(), endpoint.Port.ToString(), "*"));
                }

                foreach (TcpConnectionInformation connection in properties.GetActiveTcpConnections())
                {
                    if (connection.State != TcpState.Established)
                    {
                        continue;
                    }

                    rows.Add(new ConnectionSnapshot(
                        connection.State.ToString(),
                        connection.LocalEndPoint.Address.ToString(),
                        connection.LocalEndPoint.Port.ToString(),
                        connection.RemoteEndPoint.Address + ":" + connection.RemoteEndPoint.Port));
                }

                List<ConnectionSnapshot> topRows = rows
                    .OrderBy(row => row.LocalPort, StringComparer.Ordinal)
                    .ThenBy(row => row.State, StringComparer.Ordinal)
                    .Take(40)
                    .ToList();

                if (topRows.Count == 0)
                {
                    Write("[WARN] No listening or established TCP connections were found.");
                    return CreateResult(actionName, true, "No active TCP connections found.", output);
                }

                foreach (string line in FormatConnectionTable(topRows))
                {
                    Write(line);
                }

                Write("[GOOD] Displayed " + topRows.Count + " active TCP connection row(s).");
                return CreateResult(actionName, true, topRows.Count + " active TCP connection row(s) listed.", output);
            }
            catch (Exception ex)
            {
                Write("[FAIL] " + ex.Message);
                return CreateResult(actionName, false, ex.Message, output);
            }
        });
    }

    public Task<NetworkDiagnosticsResult> ScanCommonPortsAsync(string? target, IProgress<string>? progress)
    {
        string host = string.IsNullOrWhiteSpace(target) ? "localhost" : target.Trim();
        return ScanPortsAsync(host, CommonPorts, "Scan common TCP ports", progress);
    }

    public Task<NetworkDiagnosticsResult> ScanCustomPortsAsync(string target, IReadOnlyList<int> ports, IProgress<string>? progress)
    {
        return ScanPortsAsync(target.Trim(), ports, "Scan custom TCP ports", progress);
    }

    public async Task<NetworkDiagnosticsResult> ExportReportAsync(IProgress<string>? progress)
    {
        const string actionName = "Export network report";
        var output = new List<string>();

        void Write(string line)
        {
            output.Add(line);
            progress?.Report(line);
        }

        try
        {
            Write("[INFO] Creating network diagnostics report...");
            Directory.CreateDirectory(_reportsPath);

            NetworkDiagnosticsResult summary = await ShowNetworkSummaryAsync(null);
            NetworkDiagnosticsResult connections = await ShowActiveConnectionsAsync(null);
            string ipconfigText = await RunSystemCommandAsync("ipconfig.exe", "/all");
            string routesText = await RunSystemCommandAsync("route.exe", "print");

            string reportPath = Path.Combine(_reportsPath, BuildReportFileName());
            var builder = new StringBuilder();
            builder.AppendLine("================ NETWORK DIAGNOSTICS REPORT ================");
            builder.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            builder.AppendLine("Computer : " + Environment.MachineName);
            builder.AppendLine();
            AppendSection(builder, "NETWORK SUMMARY", summary.OutputText);
            AppendSection(builder, "ACTIVE TCP CONNECTIONS", connections.OutputText);
            AppendSection(builder, "IPCONFIG /ALL", ipconfigText);
            AppendSection(builder, "ROUTE PRINT", routesText);

            File.WriteAllText(reportPath, builder.ToString().TrimEnd() + Environment.NewLine, Encoding.UTF8);

            Write("Report : " + reportPath);
            Write("[GOOD] Network diagnostics report exported.");

            return CreateResult(actionName, true, "Network report exported.", output, new[] { reportPath });
        }
        catch (Exception ex)
        {
            Write("[FAIL] " + ex.Message);
            return CreateResult(actionName, false, ex.Message, output);
        }
    }

    public async Task<NetworkDiagnosticsResult> RunQuickHealthCheckAsync(string? dnsTarget, string? pingTarget, IProgress<string>? progress)
    {
        const string actionName = "Run quick health check";
        var reportOutputs = new List<string>();
        var results = new List<NetworkDiagnosticsResult>();

        progress?.Report("[INFO] Running quick health check...");

        NetworkDiagnosticsResult summary = await ShowNetworkSummaryAsync(progress);
        results.Add(summary);
        reportOutputs.Add(summary.OutputText);

        progress?.Report(string.Empty);
        NetworkDiagnosticsResult gateway = await TestDefaultGatewayReachabilityAsync(progress);
        results.Add(gateway);
        reportOutputs.Add(gateway.OutputText);

        progress?.Report(string.Empty);
        NetworkDiagnosticsResult dns = await TestDnsResolutionAsync(dnsTarget, progress);
        results.Add(dns);
        reportOutputs.Add(dns.OutputText);

        progress?.Report(string.Empty);
        NetworkDiagnosticsResult internet = await TestInternetConnectivityAsync(pingTarget, progress);
        results.Add(internet);
        reportOutputs.Add(internet.OutputText);

        bool succeeded = results.All(result => result.Succeeded);
        string summaryText = succeeded ? "Quick health check completed successfully." : "Quick health check found one or more issues.";
        progress?.Report(succeeded ? "[GOOD] " + summaryText : "[WARN] " + summaryText);

        return new NetworkDiagnosticsResult(
            actionName,
            succeeded,
            summaryText,
            string.Join(Environment.NewLine + Environment.NewLine, reportOutputs.Where(text => !string.IsNullOrWhiteSpace(text))).TrimEnd(),
            Array.Empty<string>());
    }

    private async Task<NetworkDiagnosticsResult> ScanPortsAsync(string target, IReadOnlyList<int> ports, string actionName, IProgress<string>? progress)
    {
        var output = new List<string>();

        void Write(string line)
        {
            output.Add(line);
            progress?.Report(line);
        }

        try
        {
            if (string.IsNullOrWhiteSpace(target))
            {
                Write("[FAIL] A host name or IP address is required.");
                return CreateResult(actionName, false, "Target is required.", output);
            }

            int[] normalizedPorts = ports
                .Where(port => port >= 1 && port <= 65535)
                .Distinct()
                .OrderBy(port => port)
                .ToArray();

            if (normalizedPorts.Length == 0)
            {
                Write("[FAIL] At least one valid TCP port is required.");
                return CreateResult(actionName, false, "No valid ports were provided.", output);
            }

            Write("[INFO] Scanning TCP ports...");
            Write("================ TCP PORT SCAN ==================");
            Write("Target: " + target);

            List<int> openPorts = new List<int>();
            foreach (int port in normalizedPorts)
            {
                bool isOpen = await CanConnectAsync(target, port, TimeSpan.FromMilliseconds(900));
                if (isOpen)
                {
                    openPorts.Add(port);
                }

                string status = isOpen ? "Open" : "Closed/Filtered";
                Write("Port " + port.ToString().PadRight(5) + " : " + status);
            }

            Write(string.Empty);
            if (openPorts.Count > 0)
            {
                Write("[GOOD] Open ports found: " + string.Join(", ", openPorts));
            }
            else
            {
                Write("[WARN] No open ports found in the selected set.");
            }

            return CreateResult(actionName, true, openPorts.Count + " open port(s) detected.", output);
        }
        catch (Exception ex)
        {
            Write("[FAIL] " + ex.Message);
            return CreateResult(actionName, false, ex.Message, output);
        }
    }

    private static NetworkDiagnosticsResult CreateResult(string actionName, bool succeeded, string summaryText, List<string> outputLines, IReadOnlyList<string>? reportPaths = null)
    {
        return new NetworkDiagnosticsResult(
            actionName,
            succeeded,
            summaryText,
            string.Join(Environment.NewLine, outputLines).TrimEnd(),
            reportPaths ?? Array.Empty<string>());
    }

    private static List<AdapterSnapshot> GetAdapterSnapshots()
    {
        List<AdapterSnapshot> adapters = new List<AdapterSnapshot>();

        foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces().OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback)
            {
                continue;
            }

            IPInterfaceProperties properties;
            try
            {
                properties = adapter.GetIPProperties();
            }
            catch
            {
                continue;
            }

            string ipv4Addresses = JoinUnicastAddresses(properties.UnicastAddresses, AddressFamily.InterNetwork);
            string ipv6Addresses = JoinUnicastAddresses(properties.UnicastAddresses, AddressFamily.InterNetworkV6);
            if (ipv4Addresses == "None" && ipv6Addresses == "None")
            {
                continue;
            }

            adapters.Add(new AdapterSnapshot(
                adapter.Name,
                adapter.OperationalStatus.ToString(),
                ipv4Addresses,
                ipv6Addresses,
                JoinAddresses(properties.GatewayAddresses.Select(gateway => gateway.Address)),
                JoinAddresses(properties.DnsAddresses)));
        }

        return adapters;
    }

    private static List<IPAddress> GetGatewayAddresses()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var gateways = new List<IPAddress>();

        foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
        {
            IPInterfaceProperties properties;
            try
            {
                properties = adapter.GetIPProperties();
            }
            catch
            {
                continue;
            }

            foreach (GatewayIPAddressInformation gateway in properties.GatewayAddresses)
            {
                IPAddress address = gateway.Address;
                if (IPAddress.Any.Equals(address) || IPAddress.IPv6Any.Equals(address))
                {
                    continue;
                }

                string text = address.ToString();
                if (text == "0.0.0.0" || text == "::")
                {
                    continue;
                }

                if (seen.Add(text))
                {
                    gateways.Add(address);
                }
            }
        }

        return gateways;
    }

    private static async Task<IReadOnlyList<long>> PingTargetAsync(string target, int attempts, int timeoutMilliseconds)
    {
        var latencies = new List<long>();
        using var ping = new Ping();

        for (int attempt = 0; attempt < attempts; attempt++)
        {
            try
            {
                PingReply reply = await ping.SendPingAsync(target, timeoutMilliseconds);
                if (reply.Status == IPStatus.Success)
                {
                    latencies.Add(reply.RoundtripTime);
                }
            }
            catch
            {
                // Keep the method tolerant and let the caller summarize failures.
            }
        }

        return latencies;
    }

    private static string FormatLatency(IReadOnlyList<long> latencies)
    {
        if (latencies.Count == 0)
        {
            return "N/A";
        }

        double average = latencies.Average();
        return average.ToString("0.##") + " ms";
    }

    private static IEnumerable<string> FormatConnectionTable(IReadOnlyList<ConnectionSnapshot> rows)
    {
        string[] headers = { "State", "Local Address", "Port", "Remote Endpoint" };
        int stateWidth = Math.Max(headers[0].Length, rows.Max(row => row.State.Length));
        int localWidth = Math.Max(headers[1].Length, rows.Max(row => row.LocalAddress.Length));
        int portWidth = Math.Max(headers[2].Length, rows.Max(row => row.LocalPort.Length));
        int remoteWidth = Math.Max(headers[3].Length, rows.Max(row => row.RemoteEndpoint.Length));

        yield return headers[0].PadRight(stateWidth) + "  "
            + headers[1].PadRight(localWidth) + "  "
            + headers[2].PadRight(portWidth) + "  "
            + headers[3].PadRight(remoteWidth);
        yield return new string('-', stateWidth) + "  "
            + new string('-', localWidth) + "  "
            + new string('-', portWidth) + "  "
            + new string('-', remoteWidth);

        foreach (ConnectionSnapshot row in rows)
        {
            yield return row.State.PadRight(stateWidth) + "  "
                + row.LocalAddress.PadRight(localWidth) + "  "
                + row.LocalPort.PadRight(portWidth) + "  "
                + row.RemoteEndpoint.PadRight(remoteWidth);
        }
    }

    private static async Task<bool> CanConnectAsync(string target, int port, TimeSpan timeout)
    {
        using var client = new TcpClient();
        try
        {
            Task connectTask = client.ConnectAsync(target, port);
            Task completedTask = await Task.WhenAny(connectTask, Task.Delay(timeout));
            if (!ReferenceEquals(completedTask, connectTask))
            {
                return false;
            }

            await connectTask;
            return client.Connected;
        }
        catch
        {
            return false;
        }
    }

    private static string JoinUnicastAddresses(UnicastIPAddressInformationCollection addresses, AddressFamily family)
    {
        return JoinAddresses(addresses
            .Where(address => address.Address.AddressFamily == family)
            .Select(address => address.Address));
    }

    private static string JoinAddresses(IEnumerable<IPAddress> addresses)
    {
        string[] values = addresses
            .Select(address => address.ToString())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return values.Length == 0 ? "None" : string.Join(", ", values);
    }

    private static void AppendSection(StringBuilder builder, string title, string content)
    {
        builder.AppendLine("--- " + title + " ---");
        builder.AppendLine(string.IsNullOrWhiteSpace(content) ? "No output." : content.TrimEnd());
        builder.AppendLine();
    }

    private static string BuildReportFileName()
    {
        return "network-report-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt";
    }

    private static async Task<string> RunSystemCommandAsync(string fileName, string arguments)
    {
        var output = new StringBuilder();
        using var process = new Process();
        process.StartInfo.FileName = ResolveSystemCommand(fileName);
        process.StartInfo.Arguments = arguments;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;

        process.OutputDataReceived += (_, args) =>
        {
            if (args.Data != null)
            {
                output.AppendLine(args.Data);
            }
        };
        process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data != null)
            {
                output.AppendLine(args.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();
        process.WaitForExit();

        string text = output.ToString().TrimEnd();
        return string.IsNullOrWhiteSpace(text) ? fileName + " returned no output." : text;
    }

    private static string ResolveSystemCommand(string fileName)
    {
        string systemPath = Path.Combine(Environment.SystemDirectory, fileName);
        return File.Exists(systemPath) ? systemPath : fileName;
    }

    private sealed class AdapterSnapshot
    {
        public AdapterSnapshot(string name, string status, string ipv4Addresses, string ipv6Addresses, string gateways, string dnsServers)
        {
            Name = name;
            Status = status;
            IPv4Addresses = ipv4Addresses;
            IPv6Addresses = ipv6Addresses;
            Gateways = gateways;
            DnsServers = dnsServers;
        }

        public string Name { get; }
        public string Status { get; }
        public string IPv4Addresses { get; }
        public string IPv6Addresses { get; }
        public string Gateways { get; }
        public string DnsServers { get; }
    }

    private sealed class ConnectionSnapshot
    {
        public ConnectionSnapshot(string state, string localAddress, string localPort, string remoteEndpoint)
        {
            State = state;
            LocalAddress = localAddress;
            LocalPort = localPort;
            RemoteEndpoint = remoteEndpoint;
        }

        public string State { get; }
        public string LocalAddress { get; }
        public string LocalPort { get; }
        public string RemoteEndpoint { get; }
    }
}
