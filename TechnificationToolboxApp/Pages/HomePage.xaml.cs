using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace TechnificationToolboxApp.Pages;

public sealed partial class HomePage : Page
{
    public HomePage()
    {
        InitializeComponent();
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

    private async Task RunActionAsync(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            var dialog = new ContentDialog
            {
                Title = "Action failed",
                Content = ex.Message,
                CloseButtonText = "Close",
                XamlRoot = XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}
