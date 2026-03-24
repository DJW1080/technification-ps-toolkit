using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using TechnificationToolboxApp.Models;

namespace TechnificationToolboxApp.Services;

public sealed class WindowsEnhancementsService
{
    private readonly string _logsPath;
    private readonly string _reportsPath;

    public WindowsEnhancementsService(string reportsPath, string logsPath)
    {
        _reportsPath = reportsPath;
        _logsPath = logsPath;
    }

    public WindowsEnhancementsStatus GetStatus()
    {
        return new WindowsEnhancementsStatus(IsAdministrator(), GetHibernationEnabled(), IsContextMenuInstalled(), _logsPath, _reportsPath);
    }

    public Task<NativeToolResult> DescribeStatusAsync(IProgress<string>? progress)
    {
        return Task.Run(() =>
        {
            const string actionName = "Show enhancements status";
            WindowsEnhancementsStatus status = GetStatus();
            var output = new List<string>();

            void Write(string line)
            {
                output.Add(line);
                progress?.Report(line);
            }

            Write("Administrator : " + (status.IsAdministrator ? "Yes" : "No"));
            Write("Hibernation   : " + FormatHibernation(status.HibernationEnabled));
            Write("Context menu  : " + (status.ContextMenuInstalled ? "Installed" : "Not installed"));
            Write("Logs          : " + status.LogsPath);
            Write("Reports       : " + status.ReportsPath);
            Write("[GOOD] Windows enhancements status refreshed.");
            return new NativeToolResult(actionName, true, 0, "Status refreshed.", string.Join(Environment.NewLine, output), Array.Empty<string>());
        });
    }

    public Task<NativeToolResult> EnableHibernationAsync(IProgress<string>? progress)
    {
        return SetHibernationStateAsync(true, progress);
    }

    public Task<NativeToolResult> DisableHibernationAsync(IProgress<string>? progress)
    {
        return SetHibernationStateAsync(false, progress);
    }

    public Task<NativeToolResult> InstallAdminContextMenuAsync(IProgress<string>? progress)
    {
        return Task.Run(() =>
        {
            const string actionName = "Install PowerShell admin context menu";
            var output = new List<string>();

            void Write(string line)
            {
                output.Add(line);
                progress?.Report(line);
            }

            if (!IsAdministrator())
            {
                string message = "Installing the context menu requires the app to be run as Administrator.";
                Write("[FAIL] " + message);
                return new NativeToolResult(actionName, false, -1, message, string.Join(Environment.NewLine, output), Array.Empty<string>());
            }

            try
            {
                string command = ProcessRunner.ResolvePowerShellPath() + " -NoExit -NoProfile -Command \"Set-Location '%V'\"";
                foreach (string path in new[] { @"directory\shell\runas", @"directory\background\shell\runas", @"drive\shell\runas" })
                {
                    using RegistryKey? shellKey = Registry.ClassesRoot.CreateSubKey(path);
                    using RegistryKey? commandKey = Registry.ClassesRoot.CreateSubKey(path + @"\command");
                    shellKey?.SetValue(null, "Open PowerShell Here (Admin)");
                    shellKey?.SetValue("HasLUAShield", string.Empty);
                    commandKey?.SetValue(null, command);
                }

                Write("[GOOD] Open PowerShell Here (Admin) context menu installed.");
                return new NativeToolResult(actionName, true, 0, "Context menu installed.", string.Join(Environment.NewLine, output), Array.Empty<string>());
            }
            catch (Exception ex)
            {
                Write("[FAIL] " + ex.Message);
                return new NativeToolResult(actionName, false, -1, ex.Message, string.Join(Environment.NewLine, output), Array.Empty<string>());
            }
        });
    }

