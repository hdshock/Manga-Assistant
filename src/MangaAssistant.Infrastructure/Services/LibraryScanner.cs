using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Text.Json;
using MangaAssistant.Core.Models;
using MangaAssistant.Core.Services;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using MangaAssistant.Infrastructure.Services.EventArgs;

namespace MangaAssistant.Infrastructure.Services
{
    public class LibraryScanner
    {
        private const string SERIES_METADATA_FILE = "series-info.json";
        private const string DEFAULT_PLACEHOLDER = "folder-placeholder.jpg";
        private const int BATCH_SIZE = 5;
        private static readonly SemaphoreSlim _scanSemaphore = new(BATCH_SIZE);
        private static readonly string[] COVER_PATTERNS = new[] 
        {
            "cover.jpg", "cover.jpeg", "cover.png",
            "Cover.jpg", "Cover.jpeg", "Cover.png",
            "COVER.jpg", "COVER.jpeg", "COVER.png"
        };

        private readonly XmlSerializer _comicInfoSerializer;
        private readonly ISettingsService _settingsService;
        private readonly JsonSerializerOptions _jsonOptions;
        private int _totalDirectories;
        private int _scannedDirectories;
        private bool _isScanning;
        private CancellationTokenSource? _scanCancellationSource;

        // Common patterns for chapter numbers in filenames
        private static readonly Regex[] ChapterPatterns = new[]
        {
            new Regex(@"ch[a]?[p]?[ter]?\.?\s*(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase),
            new Regex(@"#(\d+(?:\.\d+)?)"),
            new Regex(@"[\[\(](\d+(?:\.\d+)?)[\]\)]"),
            new Regex(@"[-_](\d+(?:\.\d+)?)(?:\.|$)"),
            new Regex(@"(\d+(?:\.\d+)?)(?:\.|$)"),
        };

        // Common patterns for volume numbers in filenames
        private static readonly Regex[] VolumePatterns = new[]
        {
            new Regex(@"vol(?:ume)?\.?\s*(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase),
            new Regex(@"v\.?(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase),
            new Regex(@"volume\s*(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase),
        };

        public event EventHandler<ScanProgressEventArgs>? ScanProgress;
        public event EventHandler<Series>? SeriesFound;

        public bool IsScanning => _isScanning;

        public LibraryScanner(ISettingsService settingsService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _comicInfoSerializer = new XmlSerializer(typeof(ComicInfo));
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            // Generate placeholder image if it doesn't exist
            var placeholderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", DEFAULT_PLACEHOLDER);
            if (!File.Exists(placeholderPath))
            {
                PlaceholderGenerator.CreatePlaceholder();
            }
        }

        public void CancelScan()
        {
            _scanCancellationSource?.Cancel();
        }

        public async Task<List<Series>> ScanLibraryAsync(CancellationToken cancellationToken = default)
        {
            if (_isScanning)
            {
                Debug.WriteLine("Scan already in progress, returning empty list");
                return new List<Series>();
            }

            try
            {
                _isScanning = true;
                _scanCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                var libraryPath = _settingsService.LibraryPath;
                Debug.WriteLine($"Starting scan of library path: {libraryPath}");
                
                if (string.IsNullOrEmpty(libraryPath) || !Directory.Exists(libraryPath))
                {
                    Debug.WriteLine("Library path is empty or doesn't exist");
                    return new List<Series>();
                }

                return await ScanLibraryPathAsync(libraryPath, _scanCancellationSource.Token);
            }
            finally
            {
                _isScanning = false;
                _totalDirectories = 0;
                _scannedDirectories = 0;
            }
        }

        private async Task<List<Series>> ScanLibraryPathAsync(string libraryPath, CancellationToken cancellationToken)
        {
            var series = new ConcurrentBag<Series>();
            var directories = Directory.GetDirectories(libraryPath);
            
            Debug.WriteLine($"Found {directories.Length} directories to scan");
            _totalDirectories = directories.Length;
            _scannedDirectories = 0;

            var tasks = new List<Task>();
            foreach (var directory in directories)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Debug.WriteLine("Scan cancelled");
                    break;
                }

                await _scanSemaphore.WaitAsync(cancellationToken);
                
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        Debug.WriteLine($"Scanning directory: {directory}");
                        var scannedSeries = await ScanSeriesDirectoryAsync(directory);
                        if (scannedSeries != null)
                        {
                            Debug.WriteLine($"Found series: {scannedSeries.Title} with {scannedSeries.Chapters?.Count ?? 0} chapters");
                            series.Add(scannedSeries);
                            SeriesFound?.Invoke(this, scannedSeries);
                        }
                        else
                        {
                            Debug.WriteLine($"No series found in directory: {directory}");
                        }
                        
                        Interlocked.Increment(ref _scannedDirectories);
                        var progress = (double)_scannedDirectories / _totalDirectories;
                        OnScanProgress(progress, scannedSeries?.Title ?? string.Empty, _scannedDirectories, _totalDirectories);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error scanning directory {directory}: {ex.Message}");
                    }
                    finally
                    {
                        _scanSemaphore.Release();
                    }
                }, cancellationToken));
            }

