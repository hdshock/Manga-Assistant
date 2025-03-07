using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using MangaAssistant.Core.Models;
using MangaAssistant.Core.ViewModels;
using MangaAssistant.Core.Commands;
using MangaAssistant.Core.Services;
using MangaAssistant.Infrastructure.Services;
using MangaAssistant.WPF.Controls.ViewModels;
using MangaAssistant.WPF.Controls;

namespace MangaAssistant.WPF.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly ISettingsService _settingsService;
        private readonly ILibraryService _libraryService;
        private readonly IMetadataService _metadataService;
        private ObservableCollection<Series> _series;

        public ISettingsService SettingsService => _settingsService;
        public ILibraryService LibraryService => _libraryService;
        public IMetadataService MetadataService => _metadataService;

        public ObservableCollection<Series> Series
        {
            get => _series;
            set
            {
                _series = value;
                OnPropertyChanged(nameof(Series));
            }
        }

        public ICommand RefreshLibraryCommand { get; }

        public MainViewModel(ILibraryService libraryService, ISettingsService settingsService, IMetadataService metadataService)
        {
            _libraryService = libraryService ?? throw new ArgumentNullException(nameof(libraryService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _metadataService = metadataService ?? throw new ArgumentNullException(nameof(metadataService));
            _series = new ObservableCollection<Series>();

            RefreshLibraryCommand = new RelayCommand(async _ => await RefreshLibraryAsync());

            // Subscribe to library updates
            _libraryService.LibraryUpdated += OnLibraryUpdated;
        }

        private void OnLibraryUpdated(object? sender, LibraryUpdatedEventArgs e)
        {
            Series.Clear();
            foreach (var series in e.Series)
            {
                Series.Add(series);
            }
        }

        public async Task InitializeAsync()
        {
            await RefreshLibraryAsync();
        }

        public async Task RefreshLibraryAsync()
        {
            try
            {
                await _libraryService.ScanLibraryAsync();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error scanning library: {ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
} 