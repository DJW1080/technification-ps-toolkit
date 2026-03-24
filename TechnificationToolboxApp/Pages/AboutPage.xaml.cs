using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace TechnificationToolboxApp.Pages;

public sealed partial class AboutPage : Page
{
    public AboutPage()
    {
        InitializeComponent();
        WorkspaceRootTextBlock.Text = Directory.GetParent(AppContext.BaseDirectory)?.Parent?.Parent?.Parent?.FullName ?? AppContext.BaseDirectory;
        ScriptsRootTextBlock.Text = App.ScriptsRoot;
        LogsPathTextBlock.Text = App.Launcher.LogsPath;
        ReportsPathTextBlock.Text = App.Launcher.ReportsPath;
    }

    private async void OpenScriptsButton_Click(object sender, RoutedEventArgs e)
    {
        await RunActionAsync(App.Launcher.OpenScriptsFolder);
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
