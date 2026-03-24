using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using TechnificationToolboxApp.Models;

namespace TechnificationToolboxApp.Services;

public sealed class AutoRepairService
{
    private readonly string _logsPath;
    private readonly string _reportsPath;

    public AutoRepairService(string reportsPath, string logsPath)
    {
        _reportsPath = reportsPath;
        _logsPath = logsPath;
    }

    public AutoRepairStatus GetStatus()
    {
        return new AutoRepairStatus(IsAdministrator(), _logsPath, _reportsPath);
    }

    public Task<NativeToolResult> RunRecommendedMaintenanceAsync(bool createRestorePoint, IProgress<string>? progress)
    {
        var steps = new List<Func<IProgress<string>, Task<NativeToolResult>>>();
        if (createRestorePoint)
        {
            steps.Add(p => CreateRestorePointAsync(p));
        }

        steps.Add(p => RunFullRepairAsync(false, p));
        steps.Add(p => RemoveTempFilesAsync(p));
        steps.Add(p => FlushDnsCacheAsync(p));

        return RunCompositeAsync("Recommended maintenance sequence", steps, progress);
    }

    public Task<NativeToolResult> RunFullRepairAsync(bool createRestorePoint, IProgress<string>? progress)
    {
        var steps = new List<Func<IProgress<string>, Task<NativeToolResult>>>();
        if (createRestorePoint)
        {
            steps.Add(p => CreateRestorePointAsync(p));
        }

        steps.Add(p => RunDismCheckHealthAsync(p));
        steps.Add(p => RunDismScanHealthAsync(p));
        steps.Add(p => RunDismRestoreHealthAsync(p));
        steps.Add(p => RunSfcAsync(p));
        steps.Add(p => RunSfcSecondPassAsync(p));
        steps.Add(p => RunAnalyzeComponentStoreAsync(p));
        steps.Add(p => RunComponentCleanupAsync(p));
        steps.Add(p => RunComponentCleanupResetBaseAsync(p));

        return RunCompositeAsync("Windows full repair", steps, progress);
    }

    public Task<NativeToolResult> RemoveTempFilesAsync(IProgress<string>? progress)
    {
        return RunPowerShellActionAsync(
            "Remove temporary files",
            @"
Remove-Item ""$env:TEMP\*"" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item 'C:\Windows\Temp\*' -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item 'C:\Windows\SoftwareDistribution\Download\*' -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item 'C:\Windows\Prefetch\*' -Recurse -Force -ErrorAction SilentlyContinue
Write-Output 'Temporary file cleanup completed.'
",
            true,
            progress);
    }

    public Task<NativeToolResult> RunDismCheckHealthAsync(IProgress<string>? progress)
    {
        return RunCommandActionAsync("DISM CheckHealth", "dism.exe", new[] { "/Online", "/Cleanup-Image", "/CheckHealth" }, true, progress);
    }

    public Task<NativeToolResult> RunDismScanHealthAsync(IProgress<string>? progress)
    {
        return RunCommandActionAsync("DISM ScanHealth", "dism.exe", new[] { "/Online", "/Cleanup-Image", "/ScanHealth" }, true, progress);
    }

    public Task<NativeToolResult> RunDismRestoreHealthAsync(IProgress<string>? progress)
    {
        return RunCommandActionAsync("DISM RestoreHealth", "dism.exe", new[] { "/Online", "/Cleanup-Image", "/RestoreHealth" }, true, progress);
    }

    public Task<NativeToolResult> RunComponentCleanupAsync(IProgress<string>? progress)
    {
        return RunCommandActionAsync("DISM Component Cleanup", "dism.exe", new[] { "/Online", "/Cleanup-Image", "/StartComponentCleanup" }, true, progress);
    }

    public Task<NativeToolResult> RunComponentCleanupResetBaseAsync(IProgress<string>? progress)
    {
        return RunCommandActionAsync("DISM Component Cleanup ResetBase", "dism.exe", new[] { "/Online", "/Cleanup-Image", "/StartComponentCleanup", "/ResetBase" }, true, progress);
    }

    public Task<NativeToolResult> ResetNetworkStackAsync(IProgress<string>? progress)
    {
        return RunPowerShellActionAsync(
            "Reset network stack",
            @"
netsh winsock reset
netsh int ip reset
",
            true,
            progress);
    }

    public Task<NativeToolResult> RepairWindowsUpdateAsync(IProgress<string>? progress)
    {
        return RunPowerShellActionAsync(
            "Repair Windows Update",
            @"
Stop-Service wuauserv -Force -ErrorAction SilentlyContinue
Stop-Service bits -Force -ErrorAction SilentlyContinue
Stop-Service cryptsvc -Force -ErrorAction SilentlyContinue
Remove-Item 'C:\Windows\SoftwareDistribution' -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item 'C:\Windows\System32\catroot2' -Recurse -Force -ErrorAction SilentlyContinue
Start-Service cryptsvc -ErrorAction SilentlyContinue
Start-Service wuauserv -ErrorAction SilentlyContinue
Start-Service bits -ErrorAction SilentlyContinue
Write-Output 'Windows Update repair sequence completed.'
",
            true,
            progress);
    }

