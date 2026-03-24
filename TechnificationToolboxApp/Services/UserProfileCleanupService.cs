using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TechnificationToolboxApp.Models;

namespace TechnificationToolboxApp.Services;

public sealed class UserProfileCleanupService
{
    private readonly string _logsPath;
    private readonly string _reportsPath;
    private readonly List<string> _excludedPatterns = new List<string> { "*.gitkeep" };
    private readonly List<LockedCleanupItem> _lastLockedItems = new List<LockedCleanupItem>();

    public UserProfileCleanupService(string reportsPath, string logsPath)
    {
        _reportsPath = reportsPath;
        _logsPath = logsPath;
    }

    public UserProfileCleanupStatus GetStatus()
    {
        return new UserProfileCleanupStatus(GetCleanupTargets().Count, _excludedPatterns.Count, _lastLockedItems.Count, _logsPath, _reportsPath);
    }

    public Task<NativeToolResult> DeepScanAsync(IProgress<string>? progress)
    {
        return Task.Run(() =>
        {
            const string actionName = "Deep scan current user profile";
            var output = new List<string>();

            void Write(string line)
            {
                output.Add(line);
                progress?.Report(line);
            }

            try
            {
                List<TargetStats> results = GetDeepScan();
                int totalFiles = results.Sum(result => result.FileCount);
                long totalBytes = results.Sum(result => result.TotalBytes);

                Write("[INFO] Scanning current user profile cleanup targets...");
                Write("================ USER PROFILE DEEP SCAN ================");
                foreach (TargetStats result in results.OrderByDescending(result => result.TotalBytes))
                {
                    Write(result.Name + " [" + result.Category + "]");
                    Write("  Path  : " + result.Path);
                    Write("  Files : " + result.FileCount);
                    Write("  Size  : " + FormatSize(result.TotalBytes));
                }

                Write("--------------------------------------------------------");
                Write("Total files : " + totalFiles);
                Write("Total size  : " + FormatSize(totalBytes));

                string reportPath = WriteReport("deep-scan", output);
                Write("Report : " + reportPath);
                Write("[GOOD] Deep scan completed.");
                return new NativeToolResult(actionName, true, 0, totalFiles + " file(s) across cleanup targets.", string.Join(Environment.NewLine, output), new[] { reportPath });
            }
            catch (Exception ex)
            {
                Write("[FAIL] " + ex.Message);
                return new NativeToolResult(actionName, false, -1, ex.Message, string.Join(Environment.NewLine, output), Array.Empty<string>());
            }
        });
    }

