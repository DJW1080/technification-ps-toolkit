using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TechnificationToolboxApp.Models;

namespace TechnificationToolboxApp.Pages;

public sealed partial class ToolsPage : Page
{
    public ToolsPage()
    {
        InitializeComponent();
        ToolsListView.ItemsSource = App.Modules;
    }

    private async void LaunchToolboxButton_Click(object sender, RoutedEventArgs e)
    {
        await RunActionAsync(() => App.Launcher.LaunchToolbox());
    }

    private async void OpenLogsButton_Click(object sender, RoutedEventArgs e)
    {
        await RunActionAsync(App.Launcher.OpenLogsFolder);
    }

    private async void OpenReportsButton_Click(object sender, RoutedEventArgs e)
    {
        await RunActionAsync(App.Launcher.OpenReportsFolder);
    }

    private async void OpenNativeButton_Click(object sender, RoutedEventArgs e)
    {
        Button? button = sender as Button;
        ToolModule? module = button?.Tag as ToolModule;
        if (module == null)
        {
            return;
        }

        Type? pageType = ResolveNativePageType(module);
        if (pageType == null)
        {
            await ShowMessageAsync("Native page unavailable", "This tool still runs through the legacy PowerShell workflow.");
            return;
        }

        Frame.Navigate(pageType);
    }

    private async void LaunchToolButton_Click(object sender, RoutedEventArgs e)
    {
        Button? button = sender as Button;
        ToolModule? module = button?.Tag as ToolModule;
        if (module != null)
        {
            await RunActionAsync(() => App.Launcher.LaunchTool(module, false));
        }
    }

    private async void LaunchToolAsAdminButton_Click(object sender, RoutedEventArgs e)
    {
        Button? button = sender as Button;
        ToolModule? module = button?.Tag as ToolModule;
        if (module != null)
        {
            await RunActionAsync(() => App.Launcher.LaunchTool(module, true));
        }
    }

    private async Task RunActionAsync(Action action)
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

    private static Type? ResolveNativePageType(ToolModule module)
    {
        return module.NativePageTag switch
        {
            "auto-repair" => typeof(AutoRepairPage),
            "profile-cleanup" => typeof(UserProfileCleanupPage),
            "event-log" => typeof(EventLogCleanerPage),
            "enhancements" => typeof(WindowsEnhancementsPage),
            "network" => typeof(NetworkPage),
            "winget" => typeof(WingetPage),
            _ => null
        };
    }
}