            await Task.WhenAll(tasks.Where(t => !t.IsCanceled));
            Debug.WriteLine($"Scan complete. Found {series.Count} series");
            return series.OrderBy(s => s.Title).ToList();
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

        private string[] FindCoverImages(string directoryPath)
        {
            var coverFiles = new List<string>();
            
            // Look for cover files in the series directory
            foreach (var pattern in COVER_PATTERNS)
            {
                var coverPath = Path.Combine(directoryPath, pattern);
                if (File.Exists(coverPath))
                {
                    coverFiles.Add(coverPath);
                }
            }

            return coverFiles.ToArray();
        }

        private async Task<Series?> ScanSeriesDirectoryAsync(string directoryPath)
        {
            try
            {
                var cbzFiles = Directory.GetFiles(directoryPath, "*.cbz", SearchOption.TopDirectoryOnly);
                Debug.WriteLine($"Found {cbzFiles.Length} CBZ files in {directoryPath}");
                
                var directoryInfo = new DirectoryInfo(directoryPath);
                var seriesName = directoryInfo.Name;

                // Try to load existing metadata first
                var existingMetadata = await LoadSeriesMetadataAsync(directoryPath);
                if (existingMetadata != null)
                {
                    Debug.WriteLine($"Loaded existing metadata for {seriesName}");
                }
                
                // Find cover images
                var coverImages = FindCoverImages(directoryPath);
                Debug.WriteLine($"Found {coverImages.Length} cover images for {seriesName}");

                // Create series object with basic info
                var series = new Series
                {
                    Id = Guid.NewGuid(),
                    Title = existingMetadata?.Series ?? seriesName,
                    FolderPath = directoryPath,
                    Created = existingMetadata?.Created ?? directoryInfo.CreationTime,
                    LastModified = directoryInfo.LastWriteTime,
                    Metadata = existingMetadata ?? new SeriesMetadata(),
                    ChapterCount = cbzFiles.Length
                };

                // Handle cover image path
                if (coverImages.Length > 0)
                {
                    var selectedIndex = existingMetadata?.SelectedCoverIndex ?? 0;
                    series.CoverPath = selectedIndex >= 0 && selectedIndex < coverImages.Length 
                        ? coverImages[selectedIndex] 
                        : coverImages[0];
                    Debug.WriteLine($"Using cover image: {series.CoverPath}");
                }
                else
                {
                    Debug.WriteLine($"No cover images found for {seriesName}, using placeholder");
                    var placeholderPath = Path.Combine(directoryPath, DEFAULT_PLACEHOLDER);
                    if (!File.Exists(placeholderPath))
                    {
                        try
                        {
                            var sourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", DEFAULT_PLACEHOLDER);
                            Debug.WriteLine($"Copying placeholder from {sourcePath} to {placeholderPath}");
                            File.Copy(sourcePath, placeholderPath);
                            series.CoverPath = placeholderPath;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Failed to create placeholder: {ex.Message}");
                        }
                    }
                    else
                    {
                        series.CoverPath = placeholderPath;
                    }
                }

                if (!cbzFiles.Any())
                {
                    Debug.WriteLine($"No CBZ files found for {seriesName}");
                    series.Chapters = new List<Chapter>();
                    return series;
                }

                // Process chapters in parallel
                var chapters = new ConcurrentBag<Chapter>();
                var chapterTasks = cbzFiles.Select(async cbzFile =>
                {
                    var chapter = await ScanChapterFileAsync(cbzFile, series.Id);
                    if (chapter != null)
                    {
                        chapters.Add(chapter);
                        Debug.WriteLine($"Added chapter {chapter.Number} for {seriesName}");
                    }
                });

                await Task.WhenAll(chapterTasks);
                Debug.WriteLine($"Processed {chapters.Count} chapters for {seriesName}");

                // Sort chapters and update series
                series.Chapters = chapters.OrderBy(c => c.Number).ToList();
                return series;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error scanning directory {directoryPath}: {ex.Message}");
                return null;
            }
        }