    public Task<NativeToolResult> ShowCategorySummaryAsync(IProgress<string>? progress)
    {
        return Task.Run(() =>
        {
            const string actionName = "Show cleanup categories";
            var output = new List<string>();

            void Write(string line)
            {
                output.Add(line);
                progress?.Report(line);
            }

            try
            {
                List<TargetStats> scan = GetDeepScan();
                Write("[INFO] Grouping cleanup targets by category...");
                Write("================ CLEANUP CATEGORIES =================");
                foreach (IGrouping<string, TargetStats> group in scan.GroupBy(item => item.Category).OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
                {
                    int files = group.Sum(item => item.FileCount);
                    long bytes = group.Sum(item => item.TotalBytes);
                    Write(group.Key);
                    Write("  Files : " + files);
                    Write("  Size  : " + FormatSize(bytes));
                }

                string reportPath = WriteReport("category-summary", output);
                Write("Report : " + reportPath);
                Write("[GOOD] Category summary completed.");
                return new NativeToolResult(actionName, true, 0, "Category summary completed.", string.Join(Environment.NewLine, output), new[] { reportPath });
            }
            catch (Exception ex)
            {
                Write("[FAIL] " + ex.Message);
                return new NativeToolResult(actionName, false, -1, ex.Message, string.Join(Environment.NewLine, output), Array.Empty<string>());
            }
        });
    }

    public Task<NativeToolResult> CleanTempAsync(IProgress<string>? progress) => CleanCategoryAsync("Temp", progress);
    public Task<NativeToolResult> CleanBrowserCacheAsync(IProgress<string>? progress) => CleanCategoryAsync("Browser Cache", progress);
    public Task<NativeToolResult> CleanAppCacheAsync(IProgress<string>? progress) => CleanCategoryAsync("App Cache", progress);
    public Task<NativeToolResult> CleanCrashDataAsync(IProgress<string>? progress) => CleanCategoryAsync("Crash Data", progress);
    public Task<NativeToolResult> CleanLogsAsync(IProgress<string>? progress) => CleanCategoryAsync("Logs", progress);

    public Task<NativeToolResult> AddExclusionPatternAsync(string pattern, IProgress<string>? progress)
    {
        return Task.Run(() =>
        {
            const string actionName = "Add exclusion pattern";
            var output = new List<string>();

            void Write(string line)
            {
                output.Add(line);
                progress?.Report(line);
            }

            if (string.IsNullOrWhiteSpace(pattern))
            {
                string message = "Enter a file pattern to exclude before adding it.";
                Write("[FAIL] " + message);
                return new NativeToolResult(actionName, false, -1, message, string.Join(Environment.NewLine, output), Array.Empty<string>());
            }

            string value = pattern.Trim();
            if (_excludedPatterns.Any(existing => string.Equals(existing, value, StringComparison.OrdinalIgnoreCase)))
            {
                string message = "Pattern already exists: " + value;
                Write("[WARN] " + message);
                return new NativeToolResult(actionName, true, 0, message, string.Join(Environment.NewLine, output), Array.Empty<string>());
            }

            _excludedPatterns.Add(value);
            Write("[GOOD] Added exclusion: " + value);
            return new NativeToolResult(actionName, true, 0, "Exclusion added.", string.Join(Environment.NewLine, output), Array.Empty<string>());
        });
    }

    public Task<NativeToolResult> RemoveExclusionPatternAsync(string pattern, IProgress<string>? progress)
    {
        return Task.Run(() =>
        {
            const string actionName = "Remove exclusion pattern";
            var output = new List<string>();

            void Write(string line)
            {
                output.Add(line);
                progress?.Report(line);
            }

            if (string.IsNullOrWhiteSpace(pattern))
            {
                string message = "Enter a file pattern to remove.";
                Write("[FAIL] " + message);
                return new NativeToolResult(actionName, false, -1, message, string.Join(Environment.NewLine, output), Array.Empty<string>());
            }

            string value = pattern.Trim();
            string? match = _excludedPatterns.FirstOrDefault(existing => string.Equals(existing, value, StringComparison.OrdinalIgnoreCase));
            if (match == null)
            {
                string message = "Pattern not found: " + value;
                Write("[WARN] " + message);
                return new NativeToolResult(actionName, true, 0, message, string.Join(Environment.NewLine, output), Array.Empty<string>());
            }

            _excludedPatterns.Remove(match);
            Write("[GOOD] Removed exclusion: " + match);
            return new NativeToolResult(actionName, true, 0, "Exclusion removed.", string.Join(Environment.NewLine, output), Array.Empty<string>());
        });
    }

    public Task<NativeToolResult> ShowExclusionsAsync(IProgress<string>? progress)
    {
        return Task.Run(() =>
        {
            const string actionName = "Show exclusions";
            var output = new List<string>();

            void Write(string line)
            {
                output.Add(line);
                progress?.Report(line);
            }

            Write("================ EXCLUSIONS =================");
            foreach (string pattern in _excludedPatterns.OrderBy(pattern => pattern, StringComparer.OrdinalIgnoreCase))
            {
                Write(pattern);
            }

            Write(string.Empty);
            Write("[GOOD] Displayed " + _excludedPatterns.Count + " exclusion pattern(s).");
            return new NativeToolResult(actionName, true, 0, _excludedPatterns.Count + " exclusion pattern(s).", string.Join(Environment.NewLine, output), Array.Empty<string>());
        });
    }

    public Task<NativeToolResult> ShowLockedItemsAsync(IProgress<string>? progress)
    {
        return Task.Run(() =>
        {
            const string actionName = "Show locked or skipped files";
            var output = new List<string>();

            void Write(string line)
            {
                output.Add(line);
                progress?.Report(line);
            }

            Write("================ LOCKED OR SKIPPED FILES ==============");
            if (_lastLockedItems.Count == 0)
            {
                Write("[GOOD] No locked or skipped files were recorded in the last cleanup run.");
                return new NativeToolResult(actionName, true, 0, "No locked items.", string.Join(Environment.NewLine, output), Array.Empty<string>());
            }

            foreach (LockedCleanupItem item in _lastLockedItems.Take(50))
            {
                Write(item.Target);
                Write("  Path   : " + item.Path);
                Write("  Reason : " + item.Reason);
            }

            if (_lastLockedItems.Count > 50)
            {
                Write("[WARN] Only the first 50 entries are shown.");
            }

            string reportPath = WriteReport("locked-items", output);
            Write("Report : " + reportPath);
            Write("[GOOD] Displayed " + _lastLockedItems.Count + " locked/skipped item(s).");
            return new NativeToolResult(actionName, true, 0, _lastLockedItems.Count + " locked/skipped item(s).", string.Join(Environment.NewLine, output), new[] { reportPath });
        });
    }

    private Task<NativeToolResult> CleanCategoryAsync(string category, IProgress<string>? progress)
    {
        return Task.Run(() =>
        {
            string actionName = "Clean " + category + " category";
            var output = new List<string>();

            void Write(string line)
            {
                output.Add(line);
                progress?.Report(line);
            }

            try
            {
                List<TargetDefinition> targets = GetCleanupTargets().Where(target => string.Equals(target.Category, category, StringComparison.OrdinalIgnoreCase)).ToList();
                if (targets.Count == 0)
                {
                    string message = "No cleanup targets matched category '" + category + "'.";
                    Write("[WARN] " + message);
                    return new NativeToolResult(actionName, false, -1, message, string.Join(Environment.NewLine, output), Array.Empty<string>());
                }

                List<TargetStats> before = targets.Select(GetTargetStats).ToList();
                _lastLockedItems.Clear();

                foreach (TargetDefinition target in targets)
                {
                    foreach (FileInfo file in GetFilesForTarget(target))
                    {
                        try
                        {
                            if (file.IsReadOnly)
                            {
                                file.IsReadOnly = false;
                            }

                            file.Delete();
                        }
                        catch (Exception ex)
                        {
                            _lastLockedItems.Add(new LockedCleanupItem(target.Name, file.FullName, ex.Message));
                        }
                    }
                }

                List<TargetStats> after = targets.Select(GetTargetStats).ToList();
                int totalFiles = 0;
                long totalBytes = 0;

                Write("================ " + category.ToUpperInvariant() + " CLEANUP SUMMARY ================");
                for (int index = 0; index < before.Count; index++)
                {
                    int removedFiles = Math.Max(0, before[index].FileCount - after[index].FileCount);
                    long recoveredBytes = Math.Max(0L, before[index].TotalBytes - after[index].TotalBytes);
                    totalFiles += removedFiles;
                    totalBytes += recoveredBytes;

                    Write(before[index].Name);
                    Write("  Files removed   : " + removedFiles);
                    Write("  Space recovered : " + FormatSize(recoveredBytes));
                }

                Write("-----------------------------------------------------");
                Write("Total files removed   : " + totalFiles);
                Write("Total space recovered : " + FormatSize(totalBytes));
                Write("Locked/skipped files  : " + _lastLockedItems.Count);

                string reportPath = WriteReport("cleanup-" + category.ToLowerInvariant().Replace(' ', '-'), output);
                Write("Report : " + reportPath);
                Write("[GOOD] " + actionName + " completed.");
                return new NativeToolResult(actionName, true, 0, totalFiles + " files removed.", string.Join(Environment.NewLine, output), new[] { reportPath });
            }
            catch (Exception ex)
            {
                Write("[FAIL] " + ex.Message);
                return new NativeToolResult(actionName, false, -1, ex.Message, string.Join(Environment.NewLine, output), Array.Empty<string>());
            }
        });
    }

    private List<TargetStats> GetDeepScan()
    {
        return GetCleanupTargets().Select(GetTargetStats).ToList();
    }

    private List<TargetDefinition> GetCleanupTargets()
    {
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return new List<TargetDefinition>
        {
            new TargetDefinition("Local Temp", "Temp", Path.Combine(userProfile, "AppData", "Local", "Temp"), "*", TargetMode.Contents),
            new TargetDefinition("INetCache", "Browser Cache", Path.Combine(userProfile, "AppData", "Local", "Microsoft", "Windows", "INetCache"), "*", TargetMode.Contents),
            new TargetDefinition("WebCache", "Browser Cache", Path.Combine(userProfile, "AppData", "Local", "Microsoft", "Windows", "WebCache"), "*", TargetMode.Contents),
            new TargetDefinition("Explorer Thumbcache", "Cache", Path.Combine(userProfile, "AppData", "Local", "Microsoft", "Windows", "Explorer"), "thumbcache_*", TargetMode.FilteredFiles),
            new TargetDefinition("Crash Dumps", "Crash Data", Path.Combine(userProfile, "AppData", "Local", "CrashDumps"), "*", TargetMode.Contents),
            new TargetDefinition("D3D Shader Cache", "Cache", Path.Combine(userProfile, "AppData", "Local", "D3DSCache"), "*", TargetMode.Contents),
            new TargetDefinition("WER Reports", "Crash Data", Path.Combine(userProfile, "AppData", "Local", "Microsoft", "Windows", "WER"), "*", TargetMode.Contents),
            new TargetDefinition("Packages LocalCache", "App Cache", Path.Combine(userProfile, "AppData", "Local", "Packages"), "LocalCache", TargetMode.PackageChildFolders),
            new TargetDefinition("Packages TempState", "App Cache", Path.Combine(userProfile, "AppData", "Local", "Packages"), "TempState", TargetMode.PackageChildFolders),
            new TargetDefinition("Downloads TMP Files", "Temp", Path.Combine(userProfile, "Downloads"), "*.tmp", TargetMode.FilteredFiles),
            new TargetDefinition("Downloads LOG Files", "Logs", Path.Combine(userProfile, "Downloads"), "*.log", TargetMode.FilteredFiles)
        };
    }

    private TargetStats GetTargetStats(TargetDefinition target)
    {
        List<FileInfo> files = GetFilesForTarget(target);
        long totalBytes = 0;
        foreach (FileInfo file in files)
        {
            totalBytes += file.Length;
        }

        return new TargetStats(target.Name, target.Category, target.Path, files.Count, totalBytes);
    }

    private List<FileInfo> GetFilesForTarget(TargetDefinition target)
    {
        var files = new List<FileInfo>();
        if (!Directory.Exists(target.Path))
        {
            return files;
        }

        if (target.Mode == TargetMode.FilteredFiles)
        {
            foreach (string filePath in EnumerateFilesSafe(target.Path, SearchOption.TopDirectoryOnly))
            {
                string fileName = Path.GetFileName(filePath);
                if (IsExcluded(fileName) || !MatchesPattern(fileName, target.Filter))
                {
                    continue;
                }

                files.Add(new FileInfo(filePath));
            }

            return files;
        }

        if (target.Mode == TargetMode.PackageChildFolders)
        {
            foreach (string packagePath in EnumerateDirectoriesSafe(target.Path))
            {
                string childPath = Path.Combine(packagePath, target.Filter);
                if (!Directory.Exists(childPath))
                {
                    continue;
                }

                foreach (string filePath in EnumerateFilesSafe(childPath, SearchOption.AllDirectories))
                {
                    string fileName = Path.GetFileName(filePath);
                    if (!IsExcluded(fileName))
                    {
                        files.Add(new FileInfo(filePath));
                    }
                }
            }

            return files;
        }

        foreach (string filePath in EnumerateFilesSafe(target.Path, SearchOption.AllDirectories))
        {
            string fileName = Path.GetFileName(filePath);
            if (!IsExcluded(fileName))
            {
                files.Add(new FileInfo(filePath));
            }
        }

        return files;
    }

    private bool IsExcluded(string name)
    {
        return _excludedPatterns.Any(pattern => MatchesPattern(name, pattern));
    }

    private string WriteReport(string prefix, IReadOnlyList<string> lines)
    {
        Directory.CreateDirectory(_reportsPath);
        string reportPath = Path.Combine(_reportsPath, "profile-cleanup-" + prefix + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt");
        File.WriteAllText(reportPath, string.Join(Environment.NewLine, lines).TrimEnd() + Environment.NewLine, Encoding.UTF8);
        return reportPath;
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

    private static bool MatchesPattern(string value, string pattern)
    {
        string regex = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
        return Regex.IsMatch(value, regex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static IEnumerable<string> EnumerateFilesSafe(string rootPath, SearchOption searchOption)
    {
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

            if (searchOption == SearchOption.TopDirectoryOnly)
            {
                continue;
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

    private static IEnumerable<string> EnumerateDirectoriesSafe(string rootPath)
    {
        try
        {
            return Directory.EnumerateDirectories(rootPath).ToArray();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private sealed class TargetDefinition
    {
        public TargetDefinition(string name, string category, string path, string filter, TargetMode mode)
        {
            Name = name;
            Category = category;
            Path = path;
            Filter = filter;
            Mode = mode;
        }

        public string Name { get; }
        public string Category { get; }
        public string Path { get; }
        public string Filter { get; }
        public TargetMode Mode { get; }
    }

    private sealed class TargetStats
    {
        public TargetStats(string name, string category, string path, int fileCount, long totalBytes)
        {
            Name = name;
            Category = category;
            Path = path;
            FileCount = fileCount;
            TotalBytes = totalBytes;
        }

        public string Name { get; }
        public string Category { get; }
        public string Path { get; }
        public int FileCount { get; }
        public long TotalBytes { get; }
    }

    private sealed class LockedCleanupItem
    {
        public LockedCleanupItem(string target, string path, string reason)
        {
            Target = target;
            Path = path;
            Reason = reason;
        }

        public string Target { get; }
        public string Path { get; }
        public string Reason { get; }
    }

    private enum TargetMode
    {
        Contents,
        FilteredFiles,
        PackageChildFolders
    }
}