    public async Task<NativeToolResult> RunDiskCleanupAsync(bool createRestorePoint, IProgress<string>? progress)
    {
        const string actionName = "Run disk cleanup tool";
        var output = new List<string>();

        void Write(string line)
        {
            output.Add(line);
            progress?.Report(line);
        }

        if (!IsAdministrator())
        {
            string message = "Disk cleanup requires the app to be run as Administrator.";
            Write("[FAIL] " + message);
            return new NativeToolResult(actionName, false, -1, message, string.Join(Environment.NewLine, output), Array.Empty<string>());
        }

        try
        {
            Write("[INFO] Running disk cleanup workflow...");
            var results = new List<CleanupStepResult>();
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            if (createRestorePoint)
            {
                results.Add(await RunCleanupStepAsync("Creating restore point", Array.Empty<string>(), async () =>
                {
                    CommandRunResult result = await ProcessRunner.RunPowerShellAsync(@"
$ErrorActionPreference = 'Stop'
Enable-ComputerRestore -Drive 'C:\' -ErrorAction SilentlyContinue
Checkpoint-Computer -Description ('Pre-DeepCleanup ' + (Get-Date -Format 'yyyy-MM-dd-HHmmss')) -RestorePointType 'MODIFY_SETTINGS'
");
                    return result.ExitCode == 0 ? null : (string.IsNullOrWhiteSpace(result.OutputText) ? "Restore point creation failed." : result.OutputText);
                }));
            }
            else
            {
                Write("[WARN] Restore point skipped by user choice.");
            }

            results.Add(await RunCleanupStepAsync("Running Disk Cleanup profile 1", Array.Empty<string>(), async () =>
            {
                string cleanMgrPath = Path.Combine(Environment.SystemDirectory, "cleanmgr.exe");
                if (!File.Exists(cleanMgrPath))
                {
                    return "cleanmgr.exe was not found. Skipping profile run.";
                }

                CommandRunResult result = await ProcessRunner.RunAsync(cleanMgrPath, new[] { "/sagerun:1" });
                return result.ExitCode == 0 ? null : (string.IsNullOrWhiteSpace(result.OutputText) ? "Disk Cleanup profile run failed." : result.OutputText);
            }));

            results.Add(await RunCleanupStepAsync("Clearing system temp folders", new[] { Path.GetTempPath(), @"C:\Windows\Temp" }, () =>
            {
                DeleteContents(Path.GetTempPath());
                DeleteContents(@"C:\Windows\Temp");
                return Task.FromResult<string?>(null);
            }));

            results.Add(await RunCleanupStepAsync("Clearing current user temp and browser caches", new[]
            {
                Path.Combine(userProfile, "AppData", "Local", "Temp"),
                Path.Combine(userProfile, "AppData", "Local", "Microsoft", "Windows", "INetCache"),
                Path.Combine(userProfile, "AppData", "Local", "Microsoft", "Windows", "WebCache")
            }, () =>
            {
                DeleteContents(Path.Combine(userProfile, "AppData", "Local", "Temp"));
                DeleteContents(Path.Combine(userProfile, "AppData", "Local", "Microsoft", "Windows", "INetCache"));
                DeleteContents(Path.Combine(userProfile, "AppData", "Local", "Microsoft", "Windows", "WebCache"));
                return Task.FromResult<string?>(null);
            }));

            results.Add(await RunCleanupStepAsync("Clearing thumbnail cache", new[] { Path.Combine(userProfile, "AppData", "Local", "Microsoft", "Windows", "Explorer") }, () =>
            {
                DeleteFilesMatching(Path.Combine(userProfile, "AppData", "Local", "Microsoft", "Windows", "Explorer"), "thumbcache_*");
                return Task.FromResult<string?>(null);
            }));

            results.Add(await RunCleanupStepAsync("Clearing DirectX shader cache", new[]
            {
                Path.Combine(userProfile, "AppData", "Local", "D3DSCache"),
                Path.Combine(userProfile, "AppData", "Local", "NVIDIA", "DXCache"),
                Path.Combine(userProfile, "AppData", "Local", "NVIDIA", "GLCache")
            }, () =>
            {
                DeleteContents(Path.Combine(userProfile, "AppData", "Local", "D3DSCache"));
                DeleteContents(Path.Combine(userProfile, "AppData", "Local", "NVIDIA", "DXCache"));
                DeleteContents(Path.Combine(userProfile, "AppData", "Local", "NVIDIA", "GLCache"));
                return Task.FromResult<string?>(null);
            }));

            results.Add(await RunCleanupStepAsync("Clearing Delivery Optimization cache", new[]
            {
                @"C:\Windows\SoftwareDistribution\DeliveryOptimization",
                @"C:\ProgramData\Microsoft\Windows\DeliveryOptimization\Cache"
            }, async () =>
            {
                CommandRunResult result = await ProcessRunner.RunPowerShellAsync(@"
$service = Get-Service -Name dosvc -ErrorAction SilentlyContinue
if ($null -eq $service) { Write-Output 'Delivery Optimization service not found. Skipping cache cleanup.'; exit 0 }
Stop-Service -Name dosvc -Force -ErrorAction Stop
try {
    Remove-Item 'C:\Windows\SoftwareDistribution\DeliveryOptimization\*' -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item 'C:\ProgramData\Microsoft\Windows\DeliveryOptimization\Cache\*' -Recurse -Force -ErrorAction SilentlyContinue
}
finally {
    Start-Service -Name dosvc -ErrorAction SilentlyContinue
}
");
                return result.ExitCode == 0 ? null : (string.IsNullOrWhiteSpace(result.OutputText) ? "Delivery Optimization cleanup failed." : result.OutputText);
            }));

            results.Add(await RunCleanupStepAsync("Clearing Windows Update download cache", new[] { @"C:\Windows\SoftwareDistribution\Download" }, async () =>
            {
                CommandRunResult result = await ProcessRunner.RunPowerShellAsync(@"
$service = Get-Service -Name wuauserv -ErrorAction SilentlyContinue
if ($null -eq $service) { Write-Output 'Windows Update service not found. Skipping cache cleanup.'; exit 0 }
Stop-Service -Name wuauserv -Force -ErrorAction Stop
try {
    Remove-Item 'C:\Windows\SoftwareDistribution\Download\*' -Recurse -Force -ErrorAction SilentlyContinue
}
finally {
    Start-Service -Name wuauserv -ErrorAction SilentlyContinue
}
");
                return result.ExitCode == 0 ? null : (string.IsNullOrWhiteSpace(result.OutputText) ? "Windows Update cache cleanup failed." : result.OutputText);
            }));

            results.Add(await RunCleanupStepAsync("Clearing Windows Error Reporting files", new[]
            {
                @"C:\ProgramData\Microsoft\Windows\WER",
                Path.Combine(userProfile, "AppData", "Local", "Microsoft", "Windows", "WER")
            }, () =>
            {
                DeleteContents(@"C:\ProgramData\Microsoft\Windows\WER");
                DeleteContents(Path.Combine(userProfile, "AppData", "Local", "Microsoft", "Windows", "WER"));
                return Task.FromResult<string?>(null);
            }));

            results.Add(await RunCleanupStepAsync("Clearing CBS and DISM logs", new[] { @"C:\Windows\Logs\CBS", @"C:\Windows\Logs\DISM" }, () =>
            {
                DeleteFilesMatching(@"C:\Windows\Logs\CBS", "*");
                DeleteFilesMatching(@"C:\Windows\Logs\DISM", "*");
                return Task.FromResult<string?>(null);
            }));

            results.Add(await RunCleanupStepAsync("Emptying Recycle Bin", Array.Empty<string>(), async () =>
            {
                CommandRunResult result = await ProcessRunner.RunPowerShellAsync("Clear-RecycleBin -Force -ErrorAction SilentlyContinue");
                return result.ExitCode == 0 ? null : (string.IsNullOrWhiteSpace(result.OutputText) ? "Recycle Bin cleanup failed." : result.OutputText);
            }));

            results.Add(await RunCleanupStepAsync("Triggering Storage Sense cleanup", Array.Empty<string>(), async () =>
            {
                CommandRunResult result = await ProcessRunner.RunPowerShellAsync(@"
$storageSense = Get-ScheduledTask -ErrorAction SilentlyContinue | Where-Object { $_.TaskName -like '*StartStorageSense*' } | Select-Object -First 1
if ($null -eq $storageSense) { Write-Output 'Storage Sense task not found. Skipping.'; exit 0 }
Start-ScheduledTask -TaskName $storageSense.TaskName -TaskPath $storageSense.TaskPath
");
                return result.ExitCode == 0 ? null : (string.IsNullOrWhiteSpace(result.OutputText) ? "Storage Sense trigger failed." : result.OutputText);
            }));

            int totalFiles = results.Sum(result => result.FilesRemoved);
            long totalBytes = results.Sum(result => result.BytesRecovered);

            Write("================ CLEANUP SUMMARY ===================");
            foreach (CleanupStepResult result in results)
            {
                Write(result.Label + " [" + result.Status + "]");
                Write("  Files removed   : " + result.FilesRemoved);
                Write("  Space recovered : " + FormatSize(result.BytesRecovered));
                if (!string.IsNullOrWhiteSpace(result.Notes))
                {
                    Write("  Notes           : " + result.Notes);
                }
            }

            Write("---------------------------------------------------");
            Write("Total files removed   : " + totalFiles);
            Write("Total space recovered : " + FormatSize(totalBytes));

            string reportPath = WriteReport("disk-cleanup", output);
            Write("Report : " + reportPath);
            Write("[GOOD] Disk cleanup complete.");
            return new NativeToolResult(actionName, true, 0, "Disk cleanup complete.", string.Join(Environment.NewLine, output), new[] { reportPath });
        }
        catch (Exception ex)
        {
            Write("[FAIL] " + ex.Message);
            return new NativeToolResult(actionName, false, -1, ex.Message, string.Join(Environment.NewLine, output), Array.Empty<string>());
        }
    }

    private async Task<NativeToolResult> SetHibernationStateAsync(bool enable, IProgress<string>? progress)
    {
        string actionName = enable ? "Enable hibernation" : "Disable hibernation";
        var output = new List<string>();

        void Write(string line)
        {
            output.Add(line);
            progress?.Report(line);
        }

        if (!IsAdministrator())
        {
            string message = "Changing hibernation requires the app to be run as Administrator.";
            Write("[FAIL] " + message);
            return new NativeToolResult(actionName, false, -1, message, string.Join(Environment.NewLine, output), Array.Empty<string>());
        }

        CommandRunResult result = await ProcessRunner.RunAsync("powercfg.exe", new[] { "/hibernate", enable ? "on" : "off" });
        bool? status = GetHibernationEnabled();
        bool succeeded = result.ExitCode == 0 && status == enable;
        Write("Exit Code : " + result.ExitCode);
        if (!string.IsNullOrWhiteSpace(result.OutputText))
        {
            Write(result.OutputText);
        }

        Write(succeeded
            ? "[GOOD] Hibernation has been " + (enable ? "enabled." : "disabled.")
            : "[FAIL] The requested hibernation change did not apply.");

        return new NativeToolResult(actionName, succeeded, result.ExitCode, succeeded ? "Hibernation updated." : "Hibernation update failed.", string.Join(Environment.NewLine, output), Array.Empty<string>());
    }

    private async Task<CleanupStepResult> RunCleanupStepAsync(string label, IReadOnlyList<string> trackedPaths, Func<Task<string?>> action)
    {
        int beforeFiles = 0;
        long beforeBytes = 0;
        foreach (string path in trackedPaths)
        {
            PathStats stats = GetPathStats(path);
            beforeFiles += stats.FileCount;
            beforeBytes += stats.TotalBytes;
        }

        string status = "Completed";
        string notes = string.Empty;

        try
        {
            string? actionNote = await action();
            if (!string.IsNullOrWhiteSpace(actionNote))
            {
                status = "Warning";
                notes = actionNote;
            }
        }
        catch (Exception ex)
        {
            status = "Warning";
            notes = ex.Message;
        }

        int afterFiles = 0;
        long afterBytes = 0;
        foreach (string path in trackedPaths)
        {
            PathStats stats = GetPathStats(path);
            afterFiles += stats.FileCount;
            afterBytes += stats.TotalBytes;
        }

        return new CleanupStepResult(label, status, Math.Max(0, beforeFiles - afterFiles), Math.Max(0L, beforeBytes - afterBytes), notes);
    }

    private static PathStats GetPathStats(string path)
    {
        if (!Directory.Exists(path))
        {
            return new PathStats(0, 0);
        }

        int files = 0;
        long bytes = 0;
        foreach (string filePath in EnumerateFilesSafe(path))
        {
            try
            {
                FileInfo info = new FileInfo(filePath);
                files++;
                bytes += info.Length;
            }
            catch
            {
            }
        }

        return new PathStats(files, bytes);
    }

    private static void DeleteContents(string path)
    {
        foreach (string filePath in EnumerateFilesSafe(path))
        {
            try
            {
                FileInfo file = new FileInfo(filePath);
                if (file.IsReadOnly)
                {
                    file.IsReadOnly = false;
                }

                file.Delete();
            }
            catch
            {
            }
        }
    }

    private static void DeleteFilesMatching(string path, string pattern)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(path, pattern, SearchOption.TopDirectoryOnly).ToArray();
        }
        catch
        {
            return;
        }

        foreach (string filePath in files)
        {
            try
            {
                FileInfo file = new FileInfo(filePath);
                if (file.IsReadOnly)
                {
                    file.IsReadOnly = false;
                }

                file.Delete();
            }
            catch
            {
            }
        }
    }

    private static IEnumerable<string> EnumerateFilesSafe(string rootPath)
    {
        if (!Directory.Exists(rootPath))
        {
            yield break;
        }

        var pending = new Stack<string>();
        pending.Push(rootPath);
        while (pending.Count > 0)
        {
            string current = pending.Pop();
            IEnumerable<string> files = Array.Empty<string>();
            IEnumerable<string> directories = Array.Empty<string>();

            try
            {
                files = Directory.EnumerateFiles(current);
            }
            catch
            {
            }

            foreach (string file in files)
            {
                yield return file;
            }

            try
            {
                directories = Directory.EnumerateDirectories(current);
            }
            catch
            {
            }

            foreach (string directory in directories)
            {
                pending.Push(directory);
            }
        }
    }

    private string WriteReport(string prefix, IReadOnlyList<string> lines)
    {
        Directory.CreateDirectory(_reportsPath);
        string reportPath = Path.Combine(_reportsPath, "windows-enhancements-" + prefix + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt");
        File.WriteAllText(reportPath, string.Join(Environment.NewLine, lines).TrimEnd() + Environment.NewLine, Encoding.UTF8);
        return reportPath;
    }

    private static bool? GetHibernationEnabled()
    {
        try
        {
            object? value = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power", "HibernateEnabled", null);
            if (value == null)
            {
                return null;
            }

            return Convert.ToInt32(value) == 1;
        }
        catch
        {
            return null;
        }
    }

    private static string FormatHibernation(bool? value)
    {
        if (value == true)
        {
            return "Enabled";
        }

        if (value == false)
        {
            return "Disabled";
        }

        return "Unknown";
    }

    private static bool IsContextMenuInstalled()
    {
        try
        {
            using RegistryKey? key = Registry.ClassesRoot.OpenSubKey(@"directory\shell\runas");
            string? value = key?.GetValue(null) as string;
            return string.Equals(value, "Open PowerShell Here (Admin)", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsAdministrator()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static string FormatSize(long bytes)
    {
        if (bytes >= 1024L * 1024L * 1024L)
        {
            return (bytes / (1024d * 1024d * 1024d)).ToString("N2") + " GB";
        }

        if (bytes >= 1024L * 1024L)
        {
            return (bytes / (1024d * 1024d)).ToString("N2") + " MB";
        }

        if (bytes >= 1024L)
        {
            return (bytes / 1024d).ToString("N2") + " KB";
        }

        return bytes + " B";
    }

    private sealed class CleanupStepResult
    {
        public CleanupStepResult(string label, string status, int filesRemoved, long bytesRecovered, string notes)
        {
            Label = label;
            Status = status;
            FilesRemoved = filesRemoved;
            BytesRecovered = bytesRecovered;
            Notes = notes;
        }

        public string Label { get; }
        public string Status { get; }
        public int FilesRemoved { get; }
        public long BytesRecovered { get; }
        public string Notes { get; }
    }

    private sealed class PathStats
    {
        public PathStats(int fileCount, long totalBytes)
        {
            FileCount = fileCount;
            TotalBytes = totalBytes;
        }

        public int FileCount { get; }
        public long TotalBytes { get; }
    }
}
