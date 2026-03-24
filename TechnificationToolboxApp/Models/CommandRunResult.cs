using System.Collections.Generic;

namespace TechnificationToolboxApp.Models;

public sealed class CommandRunResult
{
    public CommandRunResult(int exitCode, IReadOnlyList<string> outputLines)
    {
        ExitCode = exitCode;
        OutputLines = outputLines;
    }

    public int ExitCode { get; }
    public IReadOnlyList<string> OutputLines { get; }
    public string OutputText => string.Join(System.Environment.NewLine, OutputLines).TrimEnd();
}
