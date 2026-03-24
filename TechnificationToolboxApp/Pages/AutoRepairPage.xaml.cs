using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TechnificationToolboxApp.Models;

namespace TechnificationToolboxApp.Pages;

public sealed partial class AutoRepairPage : Page
{
    private readonly List<string> _latestReportPaths = new List<string>();

    public AutoRepairPage()
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
        bool createRestorePoint = CreateRestorePointCheckBox.IsChecked == true;
        string description = createRestorePoint
            ? "This will create a restore point, run the full repair pass, clean temp files, and flush DNS."
            : "This will run the full repair pass, clean temp files, and flush DNS without creating a restore point first.";

        if (!await ConfirmAsync("Run recommended maintenance sequence?", description))
        {
            return;
        }

        await RunOperationAsync("Recommended maintenance sequence", progress => App.AutoRepair.RunRecommendedMaintenanceAsync(createRestorePoint, progress));
    }

    private async void FullRepairButton_Click(object sender, RoutedEventArgs e)
    {
        bool createRestorePoint = CreateRestorePointCheckBox.IsChecked == true;
        string description = createRestorePoint
            ? "This will create a restore point first, then run DISM and SFC actions in sequence."
            : "This will run DISM and SFC actions in sequence without creating a restore point first.";

        if (!await ConfirmAsync("Run full repair pass?", description))
        {
            return;
        }

        await RunOperationAsync("Windows full repair", progress => App.AutoRepair.RunFullRepairAsync(createRestorePoint, progress));
    }

    private async void SfcButton_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync("System File Checker", progress => App.AutoRepair.RunSfcAsync(progress));
    }

    private async void RestoreHealthButton_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync("DISM RestoreHealth", progress => App.AutoRepair.RunDismRestoreHealthAsync(progress));
    }

    private async void ExportReportButton_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync("Export system health report", progress => App.AutoRepair.ExportSystemHealthReportAsync(progress));
    }

    private async void CheckHealthButton_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync("DISM CheckHealth", progress => App.AutoRepair.RunDismCheckHealthAsync(progress));
    }

    private async void ScanHealthButton_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync("DISM ScanHealth", progress => App.AutoRepair.RunDismScanHealthAsync(progress));
    }

    private async void CleanupButton_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync("DISM Component Cleanup", progress => App.AutoRepair.RunComponentCleanupAsync(progress));
    }

    private async void ResetBaseButton_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync("DISM Component Cleanup ResetBase", progress => App.AutoRepair.RunComponentCleanupResetBaseAsync(progress));
    }

    private async void TempFilesButton_Click(object sender, RoutedEventArgs e)
    {
        if (!await ConfirmAsync("Remove temporary files?", "This removes temp, prefetch, and Windows Update download leftovers."))
        {
            return;
        }

        await RunOperationAsync("Remove temporary files", progress => App.AutoRepair.RemoveTempFilesAsync(progress));
    }

    private async void NetworkResetButton_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync("Reset network stack", progress => App.AutoRepair.ResetNetworkStackAsync(progress));
    }

    private async void WindowsUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        if (!await ConfirmAsync("Repair Windows Update?", "This restarts update services and clears Windows Update caches."))
        {
            return;
        }

        await RunOperationAsync("Repair Windows Update", progress => App.AutoRepair.RepairWindowsUpdateAsync(progress));
    }

    private async void RestorePointButton_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync("Create restore point", progress => App.AutoRepair.CreateRestorePointAsync(progress));
    }

    private async void DiagnosticsButton_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync("Run diagnostics", progress => App.AutoRepair.RunDiagnosticsAsync(progress));
    }

    private async void FlushDnsButton_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync("Flush DNS cache", progress => App.AutoRepair.FlushDnsCacheAsync(progress));
    }

    private async void ChkDskButton_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync("CHKDSK online scan", progress => App.AutoRepair.RunDiskCheckScanAsync(progress));
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
        await RunUiActionAsync(() => App.Launcher.LaunchTool(GetAutoRepairModule(), false));
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

    private async Task RunOperationAsync(string actionName, Func<IProgress<string>, Task<NativeToolResult>> operation)
    {
        SetBusy(true);
        OutputConsole.Clear();
        LastActionTextBlock.Text = actionName;
        LastResultTextBlock.Text = "Running...";
        SetLatestReports(Array.Empty<string>());

        var progress = new Progress<string>(AppendOutputLine);

        try
        {
            NativeToolResult result = await operation(progress);
            LastResultTextBlock.Text = result.Succeeded ? result.SummaryText : "Failed: " + result.SummaryText;
            SetLatestReports(result.ReportPaths);
            LoadStatus();
        }
        catch (Exception ex)
        {
            LastResultTextBlock.Text = "Failed";
            AppendOutputLine(ex.Message);
            await ShowMessageAsync("Auto repair action failed", ex.Message);
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
        AutoRepairStatus status = App.AutoRepair.GetStatus();
        AdminStatusTextBlock.Text = status.IsAdministrator ? "Administrator" : "Standard session";
        LogsPathTextBlock.Text = status.LogsPath;
        ReportsPathTextBlock.Text = status.ReportsPath;
        AutoRepairCommandBar.IsEnabled = BusyProgressBar.Visibility != Visibility.Visible;
    }

    private void SetBusy(bool isBusy)
    {
        BusyProgressBar.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
        AutoRepairCommandBar.IsEnabled = !isBusy;
        CreateRestorePointCheckBox.IsEnabled = !isBusy;
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

    private static ToolModule GetAutoRepairModule()
    {
        foreach (ToolModule module in App.Modules)
        {
            if (string.Equals(module.Name, "Windows Auto Repair", StringComparison.Ordinal))
            {
                return module;
            }
        }

        throw new FileNotFoundException("Windows Auto Repair module was not found in the configured tool list.");
    }
}

