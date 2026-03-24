using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TechnificationToolboxApp.Models;

namespace TechnificationToolboxApp.Pages;

public sealed partial class UserProfileCleanupPage : Page
{
    private readonly List<string> _latestReportPaths = new List<string>();

    public UserProfileCleanupPage()
    {
        InitializeComponent();
        LoadStatus();
        LastActionTextBlock.Text = "No action run yet.";
        LastResultTextBlock.Text = "Idle";
        ReportPathsTextBox.Text = "No reports generated yet.";
        OpenLatestReportButton.IsEnabled = false;
    }

    private async void DeepScanButton_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync("Deep scan current user profile", progress => App.UserProfileCleanup.DeepScanAsync(progress));
    }

    private async void CategoriesButton_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync("Show cleanup categories", progress => App.UserProfileCleanup.ShowCategorySummaryAsync(progress));
    }

    private async void TempButton_Click(object sender, RoutedEventArgs e)
    {
        await RunCleanupAsync("Temp", progress => App.UserProfileCleanup.CleanTempAsync(progress));
    }

    private async void BrowserButton_Click(object sender, RoutedEventArgs e)
    {
        await RunCleanupAsync("Browser Cache", progress => App.UserProfileCleanup.CleanBrowserCacheAsync(progress));
    }

    private async void AppCacheButton_Click(object sender, RoutedEventArgs e)
    {
        await RunCleanupAsync("App Cache", progress => App.UserProfileCleanup.CleanAppCacheAsync(progress));
    }

    private async void CrashDataButton_Click(object sender, RoutedEventArgs e)
    {
        await RunCleanupAsync("Crash Data", progress => App.UserProfileCleanup.CleanCrashDataAsync(progress));
    }

    private async void LogsButton_Click(object sender, RoutedEventArgs e)
    {
        await RunCleanupAsync("Logs", progress => App.UserProfileCleanup.CleanLogsAsync(progress));
    }

    private async void LockedButton_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync("Show locked or skipped files", progress => App.UserProfileCleanup.ShowLockedItemsAsync(progress));
    }

    private async void ExclusionsButton_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync("Show exclusions", progress => App.UserProfileCleanup.ShowExclusionsAsync(progress));
    }

    private async void AddExclusionButton_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync("Add exclusion pattern", progress => App.UserProfileCleanup.AddExclusionPatternAsync(ExclusionPatternTextBox.Text, progress));
    }

    private async void RemoveExclusionButton_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync("Remove exclusion pattern", progress => App.UserProfileCleanup.RemoveExclusionPatternAsync(ExclusionPatternTextBox.Text, progress));
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

    private async Task RunCleanupAsync(string category, Func<IProgress<string>, Task<NativeToolResult>> operation)
    {
        if (!await ConfirmAsync("Clean " + category + "?", "This removes files in the " + category + " category from the current user profile."))
        {
            return;
        }

        await RunOperationAsync("Clean " + category, operation);
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
            await ShowMessageAsync("Cleanup action failed", ex.Message);
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
        UserProfileCleanupStatus status = App.UserProfileCleanup.GetStatus();
        TargetCountTextBlock.Text = status.TargetCount.ToString();
        ExclusionCountTextBlock.Text = status.ExclusionCount.ToString();
        LockedCountTextBlock.Text = status.LockedItemCount.ToString();
        LogsPathTextBlock.Text = status.LogsPath;
        ReportsPathTextBlock.Text = status.ReportsPath;
        CleanupCommandBar.IsEnabled = BusyProgressBar.Visibility != Visibility.Visible;
    }

    private void SetBusy(bool isBusy)
    {
        BusyProgressBar.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
        CleanupCommandBar.IsEnabled = !isBusy;
        ExclusionPatternTextBox.IsEnabled = !isBusy;
        AddExclusionButton.IsEnabled = !isBusy;
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
            if (string.Equals(module.Name, "User Profile Cleanup", StringComparison.Ordinal))
            {
                return module;
            }
        }

        throw new FileNotFoundException("User Profile Cleanup module was not found in the configured tool list.");
    }
}

