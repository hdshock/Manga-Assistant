using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MangaAssistant.Core.Models;
using System.ComponentModel;
using MangaAssistant.Core.Services;
using System.Text.Json;
using System.IO;
using System.Linq;
using MangaAssistant.Infrastructure.Services.EventArgs;
using System.Diagnostics;

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

        public event EventHandler<LibraryUpdatedEventArgs>? LibraryUpdated;
        public event EventHandler<Series>? SeriesAdded;
        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<ScanProgressEventArgs>? ScanProgress;

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
                LibraryUpdated?.Invoke(this, new LibraryUpdatedEventArgs(_series));
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

        public async Task<bool> LoadLibraryCacheAsync()
        {
            try
            {
                Debug.WriteLine($"Attempting to load cache from {CachePath}");
                if (string.IsNullOrWhiteSpace(_settingsService.LibraryPath) || !File.Exists(CachePath))
                {
                    Debug.WriteLine("Cache file doesn't exist or library path not set");
                    return false;
                }

                var json = await File.ReadAllTextAsync(CachePath);
                var options = CreateJsonOptions();
                var cachedSeries = JsonSerializer.Deserialize<List<Series>>(json, options);
                
                if (cachedSeries != null)
                {
                    Debug.WriteLine($"Loaded {cachedSeries.Count} series from cache");
                    // Verify each series still exists and load its metadata
                    var validSeries = new List<Series>();
                    foreach (var series in cachedSeries)
                    {
                        if (Directory.Exists(series.FolderPath))
                        {
                            Debug.WriteLine($"Verifying series: {series.Title} at {series.FolderPath}");
                            // Load metadata if it exists
                            var metadata = await LoadSeriesMetadataAsync(series.FolderPath);
                            if (metadata != null)
                            {
                                Debug.WriteLine($"Loaded metadata for {series.Title}");
                                series.Metadata = metadata;
                            }
                            validSeries.Add(series);
                        }
                        else
                        {
                            Debug.WriteLine($"Series directory no longer exists: {series.FolderPath}");
                        }
                    }

                    if (validSeries.Any())
                    {
                        Debug.WriteLine($"Found {validSeries.Count} valid series");
                        lock (_seriesLock)
                        {
                            _series = validSeries;
                            foreach (var series in _series)
                            {
                                SeriesAdded?.Invoke(this, series);
                            }
                        }
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Series)));
                        LibraryUpdated?.Invoke(this, new LibraryUpdatedEventArgs(_series));
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading library cache: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            return false;
        }

        private async Task SaveLibraryCacheAsync()
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
                Debug.WriteLine("Scan already in progress");
                return;
            }

            _isScanning = true;
            Debug.WriteLine("Starting library scan");

            try
            {
                // Start a new scan
                var scannedSeries = await _libraryScanner.ScanLibraryAsync();
                Debug.WriteLine($"Scanner found {scannedSeries.Count} series");
                
                // Update the series list with found series
                lock (_seriesLock)
                {
                    Debug.WriteLine("Updating series list");
                    // Keep existing series that are still valid
                    var existingSeries = _series
                        .Where(s => Directory.Exists(s.FolderPath))
                        .ToDictionary(s => s.FolderPath, StringComparer.OrdinalIgnoreCase);
                    Debug.WriteLine($"Found {existingSeries.Count} existing valid series");

                    // Update or add scanned series
                    foreach (var series in scannedSeries)
                    {
                        if (existingSeries.TryGetValue(series.FolderPath, out var existing))
                        {
                            Debug.WriteLine($"Updating existing series: {series.Title}");
                            // Update existing series while preserving metadata and progress
                            existing.Title = series.Title;
                            existing.LastModified = series.LastModified;
                            existing.ChapterCount = series.ChapterCount;
                            existing.Chapters = series.Chapters;
                            if (!string.IsNullOrEmpty(series.CoverPath))
                            {
                                existing.CoverPath = series.CoverPath;
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"Adding new series: {series.Title}");
                            // Add new series
                            _series.Add(series);
                            SeriesAdded?.Invoke(this, series);
                        }
                    }

                    // Only remove series that no longer exist
                    var removedSeries = _series
                        .Where(s => !Directory.Exists(s.FolderPath))
                        .ToList();

                    foreach (var series in removedSeries)
                    {
                        Debug.WriteLine($"Removing series that no longer exists: {series.Title}");
                        _series.Remove(series);
                    }

                    // Sort the list
                    _series = _series.OrderBy(s => s.Title).ToList();
                    Debug.WriteLine($"Final series count: {_series.Count}");
                }

                // Save updated library to cache
                await SaveLibraryCacheAsync();
                Debug.WriteLine("Saved library cache");

                // Notify that library is updated
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Series)));
                LibraryUpdated?.Invoke(this, new LibraryUpdatedEventArgs(_series));
                Debug.WriteLine("Sent library updated notifications");
            }
            finally
            {
                _isScanning = false;
                Debug.WriteLine("Scan complete");
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
                // Clear memory state
                lock (_seriesLock)
                {
                    _series.Clear();
                }
                
                // Delete cache file if it exists
                if (File.Exists(CachePath))
                {
                    File.Delete(CachePath);
                }

                // Clear any metadata cache files in series folders
                if (!string.IsNullOrWhiteSpace(_settingsService.LibraryPath) && Directory.Exists(_settingsService.LibraryPath))
                {
                    foreach (var dir in Directory.GetDirectories(_settingsService.LibraryPath))
                    {
                        var metadataPath = Path.Combine(dir, "series-info.json");
                        if (File.Exists(metadataPath))
                        {
                            File.Delete(metadataPath);
                        }
                    }
                }

                // Notify all listeners that the library is cleared
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Series)));
                LibraryUpdated?.Invoke(this, new LibraryUpdatedEventArgs(new List<Series>()));

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
            var targetPath = Path.Combine(seriesPath, "cover" + Path.GetExtension(coverPath));
            File.Copy(coverPath, targetPath, true);
            
            // Update cache after cover changes
            await SaveLibraryCacheAsync();
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
            LibraryUpdated?.Invoke(this, new LibraryUpdatedEventArgs(series));
        }

        private void OnScanProgress(double progress, string seriesTitle, int scannedDirectories, int totalDirectories)
        {
            ScanProgress?.Invoke(this, new ScanProgressEventArgs(
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
    }
} 