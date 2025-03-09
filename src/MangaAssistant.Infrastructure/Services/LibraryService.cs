using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MangaAssistant.Core.Models;
using System.ComponentModel;
using MangaAssistant.Core.Services;
using System.Text.Json;
using System.IO;
using System.Linq;
using System.Diagnostics;
using MangaAssistant.Core;
using System.Windows;
using MangaAssistant.Common.Logging;

namespace MangaAssistant.Infrastructure.Services
{
    public class LibraryService : ILibraryService
    {
        private const string LIBRARY_CACHE_FILE = "library-cache.json";
        private readonly ISettingsService _settingsService;
        private readonly LibraryScanner _libraryScanner;
        private readonly JsonSerializerOptions _jsonOptions;
        private List<Series> _series = new();
        private string CachePath => Path.Combine(
            Path.GetDirectoryName(_settingsService.LibraryPath) ?? "",
            LIBRARY_CACHE_FILE);
        private bool _isScanning = false;
        private readonly object _seriesLock = new();

        public event EventHandler<Core.LibraryUpdatedEventArgs>? LibraryUpdated;
        public event EventHandler<Series>? SeriesAdded;
        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<Core.ScanProgressEventArgs>? ScanProgress;

        public LibraryScanner LibraryScanner => _libraryScanner;

