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

        // Initial library scan
        _ = Task.Run(async () =>
        {
            await ViewModelLocator.MainViewModel.InitializeAsync();
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

