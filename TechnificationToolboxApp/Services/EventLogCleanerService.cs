using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TechnificationToolboxApp.Models;

namespace TechnificationToolboxApp.Services;

public sealed class EventLogCleanerService
{
    private readonly string _configPath;
    private readonly string _logsPath;
    private readonly string _reportsPath;
    private readonly List<string> _defaultExact = new List<string> { "Security" };
    private readonly List<string> _defaultWildcard = new List<string> { "Microsoft-Windows-Windows Defender/*" };
    private readonly List<string> _excludedExactLogs = new List<string>();
    private readonly List<string> _excludedWildcardLogs = new List<string>();

    public EventLogCleanerService(string reportsPath, string logsPath)
    {
        _reportsPath = reportsPath;
        _logsPath = logsPath;
        _configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Technification", "EventLogCleaner", "config.json");
        ResetExclusionsToDefaultsCore();
        EnsureConfigStorage();
        if (File.Exists(_configPath))
        {
            LoadConfigFromDisk();
        }
    }

    public bool DryRunEnabled { get; private set; }
    public bool SkipCounts { get; private set; } = true;

    public EventLogCleanerStatus GetStatus()
    {
        return new EventLogCleanerStatus(IsAdministrator(), DryRunEnabled, SkipCounts, _excludedExactLogs.Count, _excludedWildcardLogs.Count, _configPath, _logsPath, _reportsPath);
    }

    public void SetDryRunEnabled(bool value)
    {
        DryRunEnabled = value;
    }

    public void SetSkipCounts(bool value)
    {
        SkipCounts = value;
    }

