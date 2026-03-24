namespace TechnificationToolboxApp.Models;

public sealed class UserProfileCleanupStatus
{
    public UserProfileCleanupStatus(int targetCount, int exclusionCount, int lockedItemCount, string logsPath, string reportsPath)
    {
        TargetCount = targetCount;
        ExclusionCount = exclusionCount;
        LockedItemCount = lockedItemCount;
        LogsPath = logsPath;
        ReportsPath = reportsPath;
    }

    public int TargetCount { get; }
    public int ExclusionCount { get; }
    public int LockedItemCount { get; }
    public string LogsPath { get; }
    public string ReportsPath { get; }
}
