using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TechnificationToolboxApp.Models;

namespace TechnificationToolboxApp.Pages;

public sealed partial class EventLogCleanerPage : Page
{
    private readonly List<string> _latestReportPaths = new List<string>();
    private bool _suppressToggleSync;

    public EventLogCleanerPage()
    {
        InitializeComponent();
        LoadStatus();
        LastActionTextBlock.Text = "No action run yet.";
        LastResultTextBlock.Text = "Idle";
        ReportPathsTextBox.Text = "No reports generated yet.";
        OpenLatestReportButton.IsEnabled = false;
    }

    private async void StartCleanerButton_Click(object sender, RoutedEventArgs e)
    {
        EventLogCleanerStatus status = App.EventLogCleaner.GetStatus();
        if (!status.DryRunEnabled)
        {
            if (!await ConfirmAsync("Run live event log cleaner?", "This will clear enabled event logs except the configured exclusions."))
            {
                return;
            }
        }

        await RunOperationAsync("Start event log cleaner", progress => App.EventLogCleaner.StartCleanerAsync(progress));
    }

    private async void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync("Show current exclusions and settings", progress => App.EventLogCleaner.ShowCurrentSettingsAsync(progress));
    }

    private async void AddExactButton_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync("Add exact exclusion", progress => App.EventLogCleaner.AddExactExclusionAsync(ExactExclusionTextBox.Text, progress));
    }

    private async void AddWildcardButton_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync("Add wildcard exclusion", progress => App.EventLogCleaner.AddWildcardExclusionAsync(WildcardExclusionTextBox.Text, progress));
    }

    private async void SaveConfigButton_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync("Save exclusions to config", progress => App.EventLogCleaner.SaveConfigAsync(progress));
    }

    private async void RemoveExactButton_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync("Remove exact exclusion", progress => App.EventLogCleaner.RemoveExactExclusionAsync(ExactExclusionTextBox.Text, progress));
    }

    private async void RemoveWildcardButton_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync("Remove wildcard exclusion", progress => App.EventLogCleaner.RemoveWildcardExclusionAsync(WildcardExclusionTextBox.Text, progress));
    }

    private async void LoadConfigButton_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync("Load exclusions from config", progress => App.EventLogCleaner.LoadConfigAsync(progress));
    }

    private async void ResetDefaultsButton_Click(object sender, RoutedEventArgs e)
    {
        if (!await ConfirmAsync("Reset exclusions to defaults?", "This replaces the current in-memory exclusions with the default set."))
        {
            return;
        }

        await RunOperationAsync("Reset exclusions to defaults", progress => App.EventLogCleaner.ResetExclusionsToDefaultsAsync(progress));
    }

    private void DryRunToggleSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (_suppressToggleSync)
        {
            return;
        }

        App.EventLogCleaner.SetDryRunEnabled(DryRunToggleSwitch.IsOn);
        LoadStatus();
    }

    private void IncludeCountsToggleSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (_suppressToggleSync)
        {
            return;
        }

        App.EventLogCleaner.SetSkipCounts(!IncludeCountsToggleSwitch.IsOn);
        LoadStatus();
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
            await ShowMessageAsync("Event log cleaner action failed", ex.Message);
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
        EventLogCleanerStatus status = App.EventLogCleaner.GetStatus();
        AdminStatusTextBlock.Text = status.IsAdministrator ? "Administrator" : "Standard session";
        DryRunStatusTextBlock.Text = status.DryRunEnabled ? "WhatIf / dry run" : "Live mode";
        CountsStatusTextBlock.Text = status.SkipCounts ? "Fast / skip counts" : "Detailed / include counts";
        ExactCountTextBlock.Text = status.ExactExclusionCount.ToString();
        WildcardCountTextBlock.Text = status.WildcardExclusionCount.ToString();
        ConfigPathTextBlock.Text = status.ConfigPath;

        _suppressToggleSync = true;
        DryRunToggleSwitch.IsOn = status.DryRunEnabled;
        IncludeCountsToggleSwitch.IsOn = !status.SkipCounts;
        _suppressToggleSync = false;

        CleanerCommandBar.IsEnabled = BusyProgressBar.Visibility != Visibility.Visible;
    }

    private void SetBusy(bool isBusy)
    {
        BusyProgressBar.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
        CleanerCommandBar.IsEnabled = !isBusy;
        ExactExclusionTextBox.IsEnabled = !isBusy;
        WildcardExclusionTextBox.IsEnabled = !isBusy;
        DryRunToggleSwitch.IsEnabled = !isBusy;
        IncludeCountsToggleSwitch.IsEnabled = !isBusy;
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
            if (string.Equals(module.Name, "Event Log Cleaner", StringComparison.Ordinal))
            {
                return module;
            }
        }

        throw new FileNotFoundException("Event Log Cleaner module was not found in the configured tool list.");
    }
}

