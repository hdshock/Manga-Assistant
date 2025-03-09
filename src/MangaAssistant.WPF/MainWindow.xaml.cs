using System;
using System.Diagnostics;
using System.IO;
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
    private readonly SeriesPage _seriesPage;
    private readonly SettingsPage _settingsPage;

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
        _seriesPage = new SeriesPage(ViewModelLocator.MetadataService, ViewModelLocator.MainViewModel.LibraryService);
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
        // Just switch visibility without affecting the data
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
        }
    }

    private void MangaCard_MetadataUpdateRequested(object sender, RoutedEventArgs e)
    {
        if (sender is MangaCard card && 
            card.DataContext is Series series)
        {
            // First load the series in the series page
            _seriesPage.LoadSeries(series);
            
            // Then programmatically trigger the search metadata functionality
            _seriesPage.SearchMetadata();
            
            // Make the series page visible
            LibraryView.Visibility = Visibility.Collapsed;
            SettingsContainer.Visibility = Visibility.Collapsed;
            SeriesDetailContainer.Visibility = Visibility.Visible;
        }
    }

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        // Only process when Enter key is pressed
        if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(SearchBox.Text))
        {
            try
            {
                // Get the search query
                string searchQuery = SearchBox.Text.Trim();
                
                // Log the search attempt
                Console.WriteLine($"Searching for: {searchQuery}");
                
                // Filter the series collection based on the search query
                // This is done on the UI thread to ensure thread safety
                Application.Current.Dispatcher.Invoke(() => {
                    if (DataContext is MainViewModel viewModel)
                    {
                        // Create a temporary collection for the filtered results
                        var filteredSeries = viewModel.Series
                            .Where(s => s.Title.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                        
                        // Clear and repopulate the observable collection on the UI thread
                        viewModel.Series.Clear();
                        foreach (var series in filteredSeries)
                        {
                            viewModel.Series.Add(series);
                        }
                        
                        // Show a message if no results were found
                        if (filteredSeries.Count == 0)
                        {
                            MessageBox.Show($"No series found matching '{searchQuery}'", 
                                "No Results", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                // Handle any exceptions that occur during the search
                Application.Current.Dispatcher.Invoke(() => {
                    MessageBox.Show($"Error during search: {ex.Message}", 
                        "Search Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
                Console.WriteLine($"Search error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}