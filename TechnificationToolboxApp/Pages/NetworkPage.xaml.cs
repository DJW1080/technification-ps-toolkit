using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TechnificationToolboxApp.Models;

namespace TechnificationToolboxApp.Pages;

public sealed partial class NetworkPage : Page
{
    private readonly List<string> _latestReportPaths = new List<string>();

    public NetworkPage()
    {
        InitializeComponent();
        LoadStatus();
        LastActionTextBlock.Text = "No action run yet.";
        LastResultTextBlock.Text = "Idle";
        ReportPathsTextBox.Text = "No reports generated yet.";
        OpenLatestReportButton.IsEnabled = false;
    }

    private async void SummaryButton_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync("Show network summary", progress => App.NetworkDiagnostics.ShowNetworkSummaryAsync(progress));
    }

    private async void GatewayButton_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync("Test gateway reachability", progress => App.NetworkDiagnostics.TestDefaultGatewayReachabilityAsync(progress));
    }

    private async void DnsButton_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync("Test DNS resolution", progress => App.NetworkDiagnostics.TestDnsResolutionAsync(DnsHostTextBox.Text, progress));
    }

    private async void InternetButton_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync("Test internet connectivity", progress => App.NetworkDiagnostics.TestInternetConnectivityAsync(PingTargetTextBox.Text, progress));
    }

    private async void ConnectionsButton_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync("Show active TCP connections", progress => App.NetworkDiagnostics.ShowActiveConnectionsAsync(progress));
    }

    private async void CommonPortsButton_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync("Scan common TCP ports", progress => App.NetworkDiagnostics.ScanCommonPortsAsync(ScanTargetTextBox.Text, progress));
    }

    private async void CustomPortsButton_Click(object sender, RoutedEventArgs e)
    {
        IReadOnlyList<int> ports = ParsePorts(CustomPortsTextBox.Text);
        if (ports.Count == 0)
        {
            await ShowMessageAsync("Invalid ports", "Enter one or more comma-separated TCP ports between 1 and 65535.");
            return;
        }

        if (string.IsNullOrWhiteSpace(ScanTargetTextBox.Text))
        {
            await ShowMessageAsync("Scan target required", "Enter a host name or IP address before running a custom port scan.");
            return;
        }

        await RunOperationAsync("Scan custom TCP ports", progress => App.NetworkDiagnostics.ScanCustomPortsAsync(ScanTargetTextBox.Text, ports, progress));
    }

    private async void QuickCheckButton_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync("Run quick health check", progress => App.NetworkDiagnostics.RunQuickHealthCheckAsync(DnsHostTextBox.Text, PingTargetTextBox.Text, progress));
    }

    private async void ExportReportButton_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync("Export network report", progress => App.NetworkDiagnostics.ExportReportAsync(progress));
    }

    private void RefreshStatusButton_Click(object sender, RoutedEventArgs e)
    {
        LoadStatus();
    }

    private void OpenReportsButton_Click(object sender, RoutedEventArgs e)
    {
        App.Launcher.OpenReportsFolder();
    }

    private void OpenLogsButton_Click(object sender, RoutedEventArgs e)
    {
        App.Launcher.OpenLogsFolder();
    }

    private async void LegacyMenuButton_Click(object sender, RoutedEventArgs e)
    {
        await RunUiActionAsync(() => App.Launcher.LaunchTool(GetNetworkModule(), false));
    }

    private async void OpenLatestReportButton_Click(object sender, RoutedEventArgs e)
    {
        if (_latestReportPaths.Count == 0)
        {
            return;
        }

        string latestReportPath = _latestReportPaths[_latestReportPaths.Count - 1];
        await RunUiActionAsync(() =>
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = latestReportPath,
                UseShellExecute = true
            });
        });
    }

    private async Task RunOperationAsync(string actionName, Func<IProgress<string>, Task<NetworkDiagnosticsResult>> operation)
    {
        SetBusy(true);
        OutputConsole.Clear();
        LastActionTextBlock.Text = actionName;
        LastResultTextBlock.Text = "Running...";
        SetLatestReports(Array.Empty<string>());

        var progress = new Progress<string>(AppendOutputLine);

        try
        {
            NetworkDiagnosticsResult result = await operation(progress);
            LastResultTextBlock.Text = result.Succeeded ? result.SummaryText : "Failed: " + result.SummaryText;
            SetLatestReports(result.ReportPaths);
            LoadStatus();
        }
        catch (Exception ex)
        {
            LastResultTextBlock.Text = "Failed";
            AppendOutputLine(ex.Message);
            await ShowMessageAsync("Network action failed", ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task RunUiActionAsync(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("Action failed", ex.Message);
        }
    }

    private void LoadStatus()
    {
        NetworkEnvironmentStatus status = App.NetworkDiagnostics.GetStatus();
        AdapterCountTextBlock.Text = status.AdapterCount.ToString();
        LogsPathTextBlock.Text = status.LogsPath;
        ReportsPathTextBlock.Text = status.ReportsPath;

        if (string.IsNullOrWhiteSpace(DnsHostTextBox.Text))
        {
            DnsHostTextBox.Text = status.DefaultDnsHost;
        }

        if (string.IsNullOrWhiteSpace(PingTargetTextBox.Text))
        {
            PingTargetTextBox.Text = status.DefaultPingTarget;
        }

        if (string.IsNullOrWhiteSpace(ScanTargetTextBox.Text))
        {
            ScanTargetTextBox.Text = status.DefaultScanTarget;
        }

        if (string.IsNullOrWhiteSpace(CustomPortsTextBox.Text))
        {
            CustomPortsTextBox.Text = "22,80,443,3389";
        }

        bool commandsEnabled = BusyProgressBar.Visibility != Visibility.Visible;
        SummaryButton.IsEnabled = commandsEnabled;
        GatewayButton.IsEnabled = commandsEnabled;
        DnsButton.IsEnabled = commandsEnabled;
        InternetButton.IsEnabled = commandsEnabled;
        ConnectionsButton.IsEnabled = commandsEnabled;
        CommonPortsButton.IsEnabled = commandsEnabled;
        CustomPortsButton.IsEnabled = commandsEnabled;
        QuickCheckButton.IsEnabled = commandsEnabled;
        ExportReportButton.IsEnabled = commandsEnabled;
    }

    private void SetBusy(bool isBusy)
    {
        BusyProgressBar.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
        NetworkCommandBar.IsEnabled = !isBusy;
        DnsHostTextBox.IsEnabled = !isBusy;
        PingTargetTextBox.IsEnabled = !isBusy;
        ScanTargetTextBox.IsEnabled = !isBusy;
        CustomPortsTextBox.IsEnabled = !isBusy;
        OpenLatestReportButton.IsEnabled = !isBusy && _latestReportPaths.Count > 0;
        LoadStatus();
    }

    private void SetLatestReports(IReadOnlyList<string> reportPaths)
    {
        _latestReportPaths.Clear();
        foreach (string reportPath in reportPaths)
        {
            _latestReportPaths.Add(reportPath);
        }

        ReportPathsTextBox.Text = _latestReportPaths.Count == 0
            ? "No reports generated in the last run."
            : string.Join(Environment.NewLine, _latestReportPaths);
        OpenLatestReportButton.IsEnabled = _latestReportPaths.Count > 0 && BusyProgressBar.Visibility != Visibility.Visible;
    }

    private void AppendOutputLine(string line)
    {
        OutputConsole.AppendLine(line);
    }

    private async Task ShowMessageAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "Close",
            XamlRoot = XamlRoot
        };

        await dialog.ShowAsync();
    }

    private static IReadOnlyList<int> ParsePorts(string text)
    {
        return text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => int.TryParse(part, out int port) ? port : -1)
            .Where(port => port >= 1 && port <= 65535)
            .Distinct()
            .OrderBy(port => port)
            .ToArray();
    }

    private static ToolModule GetNetworkModule()
    {
        foreach (ToolModule module in App.Modules)
        {
            if (string.Equals(module.Name, "Network Diagnostics Suite", StringComparison.Ordinal))
            {
                return module;
            }
        }

        throw new FileNotFoundException("Network diagnostics module was not found in the configured tool list.");
    }
}

