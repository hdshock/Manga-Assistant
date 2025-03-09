using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.Text.Json;
using MangaAssistant.Core.Models;
using MangaAssistant.Core.Services;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using MangaAssistant.Infrastructure.Services.EventArgs;
using MangaAssistant.Common.Logging;

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
        private readonly ConcurrentDictionary<string, SeriesMetadata> _metadataCache = new();
        private readonly SemaphoreSlim _scanLock = new(1, 1);
        private int _totalDirectories;
        private int _scannedDirectories;
        private bool _isScanning;
        private CancellationTokenSource? _scanCancellationSource;
        private readonly ConcurrentDictionary<string, (string Path, DateTime LastModified)> _coverCache = new();

        // Events
        public event EventHandler<Series>? SeriesFound;
        public event EventHandler<ScanProgressChangedEventArgs>? ScanProgressChanged;

        // Common patterns for chapter numbers in filenames - updated to handle decimal numbers better
        private static readonly Regex[] ChapterPatterns = new[]
        {
            // Explicit decimal chapter patterns (highest priority)
            new Regex(@"(?:chapter|ch\.?|chap\.?)\s*#?\s*(0\.\d+)", RegexOptions.IgnoreCase),
            new Regex(@"(?:chapter|ch\.?|chap\.?)\s*#?\s*(\d+\.\d+)", RegexOptions.IgnoreCase),
            
            // Standalone decimal numbers with clear boundaries
            new Regex(@"(?<!\d)(0\.\d+)(?!\d)"),
            new Regex(@"(?<!\d)(\d+\.\d+)(?!\d)"),
            
            // Regular chapter patterns with numbers
            new Regex(@"(?:chapter|ch\.?|chap\.?)\s*#?\s*(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase),
            
            // Standalone numbers with clear boundaries (lowest priority)
            new Regex(@"(?:^|_|\s|-|\.)(\d+(?:\.\d+)?)(?:$|_|\s|-|\.|\s*-\s*copy)", RegexOptions.IgnoreCase)
        };

        // Common patterns for volume numbers in filenames - updated to match Manga Manager's approach
        private static readonly Regex[] VolumePatterns = new[]
        {
            // Explicit volume indicators
            new Regex(@"(?:^|[^\w])(?:volume|vol\.?)\s*[-._]?\s*(\d+(?:\.\d+)?)(?:[^\d]|$)", RegexOptions.IgnoreCase),
            new Regex(@"(?:^|[^\w])v\.?(\d+(?:\.\d+)?)(?:[^\d]|$)", RegexOptions.IgnoreCase),
            
            // Bracketed/parenthesized volume numbers
            new Regex(@"[\[\(]\s*(?:v(?:ol(?:ume)?)?)\s*[-._]?\s*(\d+(?:\.\d+)?)\s*[\]\)]", RegexOptions.IgnoreCase),
            
            // Japanese/Chinese volume indicators
            new Regex(@"(?:^|[^\w])(?:巻|卷)\s*(\d+(?:\.\d+)?)(?:[^\d]|$)"),
            
            // Season indicators (sometimes used instead of volumes)
            new Regex(@"(?:^|[^\w])(?:season|s)\s*[-._]?\s*(\d+(?:\.\d+)?)(?:[^\d]|$)", RegexOptions.IgnoreCase)
        };

        public event EventHandler<ScanProgressEventArgs>? ScanProgress;

        public bool IsScanning => _isScanning;

        public LibraryScanner(ISettingsService settingsService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _comicInfoSerializer = new XmlSerializer(typeof(ComicInfo));
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };
            
            // Log initialization
            Logger.Log($"LibraryScanner initialized. InsertCoverIntoFirstChapter setting: {_settingsService.InsertCoverIntoFirstChapter}", LogLevel.Info);
        }

        public void CancelScan()
        {
            _scanCancellationSource?.Cancel();
        }

        public async Task<ScanResult> ScanLibraryAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if a scan is already in progress
            if (_isScanning)
            {
                    Logger.Log("Scan already in progress, ignoring request", LogLevel.Warning);
                    return new ScanResult { Success = false, Message = "Scan already in progress" };
            }

                // Acquire the scan lock to prevent multiple scans
                await _scanLock.WaitAsync(cancellationToken);
                _isScanning = true;

                // Create a new cancellation token source that can be used to cancel the scan
                _scanCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var linkedToken = _scanCancellationSource.Token;

                // Get the library path from settings
                var libraryPath = _settingsService.LibraryPath;
                if (string.IsNullOrEmpty(libraryPath) || !Directory.Exists(libraryPath))
                {
                    Logger.Log($"Library path is invalid or does not exist: {libraryPath}", LogLevel.Error);
                    return new ScanResult { Success = false, Message = "Library path is invalid or does not exist" };
                }

                Logger.Log($"Starting library scan at: {libraryPath}", LogLevel.Info);

                // Get all directories in the library path
            var directories = Directory.GetDirectories(libraryPath);
            _totalDirectories = directories.Length;
            _scannedDirectories = 0;

                Logger.Log($"Found {_totalDirectories} directories to scan", LogLevel.Info);

                // Create a list to store the series
                var seriesList = new List<Series>();
                var tasks = new List<Task<Series?>>();

                // Process each directory in batches
            foreach (var directory in directories)
            {
                    // Check if the scan has been cancelled
                    if (linkedToken.IsCancellationRequested)
                {
                        Logger.Log("Scan cancelled by user", LogLevel.Warning);
                    break;
                }

                    // Wait for a slot in the semaphore
                    await _scanSemaphore.WaitAsync(linkedToken);
                
                    // Start a task to process the directory
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                            // Process the directory
                            var series = await ProcessDirectoryAsync(directory, linkedToken);
                            return series;
                        }
                        catch (Exception ex)
                        {
                            Logger.Log($"Error processing directory {directory}: {ex.Message}", LogLevel.Error);
                            return null;
                        }
                        finally
                        {
                            // Release the semaphore slot
                            _scanSemaphore.Release();
                            
                            // Update the progress
                        Interlocked.Increment(ref _scannedDirectories);
                            
                            // Raise the progress event
                            OnScanProgressChanged(new ScanProgressChangedEventArgs
                            {
                                TotalDirectories = _totalDirectories,
                                ScannedDirectories = _scannedDirectories,
                                ProgressPercentage = (int)((double)_scannedDirectories / _totalDirectories * 100)
                            });
                        }
                    }, linkedToken));

                    // When we have enough tasks, wait for them to complete
                    if (tasks.Count >= BATCH_SIZE)
                    {
                        var completedTasks = await Task.WhenAll(tasks);
                        var validSeries = completedTasks.Where(s => s != null).Select(s => s!).ToList();
                        seriesList.AddRange(validSeries);
                        tasks.Clear();
                    }
                }

                // Wait for any remaining tasks
                if (tasks.Count > 0)
                {
                    var completedTasks = await Task.WhenAll(tasks);
                    var validSeries = completedTasks.Where(s => s != null).Select(s => s!).ToList();
                    seriesList.AddRange(validSeries);
                }

                Logger.Log($"Scan completed. Found {seriesList.Count} series", LogLevel.Info);

                // Return the scan result
                return new ScanResult
                {
                    Success = true,
                    Series = seriesList,
                    Message = $"Scan completed. Found {seriesList.Count} series"
                };
            }
            catch (OperationCanceledException)
            {
                Logger.Log("Scan was cancelled", LogLevel.Warning);
                return new ScanResult { Success = false, Message = "Scan was cancelled" };
                    }
                    catch (Exception ex)
                    {
                Logger.Log($"Error during library scan: {ex.Message}", LogLevel.Error);
                return new ScanResult { Success = false, Message = $"Error during scan: {ex.Message}" };
                    }
                    finally
                    {
                // Reset the scanning state
                _isScanning = false;
                _scanCancellationSource?.Dispose();
                _scanCancellationSource = null;
                
                // Release the scan lock
                _scanLock.Release();
            }
        }

        private async Task<Series?> ProcessDirectoryAsync(string directoryPath, CancellationToken cancellationToken)
        {
            try
            {
                Logger.Log($"Starting directory scan", LogLevel.Info, "LibraryScanner", directoryPath);
                var scannedSeries = await ScanSeriesDirectoryAsync(directoryPath);
                if (scannedSeries != null)
                {
                    Logger.Log($"Found series: {scannedSeries.Title}", LogLevel.Info, "LibraryScanner", directoryPath,
                        new Dictionary<string, string>
                        {
                            { "SeriesTitle", scannedSeries.Title },
                            { "ChapterCount", (scannedSeries.Chapters?.Count ?? 0).ToString() },
                            { "HasMetadata", (scannedSeries.Metadata != null).ToString() }
                        });
                    SeriesFound?.Invoke(this, scannedSeries);
                }
                else
                {
                    Logger.Log($"No series found", LogLevel.Warning, "LibraryScanner", directoryPath);
                }
                
                return scannedSeries;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error scanning directory: {ex.Message}", LogLevel.Error, "LibraryScanner", directoryPath,
                    new Dictionary<string, string>
                    {
                        { "ErrorType", ex.GetType().Name },
                        { "StackTrace", ex.StackTrace ?? "No stack trace" }
                    });
                return null;
            }
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

        private string GetSeriesCoverPath(string seriesPath)
        {
            try
            {
                // Check if we have a valid cached cover
                if (_coverCache.TryGetValue(seriesPath, out var cached))
                {
                    var coverFile = new FileInfo(cached.Path);
                    if (coverFile.Exists && coverFile.LastWriteTime == cached.LastModified)
                    {
                        return cached.Path;
                    }
                }

                // Look for cover.jpg first
                var primaryCover = Path.Combine(seriesPath, "cover.jpg");
                if (File.Exists(primaryCover))
                {
                    var fileInfo = new FileInfo(primaryCover);
                    _coverCache[seriesPath] = (primaryCover, fileInfo.LastWriteTime);
                    return primaryCover;
                }

                // Fall back to other cover patterns if needed
            foreach (var pattern in COVER_PATTERNS)
            {
                    var coverPath = Path.Combine(seriesPath, pattern);
                if (File.Exists(coverPath))
                {
                        var fileInfo = new FileInfo(coverPath);
                        _coverCache[seriesPath] = (coverPath, fileInfo.LastWriteTime);
                        return coverPath;
                    }
                }

                // Use placeholder if no cover found
                var placeholderPath = Path.Combine(seriesPath, DEFAULT_PLACEHOLDER);
                if (File.Exists(placeholderPath))
                {
                    var fileInfo = new FileInfo(placeholderPath);
                    _coverCache[seriesPath] = (placeholderPath, fileInfo.LastWriteTime);
                    return placeholderPath;
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error getting series cover path: {ex.Message}", LogLevel.Error, "LibraryScanner");
                return string.Empty;
            }
        }

        private async Task<Series?> ScanSeriesDirectoryAsync(string directoryPath)
        {
            try
            {
                var cbzFiles = Directory.GetFiles(directoryPath, "*.cbz", SearchOption.TopDirectoryOnly);
                Logger.Log($"Found {cbzFiles.Length} CBZ files", LogLevel.Info, "LibraryScanner", directoryPath);
                
                var directoryInfo = new DirectoryInfo(directoryPath);
                var seriesName = directoryInfo.Name;

                // Try to load existing metadata first
                var existingMetadata = await LoadSeriesMetadataAsync(directoryPath);
                Logger.Log($"Metadata status: {(existingMetadata != null ? "Found" : "Not found")}", 
                    LogLevel.Info, "LibraryScanner", directoryPath,
                    new Dictionary<string, string>
                {
                        { "HasMetadata", (existingMetadata != null).ToString() },
                        { "SeriesName", seriesName }
                    });
                
                // Create series object first
                var series = new Series
                {
                    Id = Guid.NewGuid(),
                    Title = seriesName, // Will be updated later if better title is found
                    FolderPath = directoryPath,
                    Created = existingMetadata?.Created ?? directoryInfo.CreationTime,
                    LastModified = directoryInfo.LastWriteTime,
                    Metadata = existingMetadata ?? new SeriesMetadata(),
                    ChapterCount = cbzFiles.Length
                };

                // Handle cover image using the new method
                var coverPath = GetSeriesCoverPath(directoryPath);
                if (!string.IsNullOrEmpty(coverPath))
                {
                    series.CoverPath = coverPath;
                    if (existingMetadata != null)
                    {
                        existingMetadata.CoverPath = coverPath;
                        existingMetadata.LastModified = DateTime.Now;
                        await SaveSeriesMetadataAsync(series);
                    }
                }

                // Determine series title
                string seriesTitle = await DetermineSeriesTitleAsync(directoryPath, cbzFiles, existingMetadata, seriesName);
                series.Title = seriesTitle;
                Logger.Log($"Determined series title: {seriesTitle}", LogLevel.Info, "LibraryScanner", directoryPath,
                    new Dictionary<string, string>
                    {
                        { "FinalTitle", seriesTitle },
                        { "OriginalName", seriesName },
                        { "TitleSource", DetermineTitleSource(existingMetadata, seriesTitle, seriesName) }
                    });

                if (!cbzFiles.Any())
                {
                    Logger.Log($"No CBZ files found", LogLevel.Warning, "LibraryScanner", directoryPath);
                    series.Chapters = new List<Chapter>();
                    return series;
                }

                return await ProcessSeriesChaptersAsync(series, cbzFiles, directoryPath);
            }
            catch (Exception ex)
            {
                Logger.Log($"Error scanning directory: {ex.Message}", LogLevel.Error, "LibraryScanner");
                return null;
            }
        }

        private string DetermineTitleSource(SeriesMetadata? metadata, string finalTitle, string folderName)
        {
            if (metadata != null && !string.IsNullOrWhiteSpace(metadata.Series) && metadata.Series == finalTitle)
                return "Metadata";
            if (finalTitle == folderName)
                return "FolderName";
            return "ComicInfo";
        }

        // Process chapters in parallel and handle cover insertion for first chapter if enabled
        private async Task<Series?> ProcessSeriesChaptersAsync(Series series, string[] cbzFiles, string directoryPath)
        {
            try
            {
                // Process chapters in parallel
                var chapters = new ConcurrentBag<Chapter>();
                var processedNumbers = new HashSet<double>(); // Track processed chapter numbers
                
                Logger.Log($"Starting chapter processing for {series.Title}. Found {cbzFiles.Length} files.", LogLevel.Info);
                foreach (var cbzFile in cbzFiles)
                {
                    Logger.Log($"Processing file: {Path.GetFileName(cbzFile)}", LogLevel.Info);
                }

                var chapterTasks = cbzFiles.Select(async cbzFile =>
                {
                    var chapter = await CreateChapterFromFileAsync(cbzFile, series.Id);
                    if (chapter != null)
                    {
                        // Check if we've already processed this chapter number
                        bool isNewChapter = false;
                        lock (processedNumbers)
                        {
                            if (!processedNumbers.Contains(chapter.Number))
                            {
                                processedNumbers.Add(chapter.Number);
                                isNewChapter = true;
                            }
                        }

                        if (isNewChapter)
                    {
                        chapters.Add(chapter);
                            Logger.Log($"Added chapter {chapter.Number} for {series.Title} from file: {Path.GetFileName(chapter.FilePath)}", LogLevel.Info);
                        }
                        else
                        {
                            Logger.Log($"Skipping duplicate chapter number {chapter.Number} from file: {Path.GetFileName(chapter.FilePath)}", LogLevel.Warning);
                        }
                    }
                });

                await Task.WhenAll(chapterTasks);
                Logger.Log($"Processed {chapters.Count} unique chapters for {series.Title}", LogLevel.Info);

                // Sort chapters and update series
                var sortedChapters = chapters.OrderBy(c => c.Number).ToList();
                series.Chapters = sortedChapters;
                
                // Log chapter numbers for verification
                Logger.Log($"Final sorted chapters for {series.Title}:", LogLevel.Info);
                foreach (var chapter in sortedChapters)
                {
                    Logger.Log($"Chapter {chapter.Number} - File: {Path.GetFileName(chapter.FilePath)}", LogLevel.Info);
                }

                // Insert cover into first chapter if setting is enabled
                if (_settingsService.InsertCoverIntoFirstChapter && sortedChapters.Count > 0)
                {
                    Logger.Log($"Cover insertion setting is enabled. Looking for first chapter in {series.Title}", LogLevel.Info);
                    var firstChapter = sortedChapters[0];
                    Logger.Log($"Found first chapter: {firstChapter.Title}, Number: {firstChapter.Number}", LogLevel.Info);
                        string coverPath = Path.Combine(series.FolderPath, "cover.jpg");
                    Logger.Log($"Looking for cover image at: {coverPath}", LogLevel.Info);
                        if (File.Exists(coverPath))
                        {
                        Logger.Log($"Cover image found. Attempting to insert into: {firstChapter.FilePath}", LogLevel.Info);
                            await InsertCoverIntoChapterAsync(firstChapter.FilePath, coverPath);
                        }
                        else
                        {
                        Logger.Log($"Cover image not found at: {coverPath}", LogLevel.Info);
                        }
                    }
                    else
                    {
                    Logger.Log($"Cover insertion setting is disabled or no chapters found. Setting: {_settingsService.InsertCoverIntoFirstChapter}, Chapters: {sortedChapters.Count}", LogLevel.Info);
                }

                return series;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error processing chapters: {ex.Message}", LogLevel.Error);
                return series;
            }
        }

        // Method to insert cover image into a CBZ file
        public async Task<bool> InsertCoverIntoChapterAsync(string cbzFilePath, string coverImagePath)
        {
            try
            {
                Logger.Log($"Starting cover insertion for: {cbzFilePath}", LogLevel.Info);
                
                if (!File.Exists(cbzFilePath))
                {
                    Logger.Log($"CBZ file does not exist: {cbzFilePath}", LogLevel.Warning);
                    return false;
                }
                
                if (!File.Exists(coverImagePath))
                {
                    Logger.Log($"Cover image does not exist: {coverImagePath}", LogLevel.Warning);
                    return false;
                }
                
                // Create a temporary file for the new archive
                string tempFilePath = Path.Combine(
                    Path.GetDirectoryName(cbzFilePath) ?? string.Empty,
                    $"temp_{Path.GetFileName(cbzFilePath)}"
                );
                
                Logger.Log($"Created temporary file at: {tempFilePath}", LogLevel.Info);

                try
                {
                // Read the cover image into memory
                byte[] coverImageBytes = await File.ReadAllBytesAsync(coverImagePath);
                    Logger.Log($"Read cover image, size: {coverImageBytes.Length} bytes", LogLevel.Info);

                    // Create a new archive with the cover image as the first file
                    using (var tempFileStream = File.Create(tempFilePath))
                    using (var tempArchive = new ZipArchive(tempFileStream, ZipArchiveMode.Create, true))
                    {
                        // Add cover image as the first file
                        var coverEntry = tempArchive.CreateEntry("cover.jpg");
                        using (var entryStream = coverEntry.Open())
                        {
                            await entryStream.WriteAsync(coverImageBytes, 0, coverImageBytes.Length);
                            Logger.Log("Added cover image to temporary archive", LogLevel.Info);
                        }

                        // Copy all entries from the original archive
                        Logger.Log($"Opening original archive: {cbzFilePath}", LogLevel.Info);
                        using (var originalFileStream = File.OpenRead(cbzFilePath))
                        using (var originalArchive = new ZipArchive(originalFileStream, ZipArchiveMode.Read))
                        {
                            Logger.Log($"Original archive has {originalArchive.Entries.Count} entries", LogLevel.Info);
                            foreach (var entry in originalArchive.Entries)
                            {
                                // Skip if it's already a cover image with the same name
                                if (entry.Name.Equals("cover.jpg", StringComparison.OrdinalIgnoreCase))
                                {
                                    Logger.Log($"Skipping existing cover image: {entry.Name}", LogLevel.Info);
                                    continue;
                                }

                                // Copy the entry to the new archive
                                Logger.Log($"Copying entry: {entry.Name}", LogLevel.Info);
                                var newEntry = tempArchive.CreateEntry(entry.Name);
                                using (var originalEntryStream = entry.Open())
                                using (var newEntryStream = newEntry.Open())
                                {
                                    await originalEntryStream.CopyToAsync(newEntryStream);
                                }
                            }
                        }
                    }

                    // Replace the original file with the new one
                    Logger.Log($"Deleting original file: {cbzFilePath}", LogLevel.Info);
                    File.Delete(cbzFilePath);
                    Logger.Log($"Moving temporary file to original location", LogLevel.Info);
                    File.Move(tempFilePath, cbzFilePath);

                    Logger.Log($"Successfully inserted cover image into {cbzFilePath}", LogLevel.Info);
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error during archive operations: {ex.Message}", LogLevel.Error);
                    throw;
                }
                finally
                {
                    // Always clean up the temporary file if it exists
                    if (File.Exists(tempFilePath))
                    {
                        try
                        {
                            File.Delete(tempFilePath);
                            Logger.Log($"Cleaned up temporary file: {tempFilePath}", LogLevel.Info);
                        }
                        catch (Exception cleanupEx)
                        {
                            Logger.Log($"Error cleaning up temporary file: {cleanupEx.Message}", LogLevel.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error inserting cover image: {ex.Message}", LogLevel.Error);
                Logger.Log($"Stack trace: {ex.StackTrace}", LogLevel.Error);
                return false;
            }
        }

        private async Task<Chapter?> CreateChapterFromFileAsync(string filePath, Guid seriesId)
        {
            try
        {
            var fileInfo = new FileInfo(filePath);
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            
                Logger.Log($"Processing chapter file", LogLevel.Info, "ChapterProcessing", filePath);
                
                // First try to extract metadata from ComicInfo.xml
                var comicInfo = await ExtractComicInfoAsync(filePath);
                if (comicInfo != null)
                {
                    Logger.Log($"Found ComicInfo.xml", LogLevel.Info, "ChapterProcessing", filePath,
                        new Dictionary<string, string>
                        {
                            { "HasNumber", (!string.IsNullOrWhiteSpace(comicInfo.Number)).ToString() },
                            { "RawNumber", comicInfo.Number ?? "null" },
                            { "Title", comicInfo.Title ?? "null" }
                        });
                    
                    if (!string.IsNullOrWhiteSpace(comicInfo.Number))
                    {
                        string normalizedNumber = comicInfo.Number.Replace(',', '.');
                        if (double.TryParse(normalizedNumber, out double chapterNumber))
                        {
                            Logger.Log($"Successfully parsed chapter number from ComicInfo", LogLevel.Info, "ChapterProcessing", filePath,
                                new Dictionary<string, string>
                                {
                                    { "ParsedNumber", chapterNumber.ToString() },
                                    { "Source", "ComicInfo" }
                                });
                            
                            return CreateChapterFromMetadata(filePath, seriesId, chapterNumber, comicInfo);
                        }
                    }
                }
                
                // Fall back to filename parsing
                return CreateChapterFromFileName(filePath, seriesId);
            }
            catch (Exception ex)
            {
                Logger.Log($"Error processing chapter: {ex.Message}", LogLevel.Error, "ChapterProcessing", filePath,
                    new Dictionary<string, string>
                    {
                        { "ErrorType", ex.GetType().Name },
                        { "StackTrace", ex.StackTrace ?? "No stack trace" }
                    });
                return null;
            }
        }

        private Chapter CreateChapterFromMetadata(string filePath, Guid seriesId, double chapterNumber, ComicInfo comicInfo)
        {
            var fileInfo = new FileInfo(filePath);
                    return new Chapter
                    {
                        Id = Guid.NewGuid(),
                        SeriesId = seriesId,
                        FilePath = filePath,
                        Added = fileInfo.CreationTime,
                        LastRead = DateTime.MinValue,
                Number = chapterNumber,
                Volume = !string.IsNullOrWhiteSpace(comicInfo.Volume) && int.TryParse(comicInfo.Volume, out int vol) ? vol : 0,
                Title = !string.IsNullOrWhiteSpace(comicInfo.Title) ? comicInfo.Title : Path.GetFileNameWithoutExtension(filePath),
                PageCount = comicInfo.PageCount
            };
        }

        private Chapter CreateChapterFromFileName(string filePath, Guid seriesId)
        {
            var fileInfo = new FileInfo(filePath);
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            
            Logger.Log($"Extracting chapter info from filename", LogLevel.Info, "ChapterProcessing", filePath);
            
            string normalizedFileName = NormalizeFileName(fileName);
            Logger.Log($"Normalized filename", LogLevel.Debug, "ChapterProcessing", filePath,
                new Dictionary<string, string>
                {
                    { "Original", fileName },
                    { "Normalized", normalizedFileName }
                });
            
            double chapterNumber = ExtractChapterNumberFromFileName(normalizedFileName, fileName, Path.GetFileName(Path.GetDirectoryName(filePath) ?? string.Empty));
            int volumeNumber = ExtractVolumeNumberFromFileName(normalizedFileName);
            
            Logger.Log($"Extracted chapter information", LogLevel.Info, "ChapterProcessing", filePath,
                new Dictionary<string, string>
                {
                    { "ChapterNumber", chapterNumber.ToString() },
                    { "VolumeNumber", volumeNumber.ToString() },
                    { "Source", "FileName" }
                });
            
                return new Chapter
                {
                    Id = Guid.NewGuid(),
                    SeriesId = seriesId,
                    FilePath = filePath,
                    Added = fileInfo.CreationTime,
                    LastRead = DateTime.MinValue,
                Number = chapterNumber,
                Volume = volumeNumber,
                    Title = fileName,
                    PageCount = 0
                };
            }
            
        private string NormalizeFileName(string fileName)
        {
            // First protect decimal numbers by temporarily replacing them
            var tempFileName = fileName;
            var decimalMatches = Regex.Matches(tempFileName, @"(?<!\d)(\d+\.\d+)(?!\d)");
            var replacements = new Dictionary<string, string>();
            
            foreach (Match match in decimalMatches)
            {
                var placeholder = $"__DECIMAL_{replacements.Count}__";
                replacements[placeholder] = match.Value;
                tempFileName = tempFileName.Replace(match.Value, placeholder);
            }
            
            // Now do the normal cleanup
            tempFileName = tempFileName.Replace('_', ' ');
            tempFileName = tempFileName.Replace('.', ' ');
            
            // Remove common file suffixes
            tempFileName = Regex.Replace(tempFileName, @"\b(?:digital|webrip|web|cbz|cbr|zip|rar)\b", "", RegexOptions.IgnoreCase);
            
            // Remove common scan group tags in brackets or parentheses
            tempFileName = Regex.Replace(tempFileName, @"[\[\(](?:[^\[\]\(\)]+)[\]\)]", " ");
            
            // Normalize whitespace
            tempFileName = Regex.Replace(tempFileName, @"\s+", " ").Trim();
            
            // Restore decimal numbers
            foreach (var replacement in replacements)
            {
                tempFileName = tempFileName.Replace(replacement.Key, replacement.Value);
            }
            
            Logger.Log($"Normalized filename from '{fileName}' to '{tempFileName}'", LogLevel.Info);
            return tempFileName;
        }
        
        private string ExtractSeriesNameFromFileName(string normalizedFileName, string directoryName)
        {
            // Try to match series name based on directory name
            if (!string.IsNullOrWhiteSpace(directoryName))
            {
                // If directory name is found in the filename, it's likely the series name
                if (normalizedFileName.Contains(directoryName, StringComparison.OrdinalIgnoreCase))
                {
                    return directoryName;
                }
                
                // Try to match partial directory name (for cases where directory name is longer)
                string[] dirNameParts = directoryName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (dirNameParts.Length > 1)
                {
                    // Try to match at least 2 consecutive words from directory name
                    for (int i = 0; i < dirNameParts.Length - 1; i++)
                    {
                        string partialName = $"{dirNameParts[i]} {dirNameParts[i + 1]}";
                        if (normalizedFileName.Contains(partialName, StringComparison.OrdinalIgnoreCase))
                        {
                            return partialName;
                        }
                    }
                }
            }
            
            // Try to extract series name using common patterns
            // Pattern: Series Name - Chapter XX
            var dashPattern = Regex.Match(normalizedFileName, @"^(.+?)\s*[-–—]\s*(?:Chapter|Ch|Episode|Ep)?\s*\d", RegexOptions.IgnoreCase);
            if (dashPattern.Success && dashPattern.Groups.Count > 1)
            {
                return dashPattern.Groups[1].Value.Trim();
            }
            
            // Pattern: Series Name Chapter XX
            var spacePattern = Regex.Match(normalizedFileName, @"^(.+?)\s+(?:Chapter|Ch|Episode|Ep)\s+\d", RegexOptions.IgnoreCase);
            if (spacePattern.Success && spacePattern.Groups.Count > 1)
            {
                return spacePattern.Groups[1].Value.Trim();
            }
            
            return string.Empty;
        }
        
        private double ExtractChapterNumberFromFileName(string fileNameWithoutSeries, string originalFileName, string directoryName)
        {
            Logger.Log($"Extracting chapter number from: '{originalFileName}'", LogLevel.Info);
            
            // First try to find decimal numbers directly (highest priority)
            var decimalMatch = Regex.Match(originalFileName, @"(?<!\d)(0\.\d+)(?!\d)");
            if (decimalMatch.Success && double.TryParse(decimalMatch.Groups[1].Value, out double decimalNumber))
            {
                Logger.Log($"Found decimal chapter number directly: {decimalNumber}", LogLevel.Info);
                return decimalNumber;
            }
            
            // Then try normalized filename with patterns
            string normalizedFileName = NormalizeFileName(originalFileName);
            Logger.Log($"Normalized filename: '{normalizedFileName}'", LogLevel.Info);
            
            // Try each pattern in order of reliability
            foreach (var pattern in ChapterPatterns)
            {
                var match = pattern.Match(normalizedFileName);
                if (match.Success)
                {
                    string matchValue = match.Groups[1].Value;
                    if (double.TryParse(matchValue, out double number))
                    {
                        // Special handling for "- Copy" suffix - keep the original number
                        if (originalFileName.Contains("- Copy", StringComparison.OrdinalIgnoreCase))
                        {
                            Logger.Log($"Found chapter number in copy file: {number}", LogLevel.Info);
                        }
                        else
                        {
                            Logger.Log($"Found chapter number: {number} using pattern: {pattern}", LogLevel.Info);
                        }
                        return number;
                    }
                }
            }
            
            // If we still can't find a chapter number, look for any number in the filename
            var lastNumberMatch = Regex.Match(normalizedFileName, @"(\d+(?:\.\d+)?)");
            if (lastNumberMatch.Success)
            {
                string matchValue = lastNumberMatch.Groups[1].Value;
                if (double.TryParse(matchValue, out double lastNumber))
                {
                    Logger.Log($"Using last number in filename as chapter: {lastNumber}", LogLevel.Info);
                    return lastNumber;
                }
            }
            
            // Default to chapter 1 if no number found
            Logger.Log($"No chapter number found in '{originalFileName}', defaulting to 1", LogLevel.Info);
            return 1;
        }
        
        private int ExtractVolumeNumberFromFileName(string fileName)
        {
            // Try each volume pattern
            foreach (var pattern in VolumePatterns)
            {
                var match = pattern.Match(fileName);
                if (match.Success && int.TryParse(match.Groups[1].Value, out int number))
                {
                    Logger.Log($"Found volume number {number} using pattern: {pattern}", LogLevel.Info);
                    return number;
                }
            }
            
            // Try to extract volume from season indicators (sometimes used instead of volumes)
            var seasonMatch = Regex.Match(fileName, @"(?:Season|S)\.?\s*(\d+)", RegexOptions.IgnoreCase);
            if (seasonMatch.Success && int.TryParse(seasonMatch.Groups[1].Value, out int seasonNumber))
            {
                Logger.Log($"Found season number as volume: {seasonNumber}", LogLevel.Info);
                return seasonNumber;
            }
            
            return 0; // Default to volume 0 (N/A) if no volume found
        }

        // Helper method to determine the series title based on priority
        private async Task<string> DetermineSeriesTitleAsync(string directoryPath, string[] cbzFiles, SeriesMetadata? metadata, string folderName)
        {
            // Priority 1: Use metadata file if it exists and has a series name
            if (metadata != null && !string.IsNullOrWhiteSpace(metadata.Series))
            {
                Logger.Log($"Using series title from metadata file: {metadata.Series}", LogLevel.Info);
                return metadata.Series;
            }

            // Priority 2: Look for series title in CBZ files' ComicInfo.xml
            foreach (var cbzFile in cbzFiles)
            {
                try
                {
                    var comicInfo = await ExtractComicInfoAsync(cbzFile);
                    if (comicInfo != null && !string.IsNullOrWhiteSpace(comicInfo.Series))
                    {
                        Logger.Log($"Using series title from CBZ ComicInfo: {comicInfo.Series}", LogLevel.Info);
                        return comicInfo.Series;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error extracting ComicInfo from {cbzFile}: {ex.Message}", LogLevel.Error);
                    // Continue to the next file if there's an error
                }
            }

            // Priority 3: Use folder name as a last resort
            Logger.Log($"Using folder name as series title: {folderName}", LogLevel.Info);
            return folderName;
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
            {
                Logger.Log($"No metadata file found at {metadataPath}", LogLevel.Info, "LibraryScanner");
                return null;
            }

            try
            {
                var json = await File.ReadAllTextAsync(metadataPath);
                var metadata = JsonSerializer.Deserialize<SeriesMetadata>(json, _jsonOptions);
                
                // Ensure cover properties are in sync
                if (metadata != null)
                {
                    if (!string.IsNullOrEmpty(metadata.CoverUrl) && string.IsNullOrEmpty(metadata.CoverPath))
                    {
                        metadata.CoverPath = metadata.CoverUrl;
                    }
                    else if (!string.IsNullOrEmpty(metadata.CoverPath) && string.IsNullOrEmpty(metadata.CoverUrl))
                    {
                        metadata.CoverUrl = metadata.CoverPath;
                    }
                }
                
                return metadata;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error loading metadata: {ex.Message}", LogLevel.Error, "LibraryScanner");
                return null;
            }
        }

        public async Task UpdateSeriesCover(string seriesPath, string coverPath)
        {
            try
            {
                Logger.Log($"Updating series cover at {seriesPath} with new cover: {coverPath}", LogLevel.Info, "LibraryScanner");
                
            var metadata = await LoadSeriesMetadataAsync(seriesPath);
            if (metadata == null)
                {
                    metadata = new SeriesMetadata();
                }

            var coverImages = FindCoverImages(seriesPath);
            var selectedIndex = Array.IndexOf(coverImages, coverPath);
                
            if (selectedIndex >= 0)
            {
                metadata.CoverPath = coverPath;
                    metadata.CoverUrl = coverPath; // Sync both cover properties
                metadata.SelectedCoverIndex = selectedIndex;
                    metadata.LastModified = DateTime.Now; // Update modification time to trigger refresh
                
                var metadataPath = Path.Combine(seriesPath, SERIES_METADATA_FILE);
                var json = JsonSerializer.Serialize(metadata, _jsonOptions);
                await File.WriteAllTextAsync(metadataPath, json);
                    
                    Logger.Log($"Successfully updated cover metadata at {metadataPath}", LogLevel.Info, "LibraryScanner");
                    
                    // Raise the ScanProgress event to notify UI of the change
                    OnScanProgress(100, metadata.Series, 1, 1);
                }
                else
                {
                    Logger.Log($"Cover path not found in available covers: {coverPath}", LogLevel.Warning, "LibraryScanner");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error updating series cover: {ex.Message}", LogLevel.Error, "LibraryScanner");
            }
        }

        private async Task<ComicInfo?> ExtractComicInfoAsync(string cbzPath)
        {
            try
            {
                using var fileStream = File.OpenRead(cbzPath);
                using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read);
                
                // Look for ComicInfo.xml with case-insensitive search
                var comicInfoEntry = archive.Entries.FirstOrDefault(e => 
                    e.FullName.Equals("comicinfo.xml", StringComparison.OrdinalIgnoreCase));

                if (comicInfoEntry == null)
                {
                    Logger.Log($"No ComicInfo.xml found in {cbzPath}", LogLevel.Debug, "LibraryScanner");
                    return null;
                }

                try
                {
                    using var stream = comicInfoEntry.Open();
                    using var reader = new StreamReader(stream);
                    var xmlContent = await reader.ReadToEndAsync();

                    // Pre-process XML content
                    xmlContent = PreProcessXmlContent(xmlContent);

                    // Create XML settings that match Kavita's expectations
                    var xmlSettings = new XmlReaderSettings
                    {
                        IgnoreWhitespace = true,
                        IgnoreComments = true,
                        IgnoreProcessingInstructions = true,
                        CheckCharacters = false,
                        DtdProcessing = DtdProcessing.Ignore,
                        ValidationType = ValidationType.None,
                        ConformanceLevel = ConformanceLevel.Auto
                    };

                    // Create XmlSerializer with flexible namespace handling
                    var xmlOverrides = new XmlAttributeOverrides();
                    var xmlRootAttr = new XmlRootAttribute("ComicInfo")
                    {
                        Namespace = string.Empty,
                        IsNullable = true
                    };

                    var serializer = new XmlSerializer(typeof(ComicInfo), xmlOverrides, Array.Empty<Type>(), xmlRootAttr, string.Empty);

                    using var stringReader = new StringReader(xmlContent);
                    using var xmlReader = XmlReader.Create(stringReader, xmlSettings);

                    var result = (ComicInfo?)serializer.Deserialize(xmlReader);

                    if (result != null)
                    {
                        // Normalize data to match Kavita's expectations
                        NormalizeComicInfo(result);
                        Logger.Log($"Successfully parsed ComicInfo.xml from {Path.GetFileName(cbzPath)}", LogLevel.Debug, "LibraryScanner",
                            Path.GetFileName(cbzPath));
                    }
                    else
                    {
                        Logger.Log($"ComicInfo.xml was empty or invalid in {Path.GetFileName(cbzPath)}", LogLevel.Debug, "LibraryScanner");
                    }

                    return result;
                }
                catch (InvalidOperationException iex) when (iex.Message.Contains("There is an error in XML document"))
                {
                    Logger.Log($"XML format error in ComicInfo.xml for {cbzPath}: {iex.Message}", LogLevel.Warning, "LibraryScanner");
                    return AttemptFallbackParsing(cbzPath);
                }
                catch (XmlException xex)
                {
                    Logger.Log($"XML parsing error in ComicInfo.xml for {cbzPath}: {xex.Message}", LogLevel.Warning, "LibraryScanner");
                    return AttemptFallbackParsing(cbzPath);
                }
            }
            catch (InvalidDataException idex) when (idex.Message.Contains("End of Central Directory"))
            {
                Logger.Log($"Corrupted ZIP archive {cbzPath}: {idex.Message}", LogLevel.Error, "LibraryScanner");
                return null;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error extracting ComicInfo from {cbzPath}: {ex.Message}", LogLevel.Error, "LibraryScanner",
                    Path.GetFileName(cbzPath));
                return null;
            }
        }

        private string PreProcessXmlContent(string xmlContent)
        {
            // Remove any invalid characters
            xmlContent = Regex.Replace(xmlContent, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", string.Empty);
            
            // Fix common XML issues
            xmlContent = Regex.Replace(xmlContent, @"&(?!amp;|lt;|gt;|apos;|quot;)", "&amp;"); // Fix unescaped ampersands
            xmlContent = Regex.Replace(xmlContent, @"[\u0000-\u0008\u000B\u000C\u000E-\u001F]", string.Empty); // Remove control chars
            
            // Normalize line endings
            xmlContent = xmlContent.Replace("\r\n", "\n").Replace("\r", "\n");
            
            // Remove BOM if present
            if (xmlContent.StartsWith("\uFEFF"))
                xmlContent = xmlContent.Substring(1);
        
            return xmlContent;
        }

        private void NormalizeComicInfo(ComicInfo info)
        {
            // Normalize Number field
            if (!string.IsNullOrWhiteSpace(info.Number))
            {
                info.Number = info.Number.Trim().Replace(',', '.');
                // Remove leading zeros while preserving decimal numbers
                if (info.Number.Contains('.'))
                {
                    var parts = info.Number.Split('.');
                    info.Number = int.Parse(parts[0]).ToString() + "." + parts[1];
                }
                else
                {
                    info.Number = int.Parse(info.Number).ToString();
                }
            }
            
            // Normalize Series field
            if (!string.IsNullOrWhiteSpace(info.Series))
            {
                info.Series = info.Series.Trim();
            }

            // Normalize Volume field
            if (!string.IsNullOrWhiteSpace(info.Volume))
            {
                info.Volume = info.Volume.Trim();
                if (info.Volume.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                {
                    info.Volume = info.Volume.Substring(1);
                }
            }

            // Normalize Genre field
            if (!string.IsNullOrWhiteSpace(info.Genre))
            {
                // Split by commas and clean up each genre
                var genres = info.Genre.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(g => g.Trim())
                    .Where(g => !string.IsNullOrWhiteSpace(g))
                    .Distinct();
                info.Genre = string.Join(", ", genres);
            }

            // Normalize LanguageISO field
            if (!string.IsNullOrWhiteSpace(info.LanguageISO))
            {
                info.LanguageISO = info.LanguageISO.Trim().ToLowerInvariant();
            }
        }

        private ComicInfo? AttemptFallbackParsing(string cbzPath)
        {
            try
            {
                using var fileStream = File.OpenRead(cbzPath);
                using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read);
                var comicInfoEntry = archive.Entries.FirstOrDefault(e => 
                    e.FullName.Equals("comicinfo.xml", StringComparison.OrdinalIgnoreCase));

                if (comicInfoEntry == null) return null;

                using var stream = comicInfoEntry.Open();
                using var reader = new StreamReader(stream);
                var xmlContent = reader.ReadToEnd();

                // Try to extract basic information using regex patterns
                var info = new ComicInfo();
                
                // Extract Series
                var seriesMatch = Regex.Match(xmlContent, @"<Series[^>]*>(.*?)</Series>");
                if (seriesMatch.Success) info.Series = seriesMatch.Groups[1].Value.Trim();

                // Extract Number
                var numberMatch = Regex.Match(xmlContent, @"<Number[^>]*>(.*?)</Number>");
                if (numberMatch.Success) info.Number = numberMatch.Groups[1].Value.Trim();

                // Extract Volume
                var volumeMatch = Regex.Match(xmlContent, @"<Volume[^>]*>(.*?)</Volume>");
                if (volumeMatch.Success) info.Volume = volumeMatch.Groups[1].Value.Trim();

                if (!string.IsNullOrWhiteSpace(info.Series) || !string.IsNullOrWhiteSpace(info.Number))
                {
                    Logger.Log($"Successfully extracted basic info using fallback method from {Path.GetFileName(cbzPath)}", 
                        LogLevel.Info, "LibraryScanner");
                    return info;
            }

            return null;
            }
            catch (Exception ex)
            {
                Logger.Log($"Fallback parsing failed for {cbzPath}: {ex.Message}", LogLevel.Error, "LibraryScanner");
                return null;
            }
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

        // Add a public method to insert covers into all first chapters
        public async Task InsertCoversIntoFirstChaptersAsync(List<Series> seriesList)
        {
            Logger.Log($"Starting cover insertion for {seriesList.Count} series. Setting enabled: {_settingsService.InsertCoverIntoFirstChapter}", LogLevel.Info);
            
            if (!_settingsService.InsertCoverIntoFirstChapter)
            {
                Logger.Log("Cover insertion setting is disabled. Skipping process.", LogLevel.Info);
                return;
            }
            
            foreach (var series in seriesList)
            {
                try
                {
                    if (series.Chapters == null || !series.Chapters.Any())
                    {
                        Logger.Log($"No chapters found for series: {series.Title}", LogLevel.Warning);
                        continue;
                    }

                    var firstChapter = series.Chapters.OrderBy(c => c.Number).FirstOrDefault();
                    if (firstChapter == null)
                    {
                        Logger.Log($"Could not determine first chapter for series: {series.Title}", LogLevel.Warning);
                        continue;
                    }

                    string coverPath = Path.Combine(series.FolderPath, "cover.jpg");
                    if (!File.Exists(coverPath))
                    {
                        Logger.Log($"Cover image not found for series: {series.Title}", LogLevel.Warning);
                        continue;
                    }

                    await InsertCoverIntoChapterAsync(firstChapter.FilePath, coverPath);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error processing cover for series {series.Title}: {ex.Message}", LogLevel.Error);
                }
            }
        }

        /// <summary>
        /// Raises the ScanProgressChanged event
        /// </summary>
        /// <param name="e">The event arguments</param>
        protected virtual void OnScanProgressChanged(ScanProgressChangedEventArgs e)
        {
            ScanProgressChanged?.Invoke(this, e);
        }

        public void InvalidateCoverCache(string seriesPath)
        {
            _coverCache.TryRemove(seriesPath, out _);
        }

        public void ClearCoverCache()
        {
            _coverCache.Clear();
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
    }
} 