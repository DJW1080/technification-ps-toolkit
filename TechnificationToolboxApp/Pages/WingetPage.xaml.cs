using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TechnificationToolboxApp.Models;

namespace TechnificationToolboxApp.Pages;

public sealed partial class WingetPage : Page
{
    private readonly List<string> _latestReportPaths = new List<string>();

    public WingetPage()
    {
        InitializeComponent();
        LoadStatus();
        LastActionTextBlock.Text = "No action run yet.";
        LastResultTextBlock.Text = "Idle";
        ReportPathsTextBox.Text = "No reports generated yet.";
        OpenLatestReportButton.IsEnabled = false;
    }

    private async void RecommendedButton_Click(object sender, RoutedEventArgs e)
    {
        if (!await ConfirmAsync("Run recommended maintenance sequence?", "This will refresh sources, list upgrades, upgrade packages, and export inventory."))
        {
            return;
        }

        await RunOperationAsync("Recommended maintenance sequence", progress => App.WingetMaintenance.RunRecommendedSequenceAsync(progress));
    }

    private async void VersionButton_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync("Check Winget version", progress => App.WingetMaintenance.CheckVersionAsync(progress));
    }

    private async void RefreshSourcesButton_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync("Refresh Winget sources", progress => App.WingetMaintenance.RefreshSourcesAsync(progress));
    }

    private async void ListUpgradesButton_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync("List available upgrades", progress => App.WingetMaintenance.ListUpgradesAsync(progress));
    }

    private async void ExportInventoryButton_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync("Export installed package inventory", progress => App.WingetMaintenance.ExportInventoryAsync(progress));
    }

    private async void UpgradeAllButton_Click(object sender, RoutedEventArgs e)
    {
        if (!await ConfirmAsync("Upgrade all packages?", "This will install every pending Winget upgrade on the machine."))
        {
            return;
        }

        await RunOperationAsync("Upgrade all packages", progress => App.WingetMaintenance.UpgradeAllAsync(progress));
    }

    private async void ResetSourcesButton_Click(object sender, RoutedEventArgs e)
    {
        if (!await ConfirmAsync("Reset Winget sources?", "This restores Winget sources to their defaults."))
        {
            return;
        }

        await RunOperationAsync("Reset Winget sources", progress => App.WingetMaintenance.ResetSourcesAsync(progress));
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
        await RunUiActionAsync(() => App.Launcher.LaunchTool(GetWingetModule(), false));
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

    private async Task RunOperationAsync(string actionName, Func<IProgress<string>, Task<WingetCommandResult>> operation)
    {
        SetBusy(true);
        OutputConsole.Clear();
        LastActionTextBlock.Text = actionName;
        LastResultTextBlock.Text = "Running...";
        SetLatestReports(Array.Empty<string>());

        var progress = new Progress<string>(AppendOutputLine);

        try
        {
            WingetCommandResult result = await operation(progress);
            LastResultTextBlock.Text = result.Succeeded
                ? "Completed successfully (Exit Code " + result.ExitCode + ")"
                : "Failed (Exit Code " + result.ExitCode + ")";
            SetLatestReports(result.ReportPaths);
            LoadStatus();
        }
        catch (Exception ex)
        {
            LastResultTextBlock.Text = "Failed";
            AppendOutputLine(ex.Message);
            await ShowMessageAsync("Winget action failed", ex.Message);
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
        WingetEnvironmentStatus status = App.WingetMaintenance.GetStatus();
        WingetStatusTextBlock.Text = status.IsAvailable ? "Winget available" : "Winget not installed";
        WingetPathTextBlock.Text = status.ExecutablePath;
        ReportsPathTextBlock.Text = status.ReportsPath;

        bool commandsEnabled = status.IsAvailable && BusyProgressBar.Visibility != Visibility.Visible;
        RecommendedButton.IsEnabled = commandsEnabled;
        VersionButton.IsEnabled = commandsEnabled;
        RefreshSourcesButton.IsEnabled = commandsEnabled;
        ListUpgradesButton.IsEnabled = commandsEnabled;
        ExportInventoryButton.IsEnabled = commandsEnabled;
        UpgradeAllButton.IsEnabled = commandsEnabled;
        ResetSourcesButton.IsEnabled = commandsEnabled;
    }

    private void SetBusy(bool isBusy)
    {
        BusyProgressBar.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
        WingetCommandBar.IsEnabled = !isBusy;
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

    private async Task<bool> ConfirmAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = "Continue",
            CloseButtonText = "Cancel",
            XamlRoot = XamlRoot
        };

        return await dialog.ShowAsync() == ContentDialogResult.Primary;
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

    private static ToolModule GetWingetModule()
    {
        foreach (ToolModule module in App.Modules)
        {
            if (string.Equals(module.Name, "Winget Maintenance", StringComparison.Ordinal))
            {
                return module;
            }
        }

        throw new FileNotFoundException("Winget module was not found in the configured tool list.");
    }
}

