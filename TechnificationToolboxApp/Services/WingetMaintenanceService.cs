using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechnificationToolboxApp.Models;

namespace TechnificationToolboxApp.Services;

public sealed class WingetMaintenanceService
{
    private readonly string _reportsPath;

    public WingetMaintenanceService(string reportsPath)
    {
        _reportsPath = reportsPath;
    }

    public WingetEnvironmentStatus GetStatus()
    {
        string? executablePath = ResolveWingetExecutable();
        return new WingetEnvironmentStatus(
            executablePath != null,
            executablePath ?? "Winget not found",
            _reportsPath);
    }

    public Task<WingetCommandResult> RunRecommendedSequenceAsync(IProgress<string>? progress)
    {
        return RunCompositeActionAsync(
            "Recommended maintenance sequence",
            new[]
            {
                new WingetStep("Checking Winget version", new[] { "--version" }, false),
                new WingetStep("Refreshing Winget sources", new[] { "source", "update" }, false),
                new WingetStep("Listing available package upgrades", new[] { "upgrade", "--accept-source-agreements" }, true),
                new WingetStep("Upgrading all Winget packages", new[] { "upgrade", "--all", "--accept-source-agreements", "--accept-package-agreements", "--include-unknown" }, false),
                new WingetStep("Exporting Winget package inventory", new[] { "list", "--accept-source-agreements" }, true)
            },
            progress);
    }

    public Task<WingetCommandResult> CheckVersionAsync(IProgress<string>? progress)
    {
        return RunCommandAsync("Checking Winget version", new[] { "--version" }, false, progress);
    }

    public Task<WingetCommandResult> RefreshSourcesAsync(IProgress<string>? progress)
    {
        return RunCommandAsync("Refreshing Winget sources", new[] { "source", "update" }, false, progress);
    }

    public Task<WingetCommandResult> ListUpgradesAsync(IProgress<string>? progress)
    {
        return RunCommandAsync("Listing available package upgrades", new[] { "upgrade", "--accept-source-agreements" }, true, progress);
    }

    public Task<WingetCommandResult> UpgradeAllAsync(IProgress<string>? progress)
    {
        return RunCommandAsync(
            "Upgrading all Winget packages",
            new[] { "upgrade", "--all", "--accept-source-agreements", "--accept-package-agreements", "--include-unknown" },
            false,
            progress);
    }

    public Task<WingetCommandResult> ExportInventoryAsync(IProgress<string>? progress)
    {
        return RunCommandAsync("Exporting Winget package inventory", new[] { "list", "--accept-source-agreements" }, true, progress);
    }

    public Task<WingetCommandResult> ResetSourcesAsync(IProgress<string>? progress)
    {
        return RunCommandAsync("Resetting Winget sources", new[] { "source", "reset", "--force" }, false, progress);
    }

    private async Task<WingetCommandResult> RunCompositeActionAsync(string actionName, IReadOnlyList<WingetStep> steps, IProgress<string>? progress)
    {
        var combinedOutput = new StringBuilder();
        var reportPaths = new List<string>();
        int exitCode = 0;

        progress?.Report("[INFO] Running recommended Winget maintenance sequence...");
        foreach (WingetStep step in steps)
        {
            if (combinedOutput.Length > 0)
            {
                combinedOutput.AppendLine();
                progress?.Report(string.Empty);
            }

            WingetCommandResult result = await RunCommandAsync(step.Label, step.Arguments, step.CaptureReport, progress);
            if (!string.IsNullOrWhiteSpace(result.OutputText))
            {
                combinedOutput.AppendLine(result.OutputText);
            }

            foreach (string reportPath in result.ReportPaths)
            {
                reportPaths.Add(reportPath);
            }

            exitCode = result.ExitCode;
            if (!result.Succeeded)
            {
                progress?.Report("[WARN] Recommended maintenance sequence stopped because a step failed.");
                return new WingetCommandResult(actionName, false, exitCode, actionName, combinedOutput.ToString().TrimEnd(), reportPaths);
            }
        }

        progress?.Report("[GOOD] Recommended Winget maintenance sequence completed.");
        return new WingetCommandResult(actionName, true, exitCode, actionName, combinedOutput.ToString().TrimEnd(), reportPaths);
    }

