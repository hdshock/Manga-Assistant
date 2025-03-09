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

        // Events
        public event EventHandler<Series>? SeriesFound;
        public event EventHandler<ScanProgressChangedEventArgs>? ScanProgressChanged;

        // Common patterns for chapter numbers in filenames
        private static readonly Regex[] ChapterPatterns = new[]
        {
            // Match explicit chapter indicators with word boundaries to avoid matching numbers in series names
            // This pattern specifically targets "Chapter XXX" format at the beginning of the filename
            new Regex(@"^(?:Chapter|Ch\.?|Chap\.?)\s*(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase),
            
            // Match explicit chapter indicators with word boundaries
            new Regex(@"(?:^|[^\w])(?:ch(?:apter)?|ep(?:isode)?)\s*[-._]?\s*(\d+(?:\.\d+)?)(?:[^\d]|$)", RegexOptions.IgnoreCase),
            
            // Match chapter with hash notation with word boundaries
            new Regex(@"(?:^|[^\w])#\s*(\d+(?:\.\d+)?)(?:[^\d]|$)"),
            
            // Match patterns with chapter in brackets or parentheses
            new Regex(@"[\[\(]\s*(?:ch(?:apter)?|ep(?:isode)?)?\s*[-._]?\s*(\d+(?:\.\d+)?)\s*[\]\)]", RegexOptions.IgnoreCase),
            
            // Match standalone chapter numbers with clear delimiters
            new Regex(@"(?:^|[_\s\.\-])(\d+(?:\.\d+)?)(?:$|[_\s\.\-])", RegexOptions.IgnoreCase),
            
            // Match chapter at the end of filename with clear separation
            new Regex(@"[-_\s\.]+(\d+(?:\.\d+)?)(?:\.|$)"),
        };

        // Common patterns for volume numbers in filenames
        private static readonly Regex[] VolumePatterns = new[]
        {
            new Regex(@"vol(?:ume)?\.?\s*(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase),
            new Regex(@"v\.?(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase),
            new Regex(@"volume\s*(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase),
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
                        seriesList.AddRange(completedTasks.Where(s => s != null));
                        tasks.Clear();
                    }
                }

                // Wait for any remaining tasks
                if (tasks.Count > 0)
                {
                    var completedTasks = await Task.WhenAll(tasks);
                    seriesList.AddRange(completedTasks.Where(s => s != null));
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
                Logger.Log($"Scanning directory: {directoryPath}", LogLevel.Info);
                var scannedSeries = await ScanSeriesDirectoryAsync(directoryPath);
                if (scannedSeries != null)
                {
                    Logger.Log($"Found series: {scannedSeries.Title} with {scannedSeries.Chapters?.Count ?? 0} chapters", LogLevel.Info);
                    SeriesFound?.Invoke(this, scannedSeries);
                }
                else
                {
                    Logger.Log($"No series found in directory: {directoryPath}", LogLevel.Info);
                }
                
                return scannedSeries;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error scanning directory {directoryPath}: {ex.Message}", LogLevel.Error);
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
                Logger.Log($"Found {cbzFiles.Length} CBZ files in {directoryPath}", LogLevel.Info);
                
                var directoryInfo = new DirectoryInfo(directoryPath);
                var seriesName = directoryInfo.Name;

                // Try to load existing metadata first
                var existingMetadata = await LoadSeriesMetadataAsync(directoryPath);
                if (existingMetadata != null)
                {
                    Logger.Log($"Loaded existing metadata for {seriesName}", LogLevel.Info);
                }
                
                // Find cover images
                var coverImages = FindCoverImages(directoryPath);
                Logger.Log($"Found {coverImages.Length} cover images for {seriesName}", LogLevel.Info);

                // Determine the series title based on priority:
                // 1. Metadata file in the series folder
                // 2. Series title from CBZ files in the folder
                // 3. Folder name if no other titles are available
                string seriesTitle = await DetermineSeriesTitleAsync(directoryPath, cbzFiles, existingMetadata, seriesName);
                Logger.Log($"Determined series title: {seriesTitle}", LogLevel.Info);

                // Create series object with basic info
                var series = new Series
                {
                    Id = Guid.NewGuid(),
                    Title = seriesTitle,
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
                    Logger.Log($"Using cover image: {series.CoverPath}", LogLevel.Info);
                }
                else
                {
                    Logger.Log($"No cover images found for {seriesName}, using placeholder", LogLevel.Info);
                    var placeholderPath = Path.Combine(directoryPath, DEFAULT_PLACEHOLDER);
                    if (!File.Exists(placeholderPath))
                    {
                        try
                        {
                            var sourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", DEFAULT_PLACEHOLDER);
                            Logger.Log($"Copying placeholder from {sourcePath} to {placeholderPath}", LogLevel.Info);
                            File.Copy(sourcePath, placeholderPath);
                            series.CoverPath = placeholderPath;
                        }
                        catch (Exception ex)
                        {
                            Logger.Log($"Failed to create placeholder: {ex.Message}", LogLevel.Error);
                        }
                    }
                    else
                    {
                        series.CoverPath = placeholderPath;
                    }
                }

                if (!cbzFiles.Any())
                {
                    Logger.Log($"No CBZ files found for {seriesName}", LogLevel.Info);
                    series.Chapters = new List<Chapter>();
                    return series;
                }

                // Process chapters
                return await ProcessSeriesChaptersAsync(series, cbzFiles, directoryPath);
            }
            catch (Exception ex)
            {
                Logger.Log($"Error scanning directory {directoryPath}: {ex.Message}", LogLevel.Error);
                return null;
            }
        }

        // Process chapters in parallel and handle cover insertion for first chapter if enabled
        private async Task<Series?> ProcessSeriesChaptersAsync(Series series, string[] cbzFiles, string directoryPath)
        {
            try
            {
                // Process chapters in parallel
                var chapters = new ConcurrentBag<Chapter>();
                var chapterTasks = cbzFiles.Select(async cbzFile =>
                {
                    var chapter = await CreateChapterFromFileAsync(cbzFile, series.Id);
                    if (chapter != null)
                    {
                        chapters.Add(chapter);
                        Logger.Log($"Added chapter {chapter.Number} for {series.Title}", LogLevel.Info);
                    }
                });

                await Task.WhenAll(chapterTasks);
                Logger.Log($"Processed {chapters.Count} chapters for {series.Title}", LogLevel.Info);

                // Sort chapters and update series
                series.Chapters = chapters.OrderBy(c => c.Number).ToList();

                // Insert cover into first chapter if setting is enabled
                if (_settingsService.InsertCoverIntoFirstChapter && series.Chapters.Count > 0)
                {
                    Logger.Log($"Cover insertion setting is enabled. Looking for first chapter in {series.Title}", LogLevel.Info);
                    var firstChapter = series.Chapters.OrderBy(c => c.Number).FirstOrDefault();
                    if (firstChapter != null)
                    {
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
                        Logger.Log($"No chapters found for series: {series.Title}", LogLevel.Info);
                    }
                }
                else
                {
                    Logger.Log($"Cover insertion setting is disabled or no chapters found. Setting: {_settingsService.InsertCoverIntoFirstChapter}, Chapters: {series.Chapters.Count}", LogLevel.Info);
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
        private async Task<bool> InsertCoverIntoChapterAsync(string cbzFilePath, string coverImagePath)
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

                // Read the cover image into memory
                byte[] coverImageBytes = await File.ReadAllBytesAsync(coverImagePath);
                Logger.Log($"Read cover image, size: {coverImageBytes.Length} bytes", LogLevel.Info);

                try
                {
                    // Create a new archive with the cover image as the first file
                    using (var tempFileStream = File.Create(tempFilePath))
                    using (var tempArchive = new ZipArchive(tempFileStream, ZipArchiveMode.Create, true))
                    {
                        // Add cover image as the first file with name "cover.jpg" for Kavita compatibility
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
                    
                    // Clean up the temporary file if it exists
                    if (File.Exists(tempFilePath))
                    {
                        try
                        {
                            File.Delete(tempFilePath);
                            Logger.Log($"Deleted temporary file after error: {tempFilePath}", LogLevel.Info);
                        }
                        catch (Exception cleanupEx)
                        {
                            Logger.Log($"Error cleaning up temporary file: {cleanupEx.Message}", LogLevel.Error);
                        }
                    }
                    
                    throw; // Re-throw to be caught by the outer try/catch
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
            var fileInfo = new FileInfo(filePath);
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            
            // First create the chapter with basic information
            var chapter = CreateChapterFromFileName(filePath, seriesId);
            
            if (chapter == null)
            {
                return null;
            }

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
                Logger.Log($"Error scanning chapter: {ex.Message}", LogLevel.Error);
                Logger.Log($"Stack trace: {ex.StackTrace}", LogLevel.Error);
                return null;
            }
        }

        private Chapter? CreateChapterFromFileName(string filePath, Guid seriesId)
        {
            var fileInfo = new FileInfo(filePath);
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            
            // Remove file extension and series name from consideration if possible
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            
            Logger.Log($"Extracting chapter number from: {fileNameWithoutExtension}", LogLevel.Info);
            
            // Get the directory name to help with context
            string directoryName = Path.GetFileName(Path.GetDirectoryName(filePath) ?? string.Empty);
            Logger.Log($"Series directory: {directoryName}", LogLevel.Info);
            
            // Special case for Guyver manga - files are named like "Chapter XXX [Title].cbz"
            if (directoryName.Contains("Guyver", StringComparison.OrdinalIgnoreCase))
            {
                var guyverMatch = Regex.Match(fileNameWithoutExtension, @"^Chapter\s*(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
                if (guyverMatch.Success && double.TryParse(guyverMatch.Groups[1].Value, out double guyverNumber))
                {
                    Logger.Log($"Found Guyver chapter number: {guyverNumber}", LogLevel.Info);
                    return new Chapter
                    {
                        Id = Guid.NewGuid(),
                        SeriesId = seriesId,
                        FilePath = filePath,
                        Added = fileInfo.CreationTime,
                        LastRead = DateTime.MinValue,
                        Number = guyverNumber,
                        Volume = ExtractVolumeNumberFromFileName(fileName),
                        Title = fileName,
                        PageCount = 0
                    };
                }
            }
            
            // Try to extract chapter number from ComicInfo.xml first (if available)
            // This would be implemented separately
            
            // Try to match using chapter patterns
            foreach (var pattern in ChapterPatterns)
            {
                var match = pattern.Match(fileNameWithoutExtension);
                if (match.Success && double.TryParse(match.Groups[1].Value, out double number))
                {
                    Logger.Log($"Found chapter number {number} using pattern: {pattern}", LogLevel.Info);
                    return new Chapter
                    {
                        Id = Guid.NewGuid(),
                        SeriesId = seriesId,
                        FilePath = filePath,
                        Added = fileInfo.CreationTime,
                        LastRead = DateTime.MinValue,
                        Number = number,
                        Volume = ExtractVolumeNumberFromFileName(fileName),
                        Title = fileName,
                        PageCount = 0
                    };
                }
            }
            
            // Special case for filenames that are just numbers (e.g., "01.cbz", "1.cbz")
            if (Regex.IsMatch(fileNameWithoutExtension, @"^\d+(?:\.\d+)?$") && 
                double.TryParse(fileNameWithoutExtension, out double simpleNumber))
            {
                Logger.Log($"Found simple chapter number: {simpleNumber}", LogLevel.Info);
                return new Chapter
                {
                    Id = Guid.NewGuid(),
                    SeriesId = seriesId,
                    FilePath = filePath,
                    Added = fileInfo.CreationTime,
                    LastRead = DateTime.MinValue,
                    Number = simpleNumber,
                    Volume = ExtractVolumeNumberFromFileName(fileName),
                    Title = fileName,
                    PageCount = 0
                };
            }
            
            // Handle special case for series names with numbers
            // First, try to find a clear chapter indicator after the series name
            var seriesNameWithNumbersPattern = new Regex(@"(.+?)(?:ch(?:apter)?|ep(?:isode)?)?[\s._-]+(\d+(?:\.\d+)?)(?:[^\d]|$)", RegexOptions.IgnoreCase);
            var seriesMatch = seriesNameWithNumbersPattern.Match(fileNameWithoutExtension);
            if (seriesMatch.Success && seriesMatch.Groups.Count >= 3 && double.TryParse(seriesMatch.Groups[2].Value, out double seriesNumber))
            {
                string potentialSeriesName = seriesMatch.Groups[1].Value.Trim();
                // Check if the potential series name is similar to the directory name
                if (directoryName.Contains(potentialSeriesName, StringComparison.OrdinalIgnoreCase) || 
                    potentialSeriesName.Contains(directoryName, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Log($"Found chapter number {seriesNumber} after series name: {potentialSeriesName}", LogLevel.Info);
                    return new Chapter
                    {
                        Id = Guid.NewGuid(),
                        SeriesId = seriesId,
                        FilePath = filePath,
                        Added = fileInfo.CreationTime,
                        LastRead = DateTime.MinValue,
                        Number = seriesNumber,
                        Volume = ExtractVolumeNumberFromFileName(fileName),
                        Title = fileName,
                        PageCount = 0
                    };
                }
            }
            
            // Handle special case for DNA^2 series where filenames might contain numbers
            // but they're part of the series name, not chapter numbers
            if (fileNameWithoutExtension.Contains("DNA") || 
                fileNameWithoutExtension.Contains("Dna") || 
                fileNameWithoutExtension.Contains("dna"))
            {
                // Try to find chapter number after the series name
                var dnaMatch = Regex.Match(fileNameWithoutExtension, @"DNA.*?(?:ch(?:apter)?|ep(?:isode)?)?[\s._-]*(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
                if (dnaMatch.Success && double.TryParse(dnaMatch.Groups[1].Value, out double dnaNumber))
                {
                    Logger.Log($"Found DNA series chapter number: {dnaNumber}", LogLevel.Info);
                    return new Chapter
                    {
                        Id = Guid.NewGuid(),
                        SeriesId = seriesId,
                        FilePath = filePath,
                        Added = fileInfo.CreationTime,
                        LastRead = DateTime.MinValue,
                        Number = dnaNumber,
                        Volume = ExtractVolumeNumberFromFileName(fileName),
                        Title = fileName,
                        PageCount = 0
                    };
                }
            }
            
            // Handle special case for "Kill The Hero" series
            if (fileNameWithoutExtension.Contains("Kill") && 
                fileNameWithoutExtension.Contains("Hero"))
            {
                // Try to find chapter number after the series name
                var killHeroMatch = Regex.Match(fileNameWithoutExtension, @"Kill.*?Hero.*?(?:ch(?:apter)?|ep(?:isode)?)?[\s._-]*(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
                if (killHeroMatch.Success && double.TryParse(killHeroMatch.Groups[1].Value, out double killHeroNumber))
                {
                    Logger.Log($"Found Kill The Hero series chapter number: {killHeroNumber}", LogLevel.Info);
                    return new Chapter
                    {
                        Id = Guid.NewGuid(),
                        SeriesId = seriesId,
                        FilePath = filePath,
                        Added = fileInfo.CreationTime,
                        LastRead = DateTime.MinValue,
                        Number = killHeroNumber,
                        Volume = ExtractVolumeNumberFromFileName(fileName),
                        Title = fileName,
                        PageCount = 0
                    };
                }
            }
            
            // Handle special case for "Bio Booster Armor Guyver" series
            if (fileNameWithoutExtension.Contains("Bio") || 
                fileNameWithoutExtension.Contains("Booster") || 
                fileNameWithoutExtension.Contains("Armor") ||
                fileNameWithoutExtension.Contains("Guyver"))
            {
                // Try to find chapter number after the series name
                var guyverMatch = Regex.Match(fileNameWithoutExtension, @"(?:Bio.*?(?:Booster|Armor|Guyver)|Guyver).*?(?:ch(?:apter)?|ep(?:isode)?)?[\s._-]*(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
                if (guyverMatch.Success && double.TryParse(guyverMatch.Groups[1].Value, out double guyverNumber))
                {
                    Logger.Log($"Found Bio Booster Armor Guyver series chapter number: {guyverNumber}", LogLevel.Info);
                    return new Chapter
                    {
                        Id = Guid.NewGuid(),
                        SeriesId = seriesId,
                        FilePath = filePath,
                        Added = fileInfo.CreationTime,
                        LastRead = DateTime.MinValue,
                        Number = guyverNumber,
                        Volume = ExtractVolumeNumberFromFileName(fileName),
                        Title = fileName,
                        PageCount = 0
                    };
                }
            }
            
            // If we still can't determine the chapter number, look for any number in the filename
            // that might represent a chapter, but be more cautious
            var lastNumberMatch = Regex.Match(fileNameWithoutExtension, @"[-_\s\.]+(\d+(?:\.\d+)?)(?:\.|$)");
            if (lastNumberMatch.Success && double.TryParse(lastNumberMatch.Groups[1].Value, out double lastNumber))
            {
                Logger.Log($"Using last number in filename as chapter: {lastNumber}", LogLevel.Info);
                return new Chapter
                {
                    Id = Guid.NewGuid(),
                    SeriesId = seriesId,
                    FilePath = filePath,
                    Added = fileInfo.CreationTime,
                    LastRead = DateTime.MinValue,
                    Number = lastNumber,
                    Volume = ExtractVolumeNumberFromFileName(fileName),
                    Title = fileName,
                    PageCount = 0
                };
            }
            
            Logger.Log("No chapter number found, defaulting to 1", LogLevel.Info);
            return new Chapter
            {
                Id = Guid.NewGuid(),
                SeriesId = seriesId,
                FilePath = filePath,
                Added = fileInfo.CreationTime,
                LastRead = DateTime.MinValue,
                Number = 1,
                Volume = ExtractVolumeNumberFromFileName(fileName),
                Title = fileName,
                PageCount = 0
            };
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
                return null;

            try
            {
                var json = await File.ReadAllTextAsync(metadataPath);
                return JsonSerializer.Deserialize<SeriesMetadata>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                Logger.Log($"Error loading metadata: {ex.Message}", LogLevel.Error);
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

                Logger.Log($"Looking for ComicInfo.xml in {cbzPath}", LogLevel.Info);
                Logger.Log($"ComicInfo.xml found: {comicInfoEntry != null}", LogLevel.Info);

                if (comicInfoEntry != null)
                {
                    using var stream = comicInfoEntry.Open();
                    using var reader = new StreamReader(stream);
                    var xmlContent = await reader.ReadToEndAsync();
                    Logger.Log($"ComicInfo.xml content: {xmlContent}", LogLevel.Info);

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
                    Logger.Log($"ComicInfo deserialized: {result != null}", LogLevel.Info);
                    if (result != null)
                    {
                        Logger.Log($"Series: {result.Series}, Title: {result.Title}, Number: {result.Number}, PageCount: {result.PageCount}", LogLevel.Info);
                    }
                    return result;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error extracting ComicInfo: {ex.Message}", LogLevel.Error);
                Logger.Log($"Stack trace: {ex.StackTrace}", LogLevel.Error);
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

        // Add a public method to insert covers into all first chapters
        public async Task InsertCoversIntoFirstChaptersAsync(List<Series> seriesList)
        {
            Logger.Log($"Starting cover insertion for {seriesList.Count} series. Setting enabled: {_settingsService.InsertCoverIntoFirstChapter}", LogLevel.Info);
            
            if (!_settingsService.InsertCoverIntoFirstChapter)
            {
                Logger.Log("Cover insertion setting is disabled. Skipping process.", LogLevel.Info);
                return;
            }
            
            // Use our CoverInsertionUtility to process all covers
            await Utilities.CoverInsertionUtility.ProcessAllSeriesCoversAsync(seriesList);
        }

        /// <summary>
        /// Raises the ScanProgressChanged event
        /// </summary>
        /// <param name="e">The event arguments</param>
        protected virtual void OnScanProgressChanged(ScanProgressChangedEventArgs e)
        {
            ScanProgressChanged?.Invoke(this, e);
        }
    }
} 