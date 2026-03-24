using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using Microsoft.UI.Xaml;
using TechnificationToolboxApp.Models;

namespace TechnificationToolboxApp.Services;

public sealed class ToolkitLaunchService
{
    public ToolkitLaunchService(string scriptsRoot)
    {
        ScriptsRoot = scriptsRoot;
    }

    public string ScriptsRoot { get; }

    public string LogsPath
    {
        get { return Path.Combine(GetTechnificationRootPath(), "Logs"); }
    }

    public string ReportsPath
    {
        get { return Path.Combine(GetTechnificationRootPath(), "Reports"); }
    }

    public void LaunchToolbox(bool runAsAdministrator = false)
    {
        LaunchScript("technification-toolbox.ps1", runAsAdministrator);
    }

    public void LaunchTool(ToolModule module, bool runAsAdministrator = false)
    {
        LaunchScript(module.ScriptRelativePath, runAsAdministrator || module.RequiresAdministrator);
    }

    public bool IsApplicationRunningAsAdministrator()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public void RelaunchApplicationAsAdministrator()
    {
        string? executablePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            executablePath = Process.GetCurrentProcess().MainModule?.FileName;
        }

        if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
        {
            throw new FileNotFoundException("The current application executable could not be resolved.");
        }

        string[] arguments = Environment.GetCommandLineArgs().Skip(1).ToArray();

        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            WorkingDirectory = Path.GetDirectoryName(executablePath) ?? AppContext.BaseDirectory,
            UseShellExecute = true,
            Verb = "runas"
        };

        if (arguments.Length > 0)
        {
            startInfo.Arguments = string.Join(" ", arguments.Select(QuoteArgument));
        }

        Process.Start(startInfo);
    }

    public void OpenLogsFolder()
    {
        OpenFolder(LogsPath);
    }

    public void OpenReportsFolder()
    {
        OpenFolder(ReportsPath);
    }

    public void OpenScriptsFolder()
    {
        OpenFolder(ScriptsRoot);
    }

    private void LaunchScript(string relativeScriptPath, bool runAsAdministrator)
    {
        string scriptPath = ResolveScriptPath(relativeScriptPath);
        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException($"Toolkit script not found: {scriptPath}");
        }

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = ResolvePowerShellPath(),
            Arguments = $"-NoExit -ExecutionPolicy Bypass -File \"{scriptPath}\"",
            WorkingDirectory = Path.GetDirectoryName(scriptPath) ?? ScriptsRoot,
            UseShellExecute = true
        };

        if (runAsAdministrator)
        {
            startInfo.Verb = "runas";
        }

        Process.Start(startInfo);
    }

    private string ResolveScriptPath(string relativeScriptPath)
    {
        string normalized = relativeScriptPath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        return Path.Combine(ScriptsRoot, normalized);
    }

    private static string ResolvePowerShellPath()
    {
        string pwsh = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PowerShell", "7", "pwsh.exe");
        if (File.Exists(pwsh))
        {
            return pwsh;
        }

        return "pwsh.exe";
    }

    private static string GetTechnificationRootPath()
    {
        string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        if (!string.IsNullOrWhiteSpace(programData))
        {
            return Path.Combine(programData, "Technification");
        }

        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Technification");
    }

    private static void OpenFolder(string path)
    {
        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }

    private static string QuoteArgument(string argument)
    {
        if (string.IsNullOrEmpty(argument))
        {
            return "\"\"";
        }

        if (!argument.Any(char.IsWhiteSpace) && !argument.Contains('"', StringComparison.Ordinal))
        {
            return argument;
        }

        return "\"" + argument.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
    }
}