    private async Task<WingetCommandResult> RunCommandAsync(string actionName, IReadOnlyList<string> arguments, bool captureReport, IProgress<string>? progress)
    {
        string? wingetExecutable = ResolveWingetExecutable();
        string commandText = BuildCommandText(wingetExecutable ?? "winget.exe", arguments);

        if (string.IsNullOrWhiteSpace(wingetExecutable))
        {
            const string message = "Winget is not available on this system. Install App Installer and try again.";
            progress?.Report("[FAIL] " + message);
            return new WingetCommandResult(actionName, false, -1, commandText, message, Array.Empty<string>());
        }

        progress?.Report("[INFO] " + actionName + "...");
        progress?.Report("Command : " + commandText);

        var outputLines = new List<string>();
        object gate = new object();

        using var process = new Process();
        process.StartInfo.FileName = wingetExecutable;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;

        foreach (string argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        void HandleLine(string? line)
        {
            if (line == null)
            {
                return;
            }

            lock (gate)
            {
                outputLines.Add(line);
            }

            progress?.Report(line);
        }

        process.OutputDataReceived += (_, args) => HandleLine(args.Data);
        process.ErrorDataReceived += (_, args) => HandleLine(args.Data);

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();
        process.WaitForExit();

        if (outputLines.Count == 0)
        {
            progress?.Report("Winget returned no console output.");
        }

        progress?.Report("Exit Code : " + process.ExitCode);

        var reportPaths = new List<string>();
        if (captureReport)
        {
            Directory.CreateDirectory(_reportsPath);
            string reportPath = Path.Combine(_reportsPath, BuildReportFileName(actionName));
            File.WriteAllText(reportPath, BuildReportText(outputLines), Encoding.UTF8);
            reportPaths.Add(reportPath);
            progress?.Report("Report : " + reportPath);
        }

        bool succeeded = process.ExitCode == 0;
        progress?.Report(succeeded ? "[GOOD] " + actionName + " completed." : "[FAIL] " + actionName + " failed.");

        return new WingetCommandResult(
            actionName,
            succeeded,
            process.ExitCode,
            commandText,
            string.Join(Environment.NewLine, outputLines).TrimEnd(),
            reportPaths);
    }

    private static string BuildCommandText(string executablePath, IReadOnlyList<string> arguments)
    {
        return executablePath + " " + string.Join(" ", arguments.Select(QuoteForDisplay));
    }

    private static string QuoteForDisplay(string value)
    {
        return value.Contains(' ') ? "\"" + value + "\"" : value;
    }

    private static string BuildReportFileName(string actionName)
    {
        string prefix = new string(actionName
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray())
            .Trim('-');

        while (prefix.Contains("--", StringComparison.Ordinal))
        {
            prefix = prefix.Replace("--", "-", StringComparison.Ordinal);
        }

        return prefix + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt";
    }

    private static string BuildReportText(IReadOnlyList<string> outputLines)
    {
        List<Dictionary<string, string>> tableRows = ParseWingetTable(outputLines);
        if (tableRows.Count == 0)
        {
            return string.Join(Environment.NewLine, outputLines).TrimEnd();
        }

        string[] columns = new[] { "Name", "Version", "Available", "Source" }
            .Where(column => tableRows[0].ContainsKey(column))
            .ToArray();

        var widths = new Dictionary<string, int>();
        foreach (string column in columns)
        {
            int width = column.Length;
            foreach (Dictionary<string, string> row in tableRows)
            {
                width = Math.Max(width, row.TryGetValue(column, out string? value) ? value.Length : 0);
            }

            widths[column] = width;
        }

        var builder = new StringBuilder();
        builder.AppendLine(string.Join("  ", columns.Select(column => column.PadRight(widths[column]))));
        builder.AppendLine(string.Join("  ", columns.Select(column => new string('-', widths[column]))));

        foreach (Dictionary<string, string> row in tableRows)
        {
            builder.AppendLine(string.Join("  ", columns.Select(column => (row.TryGetValue(column, out string? value) ? value : string.Empty).PadRight(widths[column]))));
        }

        return builder.ToString().TrimEnd();
    }

    private static List<Dictionary<string, string>> ParseWingetTable(IReadOnlyList<string> lines)
    {
        int headerIndex = -1;
        for (int index = 0; index < lines.Count; index++)
        {
            string line = lines[index];
            if (line.Contains("Name", StringComparison.Ordinal) && line.Contains("Id", StringComparison.Ordinal))
            {
                headerIndex = index;
                break;
            }
        }

        if (headerIndex < 0 || headerIndex + 1 >= lines.Count)
        {
            return new List<Dictionary<string, string>>();
        }

        string headerLine = lines[headerIndex];
        string dividerLine = lines[headerIndex + 1];
        if (!dividerLine.TrimStart().StartsWith("---", StringComparison.Ordinal))
        {
            return new List<Dictionary<string, string>>();
        }

        var starts = new Dictionary<string, int>();
        foreach (string columnName in new[] { "Name", "Id", "Version", "Available", "Source" })
        {
            int start = headerLine.IndexOf(columnName, StringComparison.Ordinal);
            if (start >= 0)
            {
                starts[columnName] = start;
            }
        }

        if (!starts.ContainsKey("Id") || starts.Count < 3)
        {
            return new List<Dictionary<string, string>>();
        }

        var orderedColumns = starts.OrderBy(item => item.Value).ToList();
        var rows = new List<Dictionary<string, string>>();

        for (int index = headerIndex + 2; index < lines.Count; index++)
        {
            string line = lines[index];
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            string trimmed = line.Trim();
            if (trimmed.All(character => character == '\\' || character == '/' || character == '-' || character == '|' || character == ' '))
            {
                continue;
            }

            var row = new Dictionary<string, string>();
            for (int columnIndex = 0; columnIndex < orderedColumns.Count; columnIndex++)
            {
                KeyValuePair<string, int> column = orderedColumns[columnIndex];
                int start = column.Value;
                int end = columnIndex < orderedColumns.Count - 1 ? orderedColumns[columnIndex + 1].Value : line.Length;

                string value;
                if (start >= line.Length)
                {
                    value = string.Empty;
                }
                else
                {
                    int length = Math.Max(Math.Min(end, line.Length) - start, 0);
                    value = line.Substring(start, length).Trim();
                }

                row[column.Key] = value;
            }

            if (row.TryGetValue("Name", out string? name) && !string.IsNullOrWhiteSpace(name))
            {
                rows.Add(row);
            }
        }

        return rows;
    }

    private static string? ResolveWingetExecutable()
    {
        string windowsAppsWinget = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft",
            "WindowsApps",
            "winget.exe");

        if (File.Exists(windowsAppsWinget))
        {
            return windowsAppsWinget;
        }

        string? pathValue = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathValue))
        {
            return null;
        }

        foreach (string segment in pathValue.Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(segment))
            {
                continue;
            }

            try
            {
                string candidate = Path.Combine(segment.Trim(), "winget.exe");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            catch
            {
                // Ignore malformed PATH entries and continue scanning.
            }
        }

        return null;
    }

    private sealed class WingetStep
    {
        public WingetStep(string label, string[] arguments, bool captureReport)
        {
            Label = label;
            Arguments = arguments;
            CaptureReport = captureReport;
        }

        public string Label { get; }
        public string[] Arguments { get; }
        public bool CaptureReport { get; }
    }
}
