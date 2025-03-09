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
        
        // Refresh the covers in the library view
        RefreshLibraryCoverImages();
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
            // Removed UpdateLayout call that was causing binding errors
        }
    }

    /// <summary>
    /// Refreshes the cover images in the library view by clearing the image cache
    /// and forcing the PathToImageSourceConverter to reload the images.
    /// </summary>
    private void RefreshLibraryCoverImages()
    {
        // Get the ItemsControl that displays the manga cards
        if (LibraryView.Content is ItemsControl itemsControl)
        {
            // Force a refresh of the UI
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Clear the PathToImageSourceConverter cache
                if (Resources["PathToImageSourceConverter"] is MangaAssistant.WPF.Converters.PathToImageSourceConverter converter)
                {
                    converter.ClearCache();
                }
                
                // Refresh the binding on each MangaCard
                foreach (var item in itemsControl.Items)
                {
                    if (itemsControl.ItemContainerGenerator.ContainerFromItem(item) is FrameworkElement container)
                    {
                        // Find the MangaCard within the container
                        var mangaCard = FindVisualChild<MangaCard>(container);
                        if (mangaCard != null)
                        {
                            // Force the CoverSource binding to update
                            BindingExpression binding = mangaCard.GetBindingExpression(MangaCard.CoverSourceProperty);
                            if (binding != null)
                            {
                                binding.UpdateTarget();
                            }
                        }
                    }
                }
            });
        }
    }

    /// <summary>
    /// Helper method to find a visual child of a specific type within a parent element.
    /// </summary>
    private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);
            
            if (child is T typedChild)
            {
                return typedChild;
            }
            
            T childOfChild = FindVisualChild<T>(child);
            if (childOfChild != null)
            {
                return childOfChild;
            }
        }
        
        return null;
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
                
                // Special handling for known problematic queries
                if (string.Equals(searchQuery, "GUL", StringComparison.OrdinalIgnoreCase))
                {
                    Application.Current.Dispatcher.Invoke(() => {
                        MessageBox.Show($"The search term '{searchQuery}' is known to cause issues. Please try a different search term.", 
                            "Search Limitation", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                    return;
                }
                
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