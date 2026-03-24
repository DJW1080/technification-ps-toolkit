namespace TechnificationToolboxApp.Models;

public sealed class WingetEnvironmentStatus
{
    public WingetEnvironmentStatus(bool isAvailable, string executablePath, string reportsPath)
    {
        IsAvailable = isAvailable;
        ExecutablePath = executablePath;
        ReportsPath = reportsPath;
    }

    public bool IsAvailable { get; }
    public string ExecutablePath { get; }
    public string ReportsPath { get; }
}
