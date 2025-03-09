using System;
using System.Configuration;
using System.Data;
using System.Windows;
using MangaAssistant.WPF.ViewModels;
using System.Threading.Tasks;
using System.IO;
using MangaAssistant.Core.Services;
using MangaAssistant.Infrastructure.Services;
using MangaAssistant.Common.Logging;

namespace MangaAssistant.WPF;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Initialize logger
        string appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MangaAssistant");
            
        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
        }
        
        Logger.Initialize(appDataPath);
        Logger.Log("Application starting", LogLevel.Info);
        
        // Initialize services
        var settingsService = new SettingsService();
        var libraryScanner = new LibraryScanner(settingsService);
        var libraryService = new LibraryService(settingsService, libraryScanner);
        var metadataService = new MetadataService(libraryService);

        // Register services with the ViewModelLocator
        ViewModelLocator.RegisterServices(libraryService, settingsService, metadataService);

        // Initialize the main view model
        try
        {
            // Ensure the application directory exists
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
                Logger.Log($"Created application directory: {appDataPath}", LogLevel.Info);
            }

            // Initialize the main view model
            _ = Task.Run(async () =>
            {
                try
                {
                    await ViewModelLocator.MainViewModel.InitializeAsync();
                    Logger.Log("Main view model initialized successfully", LogLevel.Info);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error during initial library scan: {ex.Message}", LogLevel.Error);
                }
            });
        }
        catch (Exception ex)
        {
            Logger.Log($"Error during application startup: {ex.Message}", LogLevel.Error);
            MessageBox.Show($"Error during application startup: {ex.Message}", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);

        // Save settings before exit
        var settingsService = ViewModelLocator.MainViewModel.SettingsService;
        settingsService.SaveSettings();
    }
}