    public Task<NativeToolResult> CreateRestorePointAsync(IProgress<string>? progress)
    {
        return RunPowerShellActionAsync(
            "Create restore point",
            @"
$ErrorActionPreference = 'Stop'
Enable-ComputerRestore -Drive 'C:\' -ErrorAction SilentlyContinue
Checkpoint-Computer -Description 'WinRepairPro' -RestorePointType 'MODIFY_SETTINGS'
Write-Output 'Restore point created.'
",
            true,
            progress);
    }

    public Task<NativeToolResult> RunDiagnosticsAsync(IProgress<string>? progress)
    {
        return RunPowerShellActionAsync(
            "Run system diagnostics",
            @"
systeminfo
''
Get-Volume | Format-Table DriveLetter, FileSystemLabel, FileSystem, SizeRemaining, Size -AutoSize
''
Get-PhysicalDisk -ErrorAction SilentlyContinue | Format-Table FriendlyName, HealthStatus, OperationalStatus, Size -AutoSize
''
Get-EventLog -LogName System -Newest 20 | Format-Table TimeGenerated, EntryType, Source, EventID, Message -Wrap
",
            false,
            progress,
            captureReport: true,
            reportPrefix: "diagnostics");
    }

    public Task<NativeToolResult> RunSfcAsync(IProgress<string>? progress)
    {
        return RunCommandActionAsync("System File Checker", "sfc.exe", new[] { "/scannow" }, true, progress);
    }

    public Task<NativeToolResult> FlushDnsCacheAsync(IProgress<string>? progress)
    {
        return RunPowerShellActionAsync(
            "Flush DNS cache",
            @"
ipconfig /flushdns
Clear-DnsClientCache -ErrorAction SilentlyContinue
",
            false,
            progress);
    }

    public Task<NativeToolResult> RunDiskCheckScanAsync(IProgress<string>? progress)
    {
        return RunCommandActionAsync("CHKDSK online scan", "chkdsk.exe", new[] { "C:", "/scan" }, true, progress);
    }

    public async Task<NativeToolResult> ExportSystemHealthReportAsync(IProgress<string>? progress)
    {
        const string actionName = "Export system health report";
        var output = new List<string>();

        void Write(string line)
        {
            output.Add(line);
            progress?.Report(line);
        }

        try
        {
            Directory.CreateDirectory(_reportsPath);
            string reportPath = BuildReportPath("health-report");

            Write("[INFO] Building system health report...");

            CommandRunResult systemInfo = await ProcessRunner.RunAsync("systeminfo.exe", Array.Empty<string>());
            CommandRunResult ipconfig = await ProcessRunner.RunAsync("ipconfig.exe", new[] { "/all" });
            CommandRunResult computerInfo = await ProcessRunner.RunPowerShellAsync("Get-ComputerInfo | Select-Object WindowsProductName, WindowsVersion, OsHardwareAbstractionLayer, OsArchitecture | Format-List");
            CommandRunResult volumes = await ProcessRunner.RunPowerShellAsync("Get-Volume | Format-Table DriveLetter, FileSystemLabel, FileSystem, SizeRemaining, Size -AutoSize");
            CommandRunResult hotfixes = await ProcessRunner.RunPowerShellAsync("Get-HotFix | Sort-Object InstalledOn -Descending | Select-Object -First 15 | Format-Table HotFixID, InstalledOn, Description -AutoSize");
            CommandRunResult events = await ProcessRunner.RunPowerShellAsync("Get-EventLog -LogName System -Newest 30 | Format-Table TimeGenerated, EntryType, Source, EventID, Message -Wrap");

            var builder = new StringBuilder();
            builder.AppendLine("================ SYSTEM HEALTH REPORT ================");
            builder.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            builder.AppendLine("Computer : " + Environment.MachineName);
            builder.AppendLine("User     : " + Environment.UserName);
            builder.AppendLine();
            AppendSection(builder, "SYSTEMINFO", systemInfo.OutputText);
            AppendSection(builder, "WINDOWS EDITION", computerInfo.OutputText);
            AppendSection(builder, "DISK STATUS", volumes.OutputText);
            AppendSection(builder, "NETWORK CONFIG", ipconfig.OutputText);
            AppendSection(builder, "RECENT HOTFIXES", hotfixes.OutputText);
            AppendSection(builder, "RECENT SYSTEM EVENTS", events.OutputText);

            await File.WriteAllTextAsync(reportPath, builder.ToString().TrimEnd() + Environment.NewLine, Encoding.UTF8);

            Write("Report : " + reportPath);
            Write("[GOOD] System health report exported.");
            return new NativeToolResult(actionName, true, 0, "System health report exported.", string.Join(Environment.NewLine, output), new[] { reportPath });
        }
        catch (Exception ex)
        {
            Write("[FAIL] " + ex.Message);
            return new NativeToolResult(actionName, false, -1, ex.Message, string.Join(Environment.NewLine, output), Array.Empty<string>());
        }
    }