        public async Task SaveSeriesMetadataAsync(Series series)
        {
            if (series == null) throw new ArgumentNullException(nameof(series));

            var metadataPath = Path.Combine(series.FolderPath, SERIES_METADATA_FILE);
            var json = JsonSerializer.Serialize(series.Metadata, _jsonOptions);

            await File.WriteAllTextAsync(metadataPath, json);
        }

        public async Task<SeriesMetadata?> LoadSeriesMetadataAsync(string directoryPath)
        {
            var metadataPath = Path.Combine(directoryPath, SERIES_METADATA_FILE);
            if (!File.Exists(metadataPath))
                return null;

            try
            {
                var json = await File.ReadAllTextAsync(metadataPath);
                return JsonSerializer.Deserialize<SeriesMetadata>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading metadata: {ex.Message}");
                return null;
            }
        }

        public async Task UpdateSeriesCover(string seriesPath, string coverPath)
        {
            var metadata = await LoadSeriesMetadataAsync(seriesPath);
            if (metadata == null)
                return;

            var coverImages = FindCoverImages(seriesPath);
            var selectedIndex = Array.IndexOf(coverImages, coverPath);
            if (selectedIndex >= 0)
            {
                metadata.CoverPath = coverPath;
                metadata.SelectedCoverIndex = selectedIndex;
                
                var metadataPath = Path.Combine(seriesPath, SERIES_METADATA_FILE);
                var json = JsonSerializer.Serialize(metadata, _jsonOptions);
                await File.WriteAllTextAsync(metadataPath, json);
            }
        }

