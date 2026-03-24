using System.Collections.Generic;

namespace TechnificationToolboxApp.Models;

public sealed class NativeToolResult
{
    public NativeToolResult(string actionName, bool succeeded, int exitCode, string summaryText, string outputText, IReadOnlyList<string> reportPaths)
    {
        ActionName = actionName;
        Succeeded = succeeded;
        ExitCode = exitCode;
        SummaryText = summaryText;
        OutputText = outputText;
        ReportPaths = reportPaths;
    }

    public string ActionName { get; }
    public bool Succeeded { get; }
    public int ExitCode { get; }
    public string SummaryText { get; }
    public string OutputText { get; }
    public IReadOnlyList<string> ReportPaths { get; }
}
