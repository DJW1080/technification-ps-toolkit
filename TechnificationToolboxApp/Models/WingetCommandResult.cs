using System.Collections.Generic;

namespace TechnificationToolboxApp.Models;

public sealed class WingetCommandResult
{
    public WingetCommandResult(
        string actionName,
        bool succeeded,
        int exitCode,
        string commandText,
        string outputText,
        IReadOnlyList<string> reportPaths)
    {
        ActionName = actionName;
        Succeeded = succeeded;
        ExitCode = exitCode;
        CommandText = commandText;
        OutputText = outputText;
        ReportPaths = reportPaths;
    }

    public string ActionName { get; }
    public bool Succeeded { get; }
    public int ExitCode { get; }
    public string CommandText { get; }
    public string OutputText { get; }
    public IReadOnlyList<string> ReportPaths { get; }
}
