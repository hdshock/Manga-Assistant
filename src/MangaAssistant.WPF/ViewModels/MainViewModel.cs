using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using MangaAssistant.Core;
using MangaAssistant.Core.Commands;
using MangaAssistant.Core.Models;
using MangaAssistant.Core.Services;
using MangaAssistant.Core.ViewModels;
using MangaAssistant.Infrastructure.Services;
using MangaAssistant.Infrastructure.Services.EventArgs;

namespace MangaAssistant.WPF.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly ISettingsService _settingsService;
        private readonly ILibraryService _libraryService;
        private readonly IMetadataService _metadataService;
        private readonly DispatcherTimer _progressUpdateTimer;
        private readonly Queue<Series> _pendingSeriesUpdates = new();
        private ObservableCollection<Series> _series;
        private bool _isScanning;
        private double _scanProgress;
        private string _currentScanningSeries = string.Empty;
        private int _totalSeries;
        private int _scannedSeries;
        private bool _isBusy;
        private bool _isInitialized;
        private CancellationTokenSource? _scanCancellationSource;

        public ISettingsService SettingsService => _settingsService;
        public ILibraryService LibraryService => _libraryService;
        public IMetadataService MetadataService => _metadataService;

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                _isBusy = value;
                OnPropertyChanged(nameof(IsBusy));
            }
        }

        public ObservableCollection<Series> Series
        {
            get => _series;
            set
            {
                _series = value;
                OnPropertyChanged(nameof(Series));
            }
        }

        public bool IsScanning
        {
            get => _isScanning;
            set
            {
                _isScanning = value;
                OnPropertyChanged(nameof(IsScanning));
            }
        }

        public double ScanProgress
        {
            get => _scanProgress;
            set
            {
                _scanProgress = value;
                OnPropertyChanged(nameof(ScanProgress));
            }
        }

        public string CurrentScanningSeries
        {
            get => _currentScanningSeries;
            set
            {
                _currentScanningSeries = value;
                OnPropertyChanged(nameof(CurrentScanningSeries));
            }
        }

        public int TotalSeries
        {
            get => _totalSeries;
            set
            {
                _totalSeries = value;
                OnPropertyChanged(nameof(TotalSeries));
            }
        }

        public int ScannedSeries
        {
            get => _scannedSeries;
            set
            {
                _scannedSeries = value;
                OnPropertyChanged(nameof(ScannedSeries));
            }
        }

        public ICommand RefreshLibraryCommand { get; }

        public MainViewModel(ILibraryService libraryService, ISettingsService settingsService, IMetadataService metadataService)
        {
            _libraryService = libraryService ?? throw new ArgumentNullException(nameof(libraryService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _metadataService = metadataService ?? throw new ArgumentNullException(nameof(metadataService));
            _series = new ObservableCollection<Series>();

            // Initialize progress update timer with lower frequency to reduce UI updates
            _progressUpdateTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(250) // Reduced update frequency
            };
            _progressUpdateTimer.Tick += OnProgressUpdateTick;

            RefreshLibraryCommand = new RelayCommand(async _ => await RefreshLibraryAsync(), _ => !IsBusy);

            // Subscribe to library updates
            _libraryService.LibraryUpdated += OnLibraryUpdated;
            _libraryService.SeriesAdded += OnSeriesAdded;

            // Subscribe to scan progress
            if (_libraryService is LibraryService service)
            {
                service.LibraryScanner.ScanProgress += OnScanProgressChanged;
                service.LibraryScanner.SeriesFound += OnSeriesFound;
            }

            // Load settings immediately but don't block
            Task.Run(() => _settingsService.LoadSettings());
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                Debug.WriteLine("Already initialized, skipping");
                return;
            }
            _isInitialized = true;
            Debug.WriteLine("Starting initialization");

            // Load settings first
            await Task.Run(() => _settingsService.LoadSettings());
            Debug.WriteLine($"Loaded settings, library path: {_settingsService.LibraryPath}");

            try
            {
                // Try to load from cache first
                if (_libraryService is LibraryService service)
                {
                    Debug.WriteLine("Loading from cache");
                    await service.LoadLibraryCacheAsync();
                    
                    // Always scan the library to ensure we have the latest data
                    await service.ScanLibraryAsync();
                    Debug.WriteLine($"Cache loaded, Series count: {Series.Count}");
            
                    // Start background scan without clearing UI
                    _scanCancellationSource = new CancellationTokenSource();
                    Debug.WriteLine("Starting background scan");
            
                    await Task.Factory.StartNew(async () =>
                    {
                        try
                        {
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                Debug.WriteLine("Setting initial UI state for scan");
                                IsBusy = true;
                                IsScanning = true;
                                ScanProgress = 0;
                                CurrentScanningSeries = string.Empty;
                                TotalSeries = 0;
                                ScannedSeries = 0;
                            }, DispatcherPriority.Background);

                            // Start the scan in the background without clearing series
                            Debug.WriteLine("Starting library scan");
                            await _libraryService.ScanLibraryAsync();
                            Debug.WriteLine("Library scan complete");

                            // Reset UI state after scan completes
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                Debug.WriteLine("Resetting UI state after scan");
                                IsBusy = false;
                                IsScanning = false;
                                ScanProgress = 100;
                                CurrentScanningSeries = string.Empty;
                                Debug.WriteLine($"Final series count: {Series.Count}");
                                
                                // Refresh cover images after scan completes
                                RefreshCoverImages();
                            }, DispatcherPriority.Background);
                        }
                        catch (Exception ex)
                        {
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                Debug.WriteLine($"Error during scan: {ex.Message}");
                                MessageBox.Show($"Error scanning library: {ex.Message}", "Error", 
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                                IsBusy = false;
                                IsScanning = false;
                            }, DispatcherPriority.Background);
                        }
                    }, _scanCancellationSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                }
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Debug.WriteLine($"Error during initialization: {ex.Message}");
                    MessageBox.Show($"Error initializing: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    IsBusy = false;
                    IsScanning = false;
                }, DispatcherPriority.Background);
            }
        }

        private void OnProgressUpdateTick(object? sender, EventArgs e)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, OnProgressUpdateTick, sender, e);
                return;
            }

            const int batchSize = 10; // Process more items per batch but less frequently
            var updatedSeries = new List<Series>();

            lock (_pendingSeriesUpdates)
            {
                for (int i = 0; i < batchSize && _pendingSeriesUpdates.Count > 0; i++)
                {
                    if (_pendingSeriesUpdates.TryDequeue(out var series))
                    {
                        updatedSeries.Add(series);
                    }
                }
            }

            if (updatedSeries.Any())
            {
                var uniqueSeries = updatedSeries
                    .OrderBy(s => s.Title)
                    .Where(series => !Series.Any(s => s.Id == series.Id))
                    .ToList();

                foreach (var series in uniqueSeries)
                {
                    Series.Add(series);
                }
            }

            // Stop the timer if no more updates and not scanning
            if (_pendingSeriesUpdates.Count == 0 && !IsScanning)
            {
                _progressUpdateTimer.Stop();
            }
        }

        private void OnSeriesAdded(object? sender, Series series)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, () => OnSeriesAdded(sender, series));
                return;
            }

            // Only add if not already present
            if (!Series.Any(s => s.Id == series.Id))
            {
                Series.Add(series);
            }
        }

        private void OnScanProgressChanged(object? sender, Infrastructure.Services.EventArgs.ScanProgressEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                ScanProgress = e.Progress;
                CurrentScanningSeries = e.CurrentSeriesTitle;
                ScannedSeries = e.ScannedDirectories;
                TotalSeries = e.TotalDirectories;
                Debug.WriteLine($"Scan progress: {e.Progress:F2}%, {e.ScannedDirectories}/{e.TotalDirectories}");
            }), DispatcherPriority.Background);
        }

        private void OnSeriesFound(object? sender, Series series)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                Debug.WriteLine($"Series found: {series.Title}");
                var existingSeries = Series.FirstOrDefault(s => 
                    s.FolderPath.Equals(series.FolderPath, StringComparison.OrdinalIgnoreCase));

                if (existingSeries != null)
                {
                    Debug.WriteLine($"Updating existing series: {series.Title}");
                    // Update properties while maintaining the same object reference
                    existingSeries.Title = series.Title;
                    existingSeries.LastModified = series.LastModified;
                    existingSeries.ChapterCount = series.ChapterCount;
                    existingSeries.Chapters = series.Chapters;
                    if (!string.IsNullOrEmpty(series.CoverPath))
                    {
                        existingSeries.CoverPath = series.CoverPath;
                    }
                    OnPropertyChanged(nameof(Series));
                }
                else
                {
                    Debug.WriteLine($"Adding new series: {series.Title}");
                    Series.Add(series);
                }
            }), DispatcherPriority.Background);
        }

        private void OnLibraryUpdated(object? sender, Core.LibraryUpdatedEventArgs e)
        {
            Debug.WriteLine($"Library updated event received, series count: {e.Series?.Count ?? 0}");
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (e.Series != null)
                {
                    var allSeries = e.Series.OrderBy(s => s.Title).ToList();
                    Debug.WriteLine($"Processing {allSeries.Count} series from update");

                    // First, update existing series
                    foreach (var series in allSeries)
                    {
                        var existingSeries = Series.FirstOrDefault(s => 
                            s.FolderPath.Equals(series.FolderPath, StringComparison.OrdinalIgnoreCase));

                        if (existingSeries != null)
                        {
                            Debug.WriteLine($"Updating existing series: {series.Title}");
                            // Update properties while maintaining the same object reference
                            existingSeries.Title = series.Title;
                            existingSeries.LastModified = series.LastModified;
                            existingSeries.ChapterCount = series.ChapterCount;
                            existingSeries.Chapters = series.Chapters;
                            if (!string.IsNullOrEmpty(series.CoverPath))
                            {
                                existingSeries.CoverPath = series.CoverPath;
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"Adding new series: {series.Title}");
                            Series.Add(series);
                        }
                    }

                    // Then remove series that no longer exist
                    var seriesToRemove = Series
                        .Where(existing => !allSeries.Any(s => 
                            s.FolderPath.Equals(existing.FolderPath, StringComparison.OrdinalIgnoreCase)))
                        .ToList();

                    foreach (var series in seriesToRemove)
                    {
                        Debug.WriteLine($"Removing series: {series.Title}");
                        Series.Remove(series);
                    }

                    OnPropertyChanged(nameof(Series));
                }

                IsScanning = false;
                IsBusy = false;
                ScanProgress = 0;
                CurrentScanningSeries = string.Empty;
                TotalSeries = 0;
                ScannedSeries = 0;
                Debug.WriteLine($"Library update complete, final series count: {Series.Count}");
            }), DispatcherPriority.Background);
        }

        public async Task RefreshLibraryAsync()
        {
            if (IsBusy) return;

            try
            {
                // Cancel any ongoing scan
                _scanCancellationSource?.Cancel();
                _scanCancellationSource = new CancellationTokenSource();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsBusy = true;
                    IsScanning = true;
                    ScanProgress = 0;
                    CurrentScanningSeries = string.Empty;
                    TotalSeries = 0;
                    ScannedSeries = 0;
                    Series.Clear();
                    _pendingSeriesUpdates.Clear();
                }, DispatcherPriority.Background);

                // Start new scan with low priority
                await Task.Factory.StartNew(async () =>
                {
                    await _libraryService.ScanLibraryAsync();
                    
                    // Refresh cover images after scan completes
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        RefreshCoverImages();
                    }, DispatcherPriority.Background);
                }, _scanCancellationSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Error scanning library: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }, DispatcherPriority.Background);
            }
            finally
            {
                if (!_progressUpdateTimer.IsEnabled)
                {
                    _progressUpdateTimer.Start();
                }
            }
        }

        /// <summary>
        /// Refreshes the cover images in the library view by clearing the image cache
        /// and forcing the PathToImageSourceConverter to reload the images.
        /// </summary>
        private void RefreshCoverImages()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Clear the PathToImageSourceConverter cache
                if (Application.Current.MainWindow.Resources["PathToImageSourceConverter"] is MangaAssistant.WPF.Converters.PathToImageSourceConverter converter)
                {
                    Debug.WriteLine("Clearing image cache to refresh covers");
                    converter.ClearCache();
                }
                
                // Force property changed notification to refresh bindings
                OnPropertyChanged(nameof(Series));
            });
        }
    }
} 