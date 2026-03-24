namespace TechnificationToolboxApp.Models;

public sealed class EventLogCleanerStatus
{
    public EventLogCleanerStatus(bool isAdministrator, bool dryRunEnabled, bool skipCounts, int exactExclusionCount, int wildcardExclusionCount, string configPath, string logsPath, string reportsPath)
    {
        IsAdministrator = isAdministrator;
        DryRunEnabled = dryRunEnabled;
        SkipCounts = skipCounts;
        ExactExclusionCount = exactExclusionCount;
        WildcardExclusionCount = wildcardExclusionCount;
        ConfigPath = configPath;
        LogsPath = logsPath;
        ReportsPath = reportsPath;
    }

    public bool IsAdministrator { get; }
    public bool DryRunEnabled { get; }
    public bool SkipCounts { get; }
    public int ExactExclusionCount { get; }
    public int WildcardExclusionCount { get; }
    public string ConfigPath { get; }
    public string LogsPath { get; }
    public string ReportsPath { get; }
}
