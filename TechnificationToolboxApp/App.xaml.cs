using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.UI.Xaml;
using TechnificationToolboxApp.Models;
using TechnificationToolboxApp.Services;

namespace TechnificationToolboxApp;

public partial class App : Application
{
    private Window? _window;

    public static string DisplayName { get; } = "Technification Toolbox";
    public static string WorkspaceRoot { get; } = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    public static string ScriptsRoot { get; } = Path.Combine(AppContext.BaseDirectory, "Scripts");
    public static ToolkitLaunchService Launcher { get; } = new ToolkitLaunchService(Path.Combine(AppContext.BaseDirectory, "Scripts"));
    public static WingetMaintenanceService WingetMaintenance { get; } = new WingetMaintenanceService(Launcher.ReportsPath);
    public static NetworkDiagnosticsService NetworkDiagnostics { get; } = new NetworkDiagnosticsService(Launcher.ReportsPath, Launcher.LogsPath);
    public static AutoRepairService AutoRepair { get; } = new AutoRepairService(Launcher.ReportsPath, Launcher.LogsPath);
    public static UserProfileCleanupService UserProfileCleanup { get; } = new UserProfileCleanupService(Launcher.ReportsPath, Launcher.LogsPath);
    public static EventLogCleanerService EventLogCleaner { get; } = new EventLogCleanerService(Launcher.ReportsPath, Launcher.LogsPath);
    public static WindowsEnhancementsService WindowsEnhancements { get; } = new WindowsEnhancementsService(Launcher.ReportsPath, Launcher.LogsPath);

    public static IReadOnlyList<ToolModule> Modules { get; } = new List<ToolModule>
    {
        new ToolModule("1", "Windows Auto Repair", "v1.3", "DISM, SFC, cleanup, diagnostics, and repair tasks", @"src\windows-auto-repair\win-auto-repair-v1.ps1", true, "auto-repair"),
        new ToolModule("2", "User Profile Cleanup", "v2.2", "Deep scan and category-based cleanup for the current user profile", @"src\user-profile-cleanup\user-profile-cleanup-v2.ps1", false, "profile-cleanup"),
        new ToolModule("3", "Event Log Cleaner", "v2.2", "Event log cleanup with exclusions, config, and dry-run support", @"src\event-log-cleaner\event-log-cleaner-v2.ps1", true, "event-log"),
        new ToolModule("4", "Windows Enhancements", "v1.5", "Context-menu tools, hibernation controls, and safe cleanup", @"src\windows-enhancements\win-enhancements-v1.ps1", true, "enhancements"),
        new ToolModule("5", "Network Diagnostics Suite", "v1.2", "Connectivity checks, reports, and TCP port scanning", @"src\network-diagnostics\network-diagnostics-v1.ps1", false, "network"),
        new ToolModule("6", "Winget Maintenance", "v1.1", "Automated Winget source refresh, upgrade, and reporting actions", @"src\winget-maintenance\winget-maintenance-v1.ps1", false, "winget")
    };

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
