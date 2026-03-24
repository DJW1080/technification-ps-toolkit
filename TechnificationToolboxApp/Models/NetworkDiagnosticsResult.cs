using System.Collections.Generic;

namespace TechnificationToolboxApp.Models;

public sealed class NetworkDiagnosticsResult
{
    public NetworkDiagnosticsResult(string actionName, bool succeeded, string summaryText, string outputText, IReadOnlyList<string> reportPaths)
    {
        ActionName = actionName;
        Succeeded = succeeded;
        SummaryText = summaryText;
        OutputText = outputText;
        ReportPaths = reportPaths;
    }

    public string ActionName { get; }
    public bool Succeeded { get; }
    public string SummaryText { get; }
    public string OutputText { get; }
    public IReadOnlyList<string> ReportPaths { get; }
}
