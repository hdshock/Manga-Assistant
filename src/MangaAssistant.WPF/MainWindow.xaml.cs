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
    private TestWindow? _testWindow;

    public MainWindow()
    {
        InitializeComponent();
        
        // Generate placeholder image
        PlaceholderGenerator.CreatePlaceholder();
        
        // Set the data context
        DataContext = ViewModelLocator.MainViewModel;

        // Create the library view
        var libraryView = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        
        var itemsControl = new ItemsControl();
        itemsControl.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("Series"));
        
        var itemsPanelTemplate = new ItemsPanelTemplate();
        var wrapPanelFactory = new FrameworkElementFactory(typeof(WrapPanel));
        wrapPanelFactory.SetValue(WrapPanel.MarginProperty, new Thickness(15));
        itemsPanelTemplate.VisualTree = wrapPanelFactory;
        itemsControl.ItemsPanel = itemsPanelTemplate;
        
        var dataTemplate = new DataTemplate();
        var mangaCardFactory = new FrameworkElementFactory(typeof(MangaCard));
        mangaCardFactory.SetValue(MangaCard.WidthProperty, 133.0);
        mangaCardFactory.SetValue(MangaCard.HeightProperty, 186.0);
        mangaCardFactory.SetValue(MangaCard.MarginProperty, new Thickness(8));
        mangaCardFactory.SetBinding(MangaCard.TitleProperty, new Binding("Title"));
        mangaCardFactory.SetBinding(MangaCard.ProgressProperty, new Binding("Progress"));
        mangaCardFactory.SetBinding(MangaCard.CoverSourceProperty, new Binding("CoverPath") { Converter = Resources["PathToImageSourceConverter"] as IValueConverter });
        mangaCardFactory.SetBinding(MangaCard.UnreadChaptersProperty, new Binding("ChapterCount"));
        mangaCardFactory.SetBinding(MangaCard.SeriesFolderPathProperty, new Binding("FolderPath") { Mode = BindingMode.OneWay });
        
        // Add event handlers
        mangaCardFactory.AddHandler(
            MangaCard.SeriesClickedEvent, 
            new RoutedEventHandler(MangaCard_SeriesClicked));
        mangaCardFactory.AddHandler(
            MangaCard.MetadataUpdateRequestedEvent, 
            new RoutedEventHandler(MangaCard_MetadataUpdateRequested));
        
        dataTemplate.VisualTree = mangaCardFactory;
        itemsControl.ItemTemplate = dataTemplate;
        
        libraryView.Content = itemsControl;
        LibraryView = libraryView;

        // Initialize settings page
        _settingsPage = new SettingsPage(
            ViewModelLocator.MainViewModel.SettingsService,
            ViewModelLocator.MainViewModel.LibraryService);
        
        // Create settings container
        SettingsContainer = new Grid();
        SettingsContainer.Children.Add(_settingsPage);

        // Initialize series page
        _seriesPage = new SeriesPage(ViewModelLocator.MainViewModel.MetadataService, ViewModelLocator.MainViewModel.LibraryService);
        _seriesPage.HorizontalAlignment = HorizontalAlignment.Stretch;
        _seriesPage.VerticalAlignment = VerticalAlignment.Stretch;
        
        // Create series detail container
        SeriesDetailContainer = new Grid();
        SeriesDetailContainer.Children.Add(_seriesPage);

        // Set initial view
        MainContent.Content = LibraryView;

        // Initial scan
        _ = ViewModelLocator.MainViewModel.InitializeAsync();

        // Handle window closing
        Closing += MainWindow_Closing;

        // Add key binding for test window (Ctrl+T)
        KeyDown += MainWindow_KeyDown;
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Save settings before closing
        ViewModelLocator.MainViewModel.SettingsService.SaveSettings();
    }

    private void LibraryView_Click(object sender, RoutedEventArgs e)
    {
        // Update active menu item
        SetActiveMenuItem(LibraryViewButton);
        
        // Show library view in the main content
        MainContent.Content = LibraryView;
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        // Update active menu item
        SetActiveMenuItem(SettingsButton);
        
        // Show settings view in the main content
        MainContent.Content = SettingsContainer;
    }
    
    private void ScanLibrary_Click(object sender, RoutedEventArgs e)
    {
        // Start scanning the library
        _ = ViewModelLocator.MainViewModel.RefreshLibraryAsync();
        
        // Show library view
        LibraryView_Click(sender, e);
    }
    
    private void ViewLogs_Click(object sender, RoutedEventArgs e)
    {
        // Create and show the log window
        var logWindow = new MangaAssistant.WPF.Controls.LogWindow(this);
        logWindow.ShowDialog();
    }
    
    private void SetActiveMenuItem(Button activeButton)
    {
        // Reset all menu buttons
        LibraryViewButton.Tag = null;
        SettingsButton.Tag = null;
        
        // Set the active button
        activeButton.Tag = "Active";
    }

    private void MangaCard_SeriesClicked(object sender, RoutedEventArgs e)
    {
        if (sender is MangaCard card && 
            card.DataContext is Series series)
        {
            _seriesPage.LoadSeries(series);
            MainContent.Content = SeriesDetailContainer;
        }
    }

    private void MangaCard_MetadataUpdateRequested(object sender, RoutedEventArgs e)
    {
        if (sender is MangaCard card && 
            card.DataContext is Series series)
        {
            // First load the series in the series page
            _seriesPage.LoadSeries(series);
            
            // Then trigger metadata search
            _seriesPage.SearchMetadata();
            
            // Show the series detail view
            MainContent.Content = SeriesDetailContainer;
        }
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.T && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            if (_testWindow == null || !_testWindow.IsLoaded)
            {
                _testWindow = new TestWindow();
                _testWindow.Owner = this;
                _testWindow.Show();
            }
            else
            {
                _testWindow.Activate();
            }
        }
    }

    private ScrollViewer LibraryView { get; }
    private Grid SeriesDetailContainer { get; }
    private Grid SettingsContainer { get; }
}