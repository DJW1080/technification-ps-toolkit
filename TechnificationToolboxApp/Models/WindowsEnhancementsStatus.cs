namespace TechnificationToolboxApp.Models;

public sealed class WindowsEnhancementsStatus
{
    public WindowsEnhancementsStatus(bool isAdministrator, bool? hibernationEnabled, bool contextMenuInstalled, string logsPath, string reportsPath)
    {
        IsAdministrator = isAdministrator;
        HibernationEnabled = hibernationEnabled;
        ContextMenuInstalled = contextMenuInstalled;
        LogsPath = logsPath;
        ReportsPath = reportsPath;
    }

    public bool IsAdministrator { get; }
    public bool? HibernationEnabled { get; }
    public bool ContextMenuInstalled { get; }
    public string LogsPath { get; }
    public string ReportsPath { get; }
}
