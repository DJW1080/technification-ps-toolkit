using System;
using System.ComponentModel;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TechnificationToolboxApp.Pages;
using WinRT.Interop;

namespace TechnificationToolboxApp;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Title = App.DisplayName;

        var appWindow = GetAppWindow();
        appWindow?.Resize(new Windows.Graphics.SizeInt32(1240, 820));

        if (RootNavigationView.MenuItems.Count > 0)
        {
            RootNavigationView.SelectedItem = RootNavigationView.MenuItems[0];
        }

        UpdateElevationButtonState();
        NavigateToTag("overview");
    }

    private void RootNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer?.Tag is string tag)
        {
            NavigateToTag(tag);
        }
    }

    private async void RestartAsAdminButton_Click(object sender, RoutedEventArgs e)
    {
        if (App.Launcher.IsApplicationRunningAsAdministrator())
        {
            await ShowMessageAsync("Already elevated", "The app is already running as Administrator.");
            UpdateElevationButtonState();
            return;
        }

        try
        {
            App.Launcher.RelaunchApplicationAsAdministrator();
            Application.Current.Exit();
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            await ShowMessageAsync("Elevation canceled", "User Account Control was canceled. The app is still running in the current session.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("Elevation failed", ex.Message);
        }
    }

    private void NavigateToTag(string tag)
    {
        var pageType = tag switch
        {
            "overview" => typeof(HomePage),
            "tools" => typeof(ToolsPage),
            "auto-repair" => typeof(AutoRepairPage),
            "profile-cleanup" => typeof(UserProfileCleanupPage),
            "event-log" => typeof(EventLogCleanerPage),
            "enhancements" => typeof(WindowsEnhancementsPage),
            "network" => typeof(NetworkPage),
            "winget" => typeof(WingetPage),
            "about" => typeof(AboutPage),
            _ => typeof(HomePage)
        };

        if (ContentFrame.CurrentSourcePageType != pageType)
        {
            ContentFrame.Navigate(pageType);
        }
    }

    private void UpdateElevationButtonState()
    {
        RestartAsAdminButton.Visibility = App.Launcher.IsApplicationRunningAsAdministrator()
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private async System.Threading.Tasks.Task ShowMessageAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "Close",
            XamlRoot = ContentFrame.XamlRoot
        };

        await dialog.ShowAsync();
    }

    private AppWindow? GetAppWindow()
    {
        var hWnd = WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
        return AppWindow.GetFromWindowId(windowId);
    }
}
