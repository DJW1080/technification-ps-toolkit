namespace TechnificationToolboxApp.Models;

public sealed class AutoRepairStatus
{
    public AutoRepairStatus(bool isAdministrator, string logsPath, string reportsPath)
    {
        IsAdministrator = isAdministrator;
        LogsPath = logsPath;
        ReportsPath = reportsPath;
    }

    public bool IsAdministrator { get; }
    public string LogsPath { get; }
    public string ReportsPath { get; }
}
