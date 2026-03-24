using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using TechnificationToolboxApp.Models;

namespace TechnificationToolboxApp.Services;

internal static class ProcessRunner
{
    public static async Task<CommandRunResult> RunAsync(string fileName, IReadOnlyList<string> arguments, IProgress<string>? progress = null, string? workingDirectory = null)
    {
        var outputLines = new List<string>();
        object gate = new object();

        using var process = new Process();
        process.StartInfo.FileName = fileName;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;

        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            process.StartInfo.WorkingDirectory = workingDirectory;
        }

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

        return new CommandRunResult(process.ExitCode, outputLines.ToArray());
    }

    public static Task<CommandRunResult> RunPowerShellAsync(string script, IProgress<string>? progress = null)
    {
        return RunAsync(
            ResolvePowerShellPath(),
            new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", script },
            progress);
    }

    public static string ResolvePowerShellPath()
    {
        string pwsh = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PowerShell", "7", "pwsh.exe");
        if (File.Exists(pwsh))
        {
            return pwsh;
        }

        return "pwsh.exe";
    }
}
