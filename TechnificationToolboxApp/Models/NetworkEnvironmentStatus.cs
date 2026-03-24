namespace TechnificationToolboxApp.Models;

public sealed class NetworkEnvironmentStatus
{
    public NetworkEnvironmentStatus(int adapterCount, string logsPath, string reportsPath, string defaultDnsHost, string defaultPingTarget, string defaultScanTarget)
    {
        AdapterCount = adapterCount;
        LogsPath = logsPath;
        ReportsPath = reportsPath;
        DefaultDnsHost = defaultDnsHost;
        DefaultPingTarget = defaultPingTarget;
        DefaultScanTarget = defaultScanTarget;
    }

    public int AdapterCount { get; }
    public string LogsPath { get; }
    public string ReportsPath { get; }
    public string DefaultDnsHost { get; }
    public string DefaultPingTarget { get; }
    public string DefaultScanTarget { get; }
}
