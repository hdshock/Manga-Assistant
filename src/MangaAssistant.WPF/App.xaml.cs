using System;
using System.Configuration;
using System.Data;
using System.Windows;
using MangaAssistant.WPF.ViewModels;
using System.Threading.Tasks;

namespace MangaAssistant.WPF;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Initialize ViewModelLocator
        ViewModelLocator.Initialize();

        // Start initial library scan in the background
        Task.Run(async () =>
        {
            try
            {
                await ViewModelLocator.MainViewModel.InitializeAsync();
            }
            catch (Exception ex)
            {
                // Log error but don't show message box since we're in a background thread
                System.Diagnostics.Debug.WriteLine($"Error during initial library scan: {ex.Message}");
            }
        });
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);

        // Save settings before exit
        var settingsService = ViewModelLocator.MainViewModel.SettingsService;
        settingsService.SaveSettings();
    }
}