        public List<Series> Series
        {
            get => _series;
            private set
            {
                _series = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Series)));
                foreach (var series in _series)
                {
                    SeriesAdded?.Invoke(this, series);
                }
                LibraryUpdated?.Invoke(this, new Core.LibraryUpdatedEventArgs(_series));
            }
        }

        public LibraryService(ISettingsService settingsService, LibraryScanner libraryScanner)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _libraryScanner = libraryScanner ?? throw new ArgumentNullException(nameof(libraryScanner));
            _libraryScanner.SeriesFound += OnSeriesFound;
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                PropertyNameCaseInsensitive = true
            };
        }

        private void OnSeriesFound(object? sender, Series series)
        {
            lock (_seriesLock)
            {
                Debug.WriteLine($"Series found event: {series.Title}");
                var existingSeries = _series.FirstOrDefault(s => 
                    s.FolderPath.Equals(series.FolderPath, StringComparison.OrdinalIgnoreCase));
                
                if (existingSeries != null)
                {
                    Debug.WriteLine($"Updating existing series: {series.Title}");
                    // Update existing series while preserving metadata and progress
                    existingSeries.Title = series.Title;
                    existingSeries.LastModified = series.LastModified;
                    existingSeries.ChapterCount = series.ChapterCount;
                    existingSeries.Chapters = series.Chapters;
                    if (!string.IsNullOrEmpty(series.CoverPath))
                    {
                        existingSeries.CoverPath = series.CoverPath;
                    }
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Series)));
                }
                else
                {
                    Debug.WriteLine($"Adding new series: {series.Title}");
                    _series.Add(series);
                    SeriesAdded?.Invoke(this, series);
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Series)));
                }
            }
        }

        private JsonSerializerOptions CreateJsonOptions()
        {
            return new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task LoadLibraryCacheAsync()
        {
            await LoadLibraryCacheInternalAsync();
        }
        
        private async Task<bool> LoadLibraryCacheInternalAsync()
        {
            try
            {
                string cachePath = CachePath;
                if (!File.Exists(cachePath))
                {
                    Debug.WriteLine($"Cache file not found at {cachePath}");
                    return false;
                }

                string json = await File.ReadAllTextAsync(cachePath);
                var cachedSeries = JsonSerializer.Deserialize<List<Series>>(json, _jsonOptions);
                if (cachedSeries == null)
                {
                    Debug.WriteLine("Failed to deserialize cache");
                    return false;
                }

                // Update the series list with cached series
                lock (_seriesLock)
                {
                    _series.Clear();
                    foreach (var series in cachedSeries)
                    {
                        if (Directory.Exists(series.FolderPath))
                        {
                            _series.Add(series);
                        }
                    }
                    _series = _series.OrderBy(s => s.Title).ToList();
                }

                // Notify all listeners that the library is updated
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Series)));
                LibraryUpdated?.Invoke(this, new Core.LibraryUpdatedEventArgs(_series));

                Debug.WriteLine($"Loaded {_series.Count} series from cache");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading cache: {ex.Message}");
                return false;
            }
        }

        public async Task SaveLibraryCacheAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_settingsService.LibraryPath))
                    return;

                lock (_seriesLock)
                {
                    var options = CreateJsonOptions();
                    var json = JsonSerializer.Serialize(_series, options);
                    File.WriteAllText(CachePath, json);
                }

                // Save individual series metadata
                foreach (var series in _series)
                {
                    await SaveSeriesMetadataAsync(series);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving library cache: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        public async Task ScanLibraryAsync()
        {
            if (_isScanning)
            {
                Logger.Log("Scan already in progress", LogLevel.Warning);
                return;
            }

            _isScanning = true;
            Logger.Log("Starting library scan", LogLevel.Info);

            try
            {
                // Start a new scan
                var scanResult = await _libraryScanner.ScanLibraryAsync();
                Logger.Log($"Scanner found {scanResult.Series.Count} series", LogLevel.Info);
                
                if (!scanResult.Success)
                {
                    Logger.Log($"Scan failed: {scanResult.Message}", LogLevel.Error);
                    return;
                }
                
                bool hasChanges = false;
                
                // Update the series list with found series
                lock (_seriesLock)
                {
                    Logger.Log("Updating series list", LogLevel.Info);
                    
                    // Create a lookup of existing series by folder path
                    var existingSeries = _series
                        .Where(s => Directory.Exists(s.FolderPath))
                        .ToDictionary(s => s.FolderPath, StringComparer.OrdinalIgnoreCase);
                    Logger.Log($"Found {existingSeries.Count} existing valid series", LogLevel.Info);

                    // Update or add scanned series
                    foreach (var series in scanResult.Series)
                    {
                        if (existingSeries.TryGetValue(series.FolderPath, out var existing))
                        {
                            Logger.Log($"Updating existing series: {series.Title}", LogLevel.Info);
                            
                            // Check if there are actual changes
                            bool seriesChanged = false;
                            
                            // Update basic properties
                            if (existing.Title != series.Title)
                            {
                                existing.Title = series.Title;
                                seriesChanged = true;
                            }
                            
                            if (existing.LastModified != series.LastModified)
                            {
                                existing.LastModified = series.LastModified;
                                seriesChanged = true;
                            }
                            
                            if (existing.ChapterCount != series.ChapterCount)
                            {
                                existing.ChapterCount = series.ChapterCount;
                                seriesChanged = true;
                            }
                            
                            // Update chapters if they've changed
                            if (!existing.Chapters.SequenceEqual(series.Chapters, new ChapterEqualityComparer()))
                            {
                                Logger.Log($"Chapters changed for series: {series.Title}", LogLevel.Info);
                                existing.Chapters = series.Chapters;
                                seriesChanged = true;
                            }
                            
                            // Update cover if it's changed
                            if (!string.IsNullOrEmpty(series.CoverPath) && existing.CoverPath != series.CoverPath)
                            {
                                existing.CoverPath = series.CoverPath;
                                seriesChanged = true;
                            }
                            
                            if (seriesChanged)
                            {
                                hasChanges = true;
                                Logger.Log($"Series {series.Title} has been updated", LogLevel.Info);
                            }
                        }
                        else
                        {
                            Logger.Log($"Adding new series: {series.Title}", LogLevel.Info);
                            _series.Add(series);
                            SeriesAdded?.Invoke(this, series);
                            hasChanges = true;
                        }
                    }

                    // Remove series that no longer exist
                    var removedSeries = _series
                        .Where(s => !Directory.Exists(s.FolderPath))
                        .ToList();

                    foreach (var series in removedSeries)
                    {
                        Logger.Log($"Removing series that no longer exists: {series.Title}", LogLevel.Info);
                        _series.Remove(series);
                        hasChanges = true;
                    }

                    // Sort the list if there were changes
                    if (hasChanges)
                    {
                        _series = _series.OrderBy(s => s.Title).ToList();
                        Logger.Log($"Final series count: {_series.Count}", LogLevel.Info);
                    }
                }

                // Process cover insertion if the setting is enabled
                if (_settingsService.InsertCoverIntoFirstChapter)
                {
                    Logger.Log("Cover insertion setting is enabled. Processing covers...", LogLevel.Info);
                    await _libraryScanner.InsertCoversIntoFirstChaptersAsync(_series);
                }
                else
                {
                    Logger.Log("Cover insertion setting is disabled. Skipping cover processing.", LogLevel.Info);
                }

                // Only save and notify if there were changes
                if (hasChanges)
                {
                    // Save updated library to cache
                    await SaveLibraryCacheAsync();
                    Logger.Log("Saved library cache", LogLevel.Info);

                    // Notify that library is updated
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Series)));
                    LibraryUpdated?.Invoke(this, new Core.LibraryUpdatedEventArgs(_series));
                }
                else
                {
                    Logger.Log("No changes detected during scan", LogLevel.Info);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error scanning library: {ex.Message}", LogLevel.Error);
                Logger.Log($"Stack trace: {ex.StackTrace}", LogLevel.Error);
            }
            finally
            {
                _isScanning = false;
            }
        }

        public async Task ClearAndRescanLibraryAsync()
        {
            try
            {
                // First clear all caches
                await ClearCacheAsync();

                // Now do a fresh scan
                await ScanLibraryAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error clearing cache and rescanning: {ex.Message}");
                throw;
            }
        }

        public async Task ClearCacheAsync()
        {
            try
            {
                // Clear the in-memory series list
                _series.Clear();
                
                // Delete the cache file if it exists
                string cachePath = CachePath;
                if (File.Exists(cachePath))
                {
                    File.Delete(cachePath);
                    Debug.WriteLine($"Deleted cache file at {cachePath}");
                }
                
                // Clear any temporary files
                var libraryPath = _settingsService.LibraryPath;
                if (!string.IsNullOrEmpty(libraryPath) && Directory.Exists(libraryPath))
                {
                    var tempFiles = Directory.GetFiles(libraryPath, "temp_*", SearchOption.AllDirectories);
                    foreach (var tempFile in tempFiles)
                    {
                        try
                        {
                            File.Delete(tempFile);
                            Debug.WriteLine($"Deleted temporary file: {tempFile}");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error deleting temporary file {tempFile}: {ex.Message}");
                        }
                    }
                }
                
                // Notify that the library has been cleared
                LibraryUpdated?.Invoke(this, new Core.LibraryUpdatedEventArgs(new List<Series>()));
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error clearing cache: {ex.Message}");
                throw;
            }
        }

        public async Task SaveSeriesMetadataAsync(Series series)
        {
            if (series == null) throw new ArgumentNullException(nameof(series));
            
            try
            {
                var metadataPath = Path.Combine(series.FolderPath, "series-info.json");
                var options = CreateJsonOptions();
                var json = JsonSerializer.Serialize(series.Metadata, options);
                await File.WriteAllTextAsync(metadataPath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving series metadata: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        public async Task<SeriesMetadata?> LoadSeriesMetadataAsync(string directoryPath)
        {
            var metadataPath = Path.Combine(directoryPath, "series-info.json");
            if (!File.Exists(metadataPath))
                return null;

            try
            {
                var json = await File.ReadAllTextAsync(metadataPath);
                var options = CreateJsonOptions();
                return JsonSerializer.Deserialize<SeriesMetadata>(json, options);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading series metadata: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return null;
            }
        }

        public async Task UpdateSeriesCover(string seriesPath, string coverPath)
        {
            try
            {
                Logger.Log($"Updating cover for series at {seriesPath} with new cover {coverPath}", LogLevel.Info);
                
                // Generate the new cover path
                var targetPath = Path.Combine(seriesPath, "cover" + Path.GetExtension(coverPath));
                
                // Copy the new cover file
                File.Copy(coverPath, targetPath, true);
                Logger.Log($"Copied cover to {targetPath}", LogLevel.Info);
                
                // Update the series in memory
                lock (_seriesLock)
                {
                    var series = _series.FirstOrDefault(s => s.FolderPath.Equals(seriesPath, StringComparison.OrdinalIgnoreCase));
                    if (series != null)
                    {
                        series.CoverPath = targetPath;
                        series.LastModified = DateTime.Now;
                        
                        // Update metadata
                        if (series.Metadata == null)
                        {
                            series.Metadata = new SeriesMetadata();
                        }
                        series.Metadata.CoverPath = targetPath;
                        
                        // Trigger UI updates
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Series)));
                        LibraryUpdated?.Invoke(this, new Core.LibraryUpdatedEventArgs(_series));
                    }
                }
                
                // Save changes to disk
                await SaveLibraryCacheAsync();
                Logger.Log("Cover update completed successfully", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"Error updating series cover: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        private Series? ScanSeriesDirectory(string directory)
        {
            try
            {
                var directoryInfo = new DirectoryInfo(directory);
                if (!directoryInfo.Exists) return null;

                // Check if directory contains manga files
                var files = directoryInfo.GetFiles("*.*", SearchOption.AllDirectories)
                    .Where(f => IsValidMangaFile(f.Name))
                    .ToList();

                if (!files.Any()) return null;

                // Create series from directory
                var series = new Series
                {
                    Id = Guid.NewGuid(),
                    Title = directoryInfo.Name,
                    FolderPath = directory,
                    LastModified = directoryInfo.LastWriteTime,
                    ChapterCount = files.Count
                };

                return series;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void OnLibraryUpdated(List<Series> series)
        {
            LibraryUpdated?.Invoke(this, new Core.LibraryUpdatedEventArgs(series));
        }

        private void OnScanProgress(double progress, string seriesTitle, int scannedDirectories, int totalDirectories)
        {
            ScanProgress?.Invoke(this, new Core.ScanProgressEventArgs(
                progress,
                seriesTitle,
                scannedDirectories,
                totalDirectories
            ));
        }

        private bool IsValidMangaFile(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" or ".png" or ".webp" or ".gif" => true,
                _ => false
            };
        }

        public async Task ProcessAllCoversAsync()
        {
            Debug.WriteLine("Starting process to insert covers into first chapters for all series");
            
            if (!_settingsService.InsertCoverIntoFirstChapter)
            {
                Debug.WriteLine("Cover insertion setting is disabled. Skipping process.");
                return;
            }
            
            try
            {
                // Use the UI thread for updating UI components
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    try
                    {
                        // Get a snapshot of the current series list to avoid threading issues
                        List<Series> seriesList;
                        lock (_seriesLock)
                        {
                            seriesList = new List<Series>(_series);
                        }
                        
                        Debug.WriteLine($"Processing covers for {seriesList.Count} series");
                        await _libraryScanner.InsertCoversIntoFirstChaptersAsync(seriesList);
                        Debug.WriteLine("Cover processing completed successfully");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error during cover processing on UI thread: {ex.Message}");
                        Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initiating cover processing: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        // Add ChapterEqualityComparer class for comparing chapters
        private class ChapterEqualityComparer : IEqualityComparer<Chapter>
        {
            public bool Equals(Chapter? x, Chapter? y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (x is null || y is null) return false;
                
                return x.Number == y.Number &&
                       x.Volume == y.Volume &&
                       x.FilePath == y.FilePath &&
                       x.PageCount == y.PageCount;
            }

            public int GetHashCode(Chapter obj)
            {
                return HashCode.Combine(obj.Number, obj.Volume, obj.FilePath, obj.PageCount);
            }
        }
    }
} 