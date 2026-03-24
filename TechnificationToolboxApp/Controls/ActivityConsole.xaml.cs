using System;
using System.Collections.ObjectModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace TechnificationToolboxApp.Controls;

public sealed partial class ActivityConsole : UserControl
{
    public ActivityConsole()
    {
        InitializeComponent();
        Lines = new ObservableCollection<ActivityConsoleLine>();
    }

    public ObservableCollection<ActivityConsoleLine> Lines { get; }

    public void Clear()
    {
        Lines.Clear();
    }

    public void AppendLine(string line)
    {
        string displayText = string.IsNullOrEmpty(line) ? " " : line;
        Lines.Add(new ActivityConsoleLine(displayText, ResolveLineBrush(line ?? string.Empty)));
        QueueScrollToBottom();
    }

    private void QueueScrollToBottom()
    {
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            ConsoleScrollViewer.ChangeView(null, ConsoleScrollViewer.ScrollableHeight, null, true);
        });
    }

    private Brush ResolveLineBrush(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return ResolveBrush("ConsoleForegroundBrush");
        }

        if (line.StartsWith("[FAIL]", StringComparison.OrdinalIgnoreCase) || line.Contains("Result          : FAILED", StringComparison.OrdinalIgnoreCase))
        {
            return ResolveBrush("ConsoleFailBrush");
        }

        if (line.StartsWith("[WARN]", StringComparison.OrdinalIgnoreCase) || line.StartsWith("Skipping", StringComparison.OrdinalIgnoreCase))
        {
            return ResolveBrush("ConsoleWarnBrush");
        }

        if (line.StartsWith("[GOOD]", StringComparison.OrdinalIgnoreCase) || line.Contains("Result          : CLEARED", StringComparison.OrdinalIgnoreCase))
        {
            return ResolveBrush("ConsoleGoodBrush");
        }

        if (line.StartsWith("[INFO]", StringComparison.OrdinalIgnoreCase))
        {
            return ResolveBrush("ConsoleInfoBrush");
        }

        if (line.StartsWith("Command :", StringComparison.OrdinalIgnoreCase) ||
            line.StartsWith("Report :", StringComparison.OrdinalIgnoreCase) ||
            line.StartsWith("Mode     :", StringComparison.OrdinalIgnoreCase) ||
            line.StartsWith("Counts   :", StringComparison.OrdinalIgnoreCase))
        {
            return ResolveBrush("ConsoleCommandBrush");
        }

        if (line.StartsWith("Exit Code :", StringComparison.OrdinalIgnoreCase))
        {
            return line.EndsWith("0", StringComparison.Ordinal)
                ? ResolveBrush("ConsoleGoodBrush")
                : ResolveBrush("ConsoleFailBrush");
        }

        return ResolveBrush("ConsoleForegroundBrush");
    }

    private static Brush ResolveBrush(string resourceKey)
    {
        if (Application.Current.Resources.TryGetValue(resourceKey, out object value) && value is Brush brush)
        {
            return brush;
        }

        return new SolidColorBrush(Microsoft.UI.Colors.White);
    }
}

public sealed class ActivityConsoleLine
{
    public ActivityConsoleLine(string text, Brush foregroundBrush)
    {
        Text = text;
        ForegroundBrush = foregroundBrush;
    }

    public string Text { get; }
    public Brush ForegroundBrush { get; }
}
