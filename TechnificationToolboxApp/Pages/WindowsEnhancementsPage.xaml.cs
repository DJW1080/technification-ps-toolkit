using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TechnificationToolboxApp.Models;

namespace TechnificationToolboxApp.Pages;

public sealed partial class WindowsEnhancementsPage : Page
{
    private readonly List<string> _latestReportPaths = new List<string>();

    public WindowsEnhancementsPage()
    {
        InitializeComponent();
        LoadStatus();
        LastActionTextBlock.Text = "No action run yet.";
        LastResultTextBlock.Text = "Idle";
        ReportPathsTextBox.Text = "No reports generated yet.";
        OpenLatestReportButton.IsEnabled = false;
    }

    private async void StatusButton_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync("Show enhancements status", progress => App.WindowsEnhancements.DescribeStatusAsync(progress));
    }

    private async void EnableHibernationButton_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync("Enable hibernation", progress => App.WindowsEnhancements.EnableHibernationAsync(progress));
    }

    private async void DisableHibernationButton_Click(object sender, RoutedEventArgs e)
    {
        if (!await ConfirmAsync("Disable hibernation?", "This turns off hibernation on the local machine."))
        {
            return;
        }

        await RunOperationAsync("Disable hibernation", progress => App.WindowsEnhancements.DisableHibernationAsync(progress));
    }

    private async void InstallContextMenuButton_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync("Install PowerShell admin context menu", progress => App.WindowsEnhancements.InstallAdminContextMenuAsync(progress));
    }

    private async void DiskCleanupButton_Click(object sender, RoutedEventArgs e)
    {
        if (!await ConfirmAsync("Run disk cleanup workflow?", "This performs the configured cleanup steps and can remove cached data and logs."))
        {
            return;
        }

        await RunOperationAsync("Run disk cleanup tool", progress => App.WindowsEnhancements.RunDiskCleanupAsync(CreateRestorePointCheckBox.IsChecked == true, progress));
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
        await RunUiActionAsync(() => App.Launcher.LaunchTool(GetModule(), false));
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
            await ShowMessageAsync("Windows enhancements action failed", ex.Message);
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
        WindowsEnhancementsStatus status = App.WindowsEnhancements.GetStatus();
        AdminStatusTextBlock.Text = status.IsAdministrator ? "Administrator" : "Standard session";
        HibernationStatusTextBlock.Text = status.HibernationEnabled == true ? "Enabled" : status.HibernationEnabled == false ? "Disabled" : "Unknown";
        ContextMenuStatusTextBlock.Text = status.ContextMenuInstalled ? "Installed" : "Not installed";
        LogsPathTextBlock.Text = status.LogsPath;
        ReportsPathTextBlock.Text = status.ReportsPath;
        EnhancementsCommandBar.IsEnabled = BusyProgressBar.Visibility != Visibility.Visible;
    }

    private void SetBusy(bool isBusy)
    {
        BusyProgressBar.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
        EnhancementsCommandBar.IsEnabled = !isBusy;
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

    private static ToolModule GetModule()
    {
        foreach (ToolModule module in App.Modules)
        {
            if (string.Equals(module.Name, "Windows Enhancements", StringComparison.Ordinal))
            {
                return module;
            }
        }

        throw new FileNotFoundException("Windows Enhancements module was not found in the configured tool list.");
    }
}