        private async Task<Chapter?> ScanChapterFileAsync(string filePath, Guid seriesId)
        {
            var fileInfo = new FileInfo(filePath);
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var chapter = new Chapter
            {
                Id = Guid.NewGuid(),
                SeriesId = seriesId,
                FilePath = filePath,
                Added = fileInfo.CreationTime,
                LastRead = DateTime.MinValue,
                Number = ExtractChapterNumberFromFileName(fileName),
                Volume = ExtractVolumeNumberFromFileName(fileName),
                Title = fileName,
                PageCount = 0
            };

            try
            {
                using var fileStream = File.OpenRead(filePath);
                using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read);

                // Count image files efficiently
                chapter.PageCount = archive.Entries
                    .AsParallel()
                    .Count(e => IsImageFile(e.Name));

                // Try to extract chapter info from comicinfo.xml
                var comicInfo = await ExtractComicInfoAsync(filePath);
                if (comicInfo != null)
                {
                    if (!string.IsNullOrWhiteSpace(comicInfo.Title))
                    {
                        chapter.Title = comicInfo.Title;
                    }

                    if (comicInfo.PageCount > 0)
                    {
                        chapter.PageCount = comicInfo.PageCount;
                    }
                    
                    if (!string.IsNullOrWhiteSpace(comicInfo.Number) && 
                        double.TryParse(comicInfo.Number, out double number))
                    {
                        chapter.Number = number;
                    }
                    
                    if (!string.IsNullOrWhiteSpace(comicInfo.Volume) && 
                        int.TryParse(comicInfo.Volume, out int volume))
                    {
                        chapter.Volume = volume;
                    }
                }

                return chapter;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error scanning chapter: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return null;
            }
        }

        private double ExtractChapterNumberFromFileName(string fileName)
        {
            foreach (var pattern in ChapterPatterns)
            {
                var match = pattern.Match(fileName);
                if (match.Success && double.TryParse(match.Groups[1].Value, out double number))
                {
                    return number;
                }
            }
            return 1; // Default to chapter 1 if no number found
        }

        private int ExtractVolumeNumberFromFileName(string fileName)
        {
            foreach (var pattern in VolumePatterns)
            {
                var match = pattern.Match(fileName);
                if (match.Success && int.TryParse(match.Groups[1].Value, out int number))
                {
                    return number;
                }
            }
            
            // Try to extract volume from filename patterns like "Vol01" or "v01"
            var volMatch = Regex.Match(fileName, @"(?:^|[^a-zA-Z])(?:vol|v)\.?(\d+)(?:[^0-9]|$)", RegexOptions.IgnoreCase);
            if (volMatch.Success && int.TryParse(volMatch.Groups[1].Value, out int volNumber))
            {
                return volNumber;
            }
            
            return 0; // Default to volume 0 (N/A) if no volume found
        }

        private async Task<ComicInfo?> ExtractComicInfoAsync(string cbzPath)
        {
            try
            {
                using var fileStream = File.OpenRead(cbzPath);
                using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read);
                var comicInfoEntry = archive.Entries.FirstOrDefault(e => 
                    e.FullName.Equals("comicinfo.xml", StringComparison.OrdinalIgnoreCase) ||
                    e.FullName.Equals("ComicInfo.xml", StringComparison.OrdinalIgnoreCase));

                System.Diagnostics.Debug.WriteLine($"Looking for ComicInfo.xml in {cbzPath}");
                System.Diagnostics.Debug.WriteLine($"ComicInfo.xml found: {comicInfoEntry != null}");

                if (comicInfoEntry != null)
                {
                    using var stream = comicInfoEntry.Open();
                    using var reader = new StreamReader(stream);
                    var xmlContent = await reader.ReadToEndAsync();
                    System.Diagnostics.Debug.WriteLine($"ComicInfo.xml content: {xmlContent}");

                    // Create XmlSerializer with XML namespace handling
                    var xmlOverrides = new XmlAttributeOverrides();
                    var xmlRootAttr = new XmlRootAttribute("ComicInfo")
                    {
                        Namespace = "",
                        IsNullable = true
                    };
                    var serializer = new XmlSerializer(typeof(ComicInfo), xmlOverrides, Array.Empty<Type>(), xmlRootAttr, "");

                    using var stringReader = new StringReader(xmlContent);
                    var result = (ComicInfo?)serializer.Deserialize(stringReader);
                    System.Diagnostics.Debug.WriteLine($"ComicInfo deserialized: {result != null}");
                    if (result != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Series: {result.Series}, Title: {result.Title}, Number: {result.Number}, PageCount: {result.PageCount}");
                    }
                    return result;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error extracting ComicInfo: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            return null;
        }

        private bool IsImageFile(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLower();
            return extension == ".jpg" || extension == ".jpeg" || 
                   extension == ".png" || extension == ".gif" || 
                   extension == ".webp";
        }

        public async Task ClearCacheAsync()
        {
            // LibraryScanner doesn't need to handle cache clearing
            await Task.CompletedTask;
        }

        public async Task ClearAndRescanLibraryAsync()
        {
            // LibraryScanner doesn't need to handle cache clearing and rescanning
            await Task.CompletedTask;
        }
    }
} 