    public Task<NativeToolResult> ShowCurrentSettingsAsync(IProgress<string>? progress)
    {
        return Task.Run(() =>
        {
            const string actionName = "Show current exclusions and settings";
            var output = new List<string>();

            void Write(string line)
            {
                output.Add(line);
                progress?.Report(line);
            }

            Write("Current exclusions");
            Write("  Exact:");
            if (_excludedExactLogs.Count == 0)
            {
                Write("    (none)");
            }
            else
            {
                foreach (string log in _excludedExactLogs.OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
                {
                    Write("    " + log);
                }
            }

            Write("  Wildcards:");
            if (_excludedWildcardLogs.Count == 0)
            {
                Write("    (none)");
            }
            else
            {
                foreach (string pattern in _excludedWildcardLogs.OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
                {
                    Write("    " + pattern);
                }
            }

            Write(string.Empty);
            Write("Config path: " + _configPath);
            Write("SkipCounts : " + SkipCounts);
            Write("WhatIf     : " + DryRunEnabled);
            Write("Logs       : " + _logsPath);
            Write("Reports    : " + _reportsPath);
            Write("[GOOD] Displayed current exclusions and settings.");

            return new NativeToolResult(actionName, true, 0, "Settings displayed.", string.Join(Environment.NewLine, output), Array.Empty<string>());
        });
    }

    public Task<NativeToolResult> AddExactExclusionAsync(string value, IProgress<string>? progress)
    {
        return UpdateExclusionAsync("Add exact exclusion", value, _excludedExactLogs, progress);
    }

    public Task<NativeToolResult> AddWildcardExclusionAsync(string value, IProgress<string>? progress)
    {
        return UpdateExclusionAsync("Add wildcard exclusion", value, _excludedWildcardLogs, progress);
    }

    public Task<NativeToolResult> RemoveExactExclusionAsync(string value, IProgress<string>? progress)
    {
        return RemoveExclusionAsync("Remove exact exclusion", value, _excludedExactLogs, progress);
    }

    public Task<NativeToolResult> RemoveWildcardExclusionAsync(string value, IProgress<string>? progress)
    {
        return RemoveExclusionAsync("Remove wildcard exclusion", value, _excludedWildcardLogs, progress);
    }

    public Task<NativeToolResult> SaveConfigAsync(IProgress<string>? progress)
    {
        return Task.Run(() =>
        {
            const string actionName = "Save exclusions to config";
            var output = new List<string>();

            void Write(string line)
            {
                output.Add(line);
                progress?.Report(line);
            }

            try
            {
                EnsureConfigStorage();
                var payload = new ConfigModel
                {
                    SchemaVersion = 1,
                    SavedAt = DateTimeOffset.Now,
                    ExcludedExact = _excludedExactLogs.ToArray(),
                    ExcludedWildcard = _excludedWildcardLogs.ToArray()
                };

                string json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configPath, json, Encoding.UTF8);
                Write("[GOOD] Saved exclusions to: " + _configPath);
                return new NativeToolResult(actionName, true, 0, "Config saved.", string.Join(Environment.NewLine, output), Array.Empty<string>());
            }
            catch (Exception ex)
            {
                Write("[FAIL] " + ex.Message);
                return new NativeToolResult(actionName, false, -1, ex.Message, string.Join(Environment.NewLine, output), Array.Empty<string>());
            }
        });
    }

    public Task<NativeToolResult> LoadConfigAsync(IProgress<string>? progress)
    {
        return Task.Run(() =>
        {
            const string actionName = "Load exclusions from config";
            var output = new List<string>();

            void Write(string line)
            {
                output.Add(line);
                progress?.Report(line);
            }

            try
            {
                if (!File.Exists(_configPath))
                {
                    string message = "Config not found: " + _configPath;
                    Write("[WARN] " + message);
                    return new NativeToolResult(actionName, false, -1, message, string.Join(Environment.NewLine, output), Array.Empty<string>());
                }

                LoadConfigFromDisk();
                Write("[GOOD] Loaded exclusions from: " + _configPath);
                return new NativeToolResult(actionName, true, 0, "Config loaded.", string.Join(Environment.NewLine, output), Array.Empty<string>());
            }
            catch (Exception ex)
            {
                Write("[FAIL] " + ex.Message);
                return new NativeToolResult(actionName, false, -1, ex.Message, string.Join(Environment.NewLine, output), Array.Empty<string>());
            }
        });
    }

    public Task<NativeToolResult> ResetExclusionsToDefaultsAsync(IProgress<string>? progress)
    {
        return Task.Run(() =>
        {
            const string actionName = "Reset exclusions to defaults";
            var output = new List<string>();

            void Write(string line)
            {
                output.Add(line);
                progress?.Report(line);
            }

            ResetExclusionsToDefaultsCore();
            Write("[GOOD] Exclusions reset to defaults.");
            return new NativeToolResult(actionName, true, 0, "Defaults restored.", string.Join(Environment.NewLine, output), Array.Empty<string>());
        });
    }

    public async Task<NativeToolResult> StartCleanerAsync(IProgress<string>? progress)
    {
        const string actionName = "Start event log cleaner";
        var output = new List<string>();

        void Write(string line)
        {
            output.Add(line);
            progress?.Report(line);
        }

        try
        {
            if (!DryRunEnabled && !IsAdministrator())
            {
                string message = "Live event log clearing requires the app to be run as Administrator.";
                Write("[FAIL] " + message);
                return new NativeToolResult(actionName, false, -1, message, string.Join(Environment.NewLine, output), Array.Empty<string>());
            }

            Write("[INFO] Enumerating event logs...");
            List<EventLogInfo> logs = await GetTargetLogsAsync();
            if (logs.Count == 0)
            {
                string message = "No enabled event logs with records were found.";
                Write("[WARN] " + message);
                return new NativeToolResult(actionName, false, -1, message, string.Join(Environment.NewLine, output), Array.Empty<string>());
            }

            long totalLogsCleared = 0;
            long totalEventsRemoved = 0;
            long totalLogsSkipped = 0;
            long totalLogsFailed = 0;
            long totalLogsEligible = 0;
            long totalEventsEligible = 0;

            Write("Found " + logs.Count + " enabled logs with records.");
            Write("Mode     : " + (DryRunEnabled ? "WHATIF / DRY RUN" : "LIVE (will clear logs)"));
            Write("Counts   : " + (SkipCounts ? "SKIP (fast)" : "INCLUDE (slower)"));
            Write(string.Empty);

            foreach (EventLogInfo log in logs)
            {
                if (IsExcludedLog(log.LogName))
                {
                    Write("Skipping excluded log: " + log.LogName);
                    totalLogsSkipped++;
                    continue;
                }

                totalLogsEligible++;
                totalEventsEligible += log.RecordCount;

                int criticalCount = -1;
                int errorCount = -1;
                if (!SkipCounts)
                {
                    criticalCount = await GetLevelCountAsync(log.LogName, 1);
                    errorCount = await GetLevelCountAsync(log.LogName, 2);
                }

                Write("------------------------------------------------------------");
                Write("Log Name        : " + log.LogName);
                Write("Total Records   : " + log.RecordCount);
                Write("Critical (L1)   : " + criticalCount);
                Write("Error    (L2)   : " + errorCount);
                Write("Action          : " + (DryRunEnabled ? "SKIP (WhatIf)" : "CLEAR"));

                if (DryRunEnabled)
                {
                    Write("Result          : WHATIF (not cleared)");
                    Write(string.Empty);
                    continue;
                }

                CommandRunResult clearResult = await ProcessRunner.RunAsync(Path.Combine(Environment.SystemDirectory, "wevtutil.exe"), new[] { "cl", log.LogName });
                if (clearResult.ExitCode == 0)
                {
                    totalLogsCleared++;
                    totalEventsRemoved += log.RecordCount;
                    Write("Result          : CLEARED");
                }
                else
                {
                    totalLogsFailed++;
                    Write("Result          : FAILED");
                    Write("Reason          : " + (string.IsNullOrWhiteSpace(clearResult.OutputText) ? "wevtutil failed." : clearResult.OutputText));
                }

                Write(string.Empty);
            }

            Write("================= EVENT LOG CLEANER SUMMARY ================");
            Write("Eligible logs         : " + totalLogsEligible);
            Write("Eligible events       : " + totalEventsEligible);
            Write("Total logs cleared    : " + totalLogsCleared);
            Write("Total events removed  : " + totalEventsRemoved);
            Write("Total logs skipped    : " + totalLogsSkipped);
            Write("Total logs failed     : " + totalLogsFailed);
            Write("Excluded exact logs   : " + (_excludedExactLogs.Count == 0 ? "(none)" : string.Join(", ", _excludedExactLogs)));
            Write("Excluded wildcards    : " + (_excludedWildcardLogs.Count == 0 ? "(none)" : string.Join(", ", _excludedWildcardLogs)));

            string summaryText = DryRunEnabled
                ? "Dry run complete. " + totalLogsEligible + " logs and " + totalEventsEligible + " events matched, " + totalLogsSkipped + " logs skipped by exclusions."
                : "Cleared " + totalLogsCleared + " logs, removed " + totalEventsRemoved + " events, skipped " + totalLogsSkipped + ", failed " + totalLogsFailed + ".";

            string reportPath = WriteReport("cleaner-run", output);
            Write("Report : " + reportPath);
            Write("[GOOD] Event log cleaner completed.");
            bool succeeded = totalLogsFailed == 0;
            return new NativeToolResult(actionName, succeeded, succeeded ? 0 : 1, summaryText, string.Join(Environment.NewLine, output), new[] { reportPath });
        }
        catch (Exception ex)
        {
            Write("[FAIL] " + ex.Message);
            return new NativeToolResult(actionName, false, -1, ex.Message, string.Join(Environment.NewLine, output), Array.Empty<string>());
        }
    }

    private Task<NativeToolResult> UpdateExclusionAsync(string actionName, string value, List<string> collection, IProgress<string>? progress)
    {
        return Task.Run(() =>
        {
            var output = new List<string>();
            void Write(string line)
            {
                output.Add(line);
                progress?.Report(line);
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                string message = "Enter a value before applying this change.";
                Write("[FAIL] " + message);
                return new NativeToolResult(actionName, false, -1, message, string.Join(Environment.NewLine, output), Array.Empty<string>());
            }

            string trimmed = value.Trim();
            if (collection.Any(item => string.Equals(item, trimmed, StringComparison.OrdinalIgnoreCase)))
            {
                string message = "Already present: " + trimmed;
                Write("[WARN] " + message);
                return new NativeToolResult(actionName, true, 0, message, string.Join(Environment.NewLine, output), Array.Empty<string>());
            }

            collection.Add(trimmed);
            Write("[GOOD] Added: " + trimmed);
            return new NativeToolResult(actionName, true, 0, "Exclusion added.", string.Join(Environment.NewLine, output), Array.Empty<string>());
        });
    }

    private Task<NativeToolResult> RemoveExclusionAsync(string actionName, string value, List<string> collection, IProgress<string>? progress)
    {
        return Task.Run(() =>
        {
            var output = new List<string>();
            void Write(string line)
            {
                output.Add(line);
                progress?.Report(line);
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                string message = "Enter a value before removing it.";
                Write("[FAIL] " + message);
                return new NativeToolResult(actionName, false, -1, message, string.Join(Environment.NewLine, output), Array.Empty<string>());
            }

            string? match = collection.FirstOrDefault(item => string.Equals(item, value.Trim(), StringComparison.OrdinalIgnoreCase));
            if (match == null)
            {
                string message = "Not found: " + value.Trim();
                Write("[WARN] " + message);
                return new NativeToolResult(actionName, true, 0, message, string.Join(Environment.NewLine, output), Array.Empty<string>());
            }

            collection.Remove(match);
            Write("[GOOD] Removed: " + match);
            return new NativeToolResult(actionName, true, 0, "Exclusion removed.", string.Join(Environment.NewLine, output), Array.Empty<string>());
        });
    }

    private async Task<List<EventLogInfo>> GetTargetLogsAsync()
    {
        const string script = @"
$logs = @(Get-WinEvent -ListLog * -ErrorAction SilentlyContinue |
    Where-Object { $_ -and $_.IsEnabled -eq $true -and $_.LogName -and ($_.RecordCount -as [int64]) -gt 0 } |
    Select-Object LogName, RecordCount)
$logs | ConvertTo-Json -Compress
";

        CommandRunResult result = await ProcessRunner.RunPowerShellAsync(script);
        if (result.ExitCode != 0 || string.IsNullOrWhiteSpace(result.OutputText))
        {
            return new List<EventLogInfo>();
        }

        return DeserializeLogInfos(result.OutputText);
    }

    private static List<EventLogInfo> DeserializeLogInfos(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        var logs = new List<EventLogInfo>();

        if (document.RootElement.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement element in document.RootElement.EnumerateArray())
            {
                string logName = element.TryGetProperty("LogName", out JsonElement nameElement) ? nameElement.GetString() ?? string.Empty : string.Empty;
                long recordCount = element.TryGetProperty("RecordCount", out JsonElement countElement) ? countElement.GetInt64() : 0;
                if (!string.IsNullOrWhiteSpace(logName))
                {
                    logs.Add(new EventLogInfo(logName, recordCount));
                }
            }
        }
        else if (document.RootElement.ValueKind == JsonValueKind.Object)
        {
            string logName = document.RootElement.TryGetProperty("LogName", out JsonElement nameElement) ? nameElement.GetString() ?? string.Empty : string.Empty;
            long recordCount = document.RootElement.TryGetProperty("RecordCount", out JsonElement countElement) ? countElement.GetInt64() : 0;
            if (!string.IsNullOrWhiteSpace(logName))
            {
                logs.Add(new EventLogInfo(logName, recordCount));
            }
        }

        return logs.OrderBy(item => item.LogName, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private async Task<int> GetLevelCountAsync(string logName, int level)
    {
        string escapedName = logName.Replace("'", "''", StringComparison.Ordinal);
        string script = "$count = @(Get-WinEvent -FilterHashtable @{ LogName = '" + escapedName + "'; Level = " + level + " } -ErrorAction SilentlyContinue).Count; Write-Output $count";
        CommandRunResult result = await ProcessRunner.RunPowerShellAsync(script);
        if (result.ExitCode != 0)
        {
            return -1;
        }

        string? lastLine = result.OutputLines.LastOrDefault();
        return int.TryParse(lastLine, out int count) ? count : -1;
    }

    private bool IsExcludedLog(string logName)
    {
        if (_excludedExactLogs.Any(value => string.Equals(value, logName, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return _excludedWildcardLogs.Any(pattern => MatchesPattern(logName, pattern));
    }

    private void EnsureConfigStorage()
    {
        string? directory = Path.GetDirectoryName(_configPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private void LoadConfigFromDisk()
    {
        string json = File.ReadAllText(_configPath, Encoding.UTF8);
        ConfigModel? model = JsonSerializer.Deserialize<ConfigModel>(json);
        if (model == null)
        {
            return;
        }

        _excludedExactLogs.Clear();
        _excludedWildcardLogs.Clear();

        foreach (string item in model.ExcludedExact ?? Array.Empty<string>())
        {
            if (!string.IsNullOrWhiteSpace(item) && !_excludedExactLogs.Any(existing => string.Equals(existing, item, StringComparison.OrdinalIgnoreCase)))
            {
                _excludedExactLogs.Add(item);
            }
        }

        foreach (string item in model.ExcludedWildcard ?? Array.Empty<string>())
        {
            if (!string.IsNullOrWhiteSpace(item) && !_excludedWildcardLogs.Any(existing => string.Equals(existing, item, StringComparison.OrdinalIgnoreCase)))
            {
                _excludedWildcardLogs.Add(item);
            }
        }
    }

    private void ResetExclusionsToDefaultsCore()
    {
        _excludedExactLogs.Clear();
        _excludedWildcardLogs.Clear();
        _excludedExactLogs.AddRange(_defaultExact);
        _excludedWildcardLogs.AddRange(_defaultWildcard);
    }

    private string WriteReport(string prefix, IReadOnlyList<string> lines)
    {
        Directory.CreateDirectory(_reportsPath);
        string reportPath = Path.Combine(_reportsPath, "event-log-cleaner-" + prefix + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt");
        File.WriteAllText(reportPath, string.Join(Environment.NewLine, lines).TrimEnd() + Environment.NewLine, Encoding.UTF8);
        return reportPath;
    }

    private static bool MatchesPattern(string value, string pattern)
    {
        string regex = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
        return Regex.IsMatch(value, regex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static bool IsAdministrator()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private sealed class ConfigModel
    {
        public int SchemaVersion { get; set; }
        public DateTimeOffset SavedAt { get; set; }
        public string[]? ExcludedExact { get; set; }
        public string[]? ExcludedWildcard { get; set; }
    }

    private sealed class EventLogInfo
    {
        public EventLogInfo(string logName, long recordCount)
        {
            LogName = logName;
            RecordCount = recordCount;
        }

        public string LogName { get; }
        public long RecordCount { get; }
    }
}