    private Task<NativeToolResult> RunAnalyzeComponentStoreAsync(IProgress<string>? progress)
    {
        return RunCommandActionAsync("Analyze component store", "dism.exe", new[] { "/Online", "/Cleanup-Image", "/AnalyzeComponentStore" }, true, progress);
    }

    private Task<NativeToolResult> RunSfcSecondPassAsync(IProgress<string>? progress)
    {
        return RunCommandActionAsync("System File Checker second run", "sfc.exe", new[] { "/scannow" }, true, progress);
    }

    private async Task<NativeToolResult> RunCompositeAsync(string actionName, IReadOnlyList<Func<IProgress<string>, Task<NativeToolResult>>> steps, IProgress<string>? progress)
    {
        var output = new List<string>();
        var reportPaths = new List<string>();
        int exitCode = 0;

        void Forward(string line)
        {
            output.Add(line);
            progress?.Report(line);
        }

        var bridge = new Progress<string>(Forward);

        foreach (Func<IProgress<string>, Task<NativeToolResult>> step in steps)
        {
            if (output.Count > 0)
            {
                Forward(string.Empty);
            }

            NativeToolResult result = await step(bridge);
            exitCode = result.ExitCode;
            foreach (string reportPath in result.ReportPaths)
            {
                reportPaths.Add(reportPath);
            }

            if (!result.Succeeded)
            {
                Forward("[WARN] " + actionName + " stopped because a step failed.");
                return new NativeToolResult(actionName, false, exitCode, result.SummaryText, string.Join(Environment.NewLine, output), reportPaths);
            }
        }

        Forward("[GOOD] " + actionName + " completed.");
        return new NativeToolResult(actionName, true, exitCode, actionName + " completed.", string.Join(Environment.NewLine, output), reportPaths);
    }

    private async Task<NativeToolResult> RunCommandActionAsync(string actionName, string fileName, IReadOnlyList<string> arguments, bool requiresAdmin, IProgress<string>? progress, bool captureReport = false, string? reportPrefix = null)
    {
        if (requiresAdmin && !IsAdministrator())
        {
            string message = "This action requires the app to be run as Administrator.";
            progress?.Report("[FAIL] " + message);
            return new NativeToolResult(actionName, false, -1, message, message, Array.Empty<string>());
        }

        progress?.Report("[INFO] " + actionName + "...");
        progress?.Report("Command : " + fileName + " " + string.Join(" ", arguments));

        CommandRunResult result = await ProcessRunner.RunAsync(fileName, arguments, progress);
        if (result.OutputLines.Count == 0)
        {
            progress?.Report("Command returned no console output.");
        }

        progress?.Report("Exit Code : " + result.ExitCode);

        var reports = new List<string>();
        if (captureReport)
        {
            Directory.CreateDirectory(_reportsPath);
            string reportPath = BuildReportPath(reportPrefix ?? actionName.ToLowerInvariant().Replace(' ', '-'));
            await File.WriteAllTextAsync(reportPath, result.OutputText + Environment.NewLine, Encoding.UTF8);
            progress?.Report("Report : " + reportPath);
            reports.Add(reportPath);
        }

        bool succeeded = result.ExitCode == 0;
        progress?.Report(succeeded ? "[GOOD] " + actionName + " completed." : "[FAIL] " + actionName + " failed.");
        return new NativeToolResult(actionName, succeeded, result.ExitCode, succeeded ? actionName + " completed." : actionName + " failed.", result.OutputText, reports);
    }

    private Task<NativeToolResult> RunPowerShellActionAsync(string actionName, string script, bool requiresAdmin, IProgress<string>? progress, bool captureReport = false, string? reportPrefix = null)
    {
        return RunCommandActionAsync(actionName, ProcessRunner.ResolvePowerShellPath(), new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", script }, requiresAdmin, progress, captureReport, reportPrefix);
    }

    private string BuildReportPath(string prefix)
    {
        Directory.CreateDirectory(_reportsPath);
        string safePrefix = new string(prefix.Select(character => char.IsLetterOrDigit(character) ? character : '-').ToArray()).Trim('-');
        return Path.Combine(_reportsPath, "auto-repair-" + safePrefix + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt");
    }

    private static void AppendSection(StringBuilder builder, string title, string text)
    {
        builder.AppendLine("--- " + title + " ---");
        builder.AppendLine(string.IsNullOrWhiteSpace(text) ? "(no output)" : text.TrimEnd());
        builder.AppendLine();
    }

    private static bool IsAdministrator()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}
