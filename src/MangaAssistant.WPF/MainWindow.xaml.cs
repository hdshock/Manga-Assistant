using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MangaAssistant.WPF.Controls;
using MangaAssistant.Infrastructure.Services;
using MangaAssistant.WPF.ViewModels;
using MangaAssistant.WPF.Controls.ViewModels;
using MangaAssistant.Core.Models;
using System.Linq;
using System.Collections.ObjectModel;

namespace MangaAssistant.WPF;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly SettingsPage _settingsPage;
    private readonly SeriesPage _seriesPage;

    public MainWindow()
    {
        InitializeComponent();
        
        // Generate placeholder image
        PlaceholderGenerator.CreatePlaceholder();
        
        // Initialize view model locator
        ViewModelLocator.Initialize();
        DataContext = ViewModelLocator.MainViewModel;

        // Initialize settings page
        _settingsPage = new SettingsPage(
            ViewModelLocator.MainViewModel.SettingsService,
            ViewModelLocator.MainViewModel.LibraryService);
        SettingsContainer.Children.Add(_settingsPage);

        // Initialize series page
        _seriesPage = new SeriesPage(ViewModelLocator.MetadataService);
        SeriesDetailContainer.Children.Add(_seriesPage);
        _seriesPage.HorizontalAlignment = HorizontalAlignment.Stretch;
        _seriesPage.VerticalAlignment = VerticalAlignment.Stretch;

        // Initial scan
        _ = ViewModelLocator.MainViewModel.InitializeAsync();

        // Handle window closing
        Closing += MainWindow_Closing;
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Save settings before closing
        ViewModelLocator.MainViewModel.SettingsService.SaveSettings();
    }

    private void LibraryView_Click(object sender, RoutedEventArgs e)
    {
        LibraryView.Visibility = Visibility.Visible;
        SeriesDetailContainer.Visibility = Visibility.Collapsed;
        SettingsContainer.Visibility = Visibility.Collapsed;
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        LibraryView.Visibility = Visibility.Collapsed;
        SeriesDetailContainer.Visibility = Visibility.Collapsed;
        SettingsContainer.Visibility = Visibility.Visible;
    }

    private void MangaCard_SeriesClicked(object sender, RoutedEventArgs e)
    {
        if (sender is MangaCard card && 
            card.DataContext is Series series)
        {
            _seriesPage.LoadSeries(series);
            LibraryView.Visibility = Visibility.Collapsed;
            SettingsContainer.Visibility = Visibility.Collapsed;
            SeriesDetailContainer.Visibility = Visibility.Visible;
            SeriesDetailContainer.UpdateLayout();
        }
    }
}