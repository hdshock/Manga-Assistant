using System;
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
using System.Diagnostics;

namespace MangaAssistant.Infrastructure.Services
{
    public class LibraryScanner : ILibraryService
    {
        private const string SERIES_METADATA_FILE = "series-info.json";
        private const string DEFAULT_PLACEHOLDER = "folder-placeholder.jpg";
        private static readonly string[] COVER_PATTERNS = new[] 
        {
            "cover.jpg", "cover.jpeg", "cover.png",
            "Cover.jpg", "Cover.jpeg", "Cover.png",
            "COVER.jpg", "COVER.jpeg", "COVER.png"
        };

        private readonly XmlSerializer _comicInfoSerializer;
        private readonly ISettingsService _settingsService;
        private readonly JsonSerializerOptions _jsonOptions;
        private List<Series> _series = new();

        // Common patterns for chapter numbers in filenames
        private static readonly Regex[] ChapterPatterns = new[]
        {
            new Regex(@"ch[a]?[p]?[ter]?\.?\s*(\d+)", RegexOptions.IgnoreCase),    // Matches: ch.1, chp.1, chap.1, chapter.1, etc.
            new Regex(@"#(\d+)"),                                                    // Matches: #1
            new Regex(@"[\[\(](\d+)[\]\)]"),                                        // Matches: [1], (1)
            new Regex(@"[-_](\d+)(?:\.|$)"),                                        // Matches: -1, _1 at end or before extension
            new Regex(@"(\d+)(?:\.|$)"),                                            // Last resort: just look for numbers at end
        };

        public event EventHandler<LibraryUpdatedEventArgs>? LibraryUpdated;
        public List<Series> Series => _series;

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

        public async Task ScanLibraryAsync()
        {
            var libraryPath = _settingsService.LibraryPath;
            if (string.IsNullOrWhiteSpace(libraryPath))
            {
                return;
            }

            var scannedSeries = await ScanLibraryPathAsync(libraryPath);
            _series = scannedSeries;
            LibraryUpdated?.Invoke(this, new LibraryUpdatedEventArgs(scannedSeries));
        }

        private async Task<List<Series>> ScanLibraryPathAsync(string libraryPath)
        {
            var series = new List<Series>();
            var directories = Directory.GetDirectories(libraryPath);

            foreach (var directory in directories)
            {
                var seriesInfo = await ScanSeriesDirectoryAsync(directory);
                if (seriesInfo != null)
                {
                    series.Add(seriesInfo);
                    await SaveSeriesMetadataAsync(seriesInfo);
                }
            }

            return series;
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
            var cbzFiles = Directory.GetFiles(directoryPath, "*.cbz", SearchOption.TopDirectoryOnly);
            var directoryInfo = new DirectoryInfo(directoryPath);
            var seriesName = directoryInfo.Name;
            var hasMetadata = false;
            var totalChapters = 0;
            ComicInfo? firstChapterInfo = null;

            // Try to load existing metadata first
            var existingMetadata = await LoadSeriesMetadataAsync(directoryPath);
            
            // Find cover images in the series directory
            var coverImages = FindCoverImages(directoryPath);

            // Create default placeholder if no covers exist
            var placeholderPath = Path.Combine(directoryPath, DEFAULT_PLACEHOLDER);
            if (coverImages.Length == 0 && !File.Exists(placeholderPath))
            {
                try
                {
                    File.Copy(
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", DEFAULT_PLACEHOLDER),
                        placeholderPath);
                    coverImages = new[] { placeholderPath };
                }
                catch
                {
                    // If copying fails, we'll handle this later
                }
            }
            
            // Create series even if directory is empty
            var series = new Series
            {
                Id = Guid.NewGuid(),
                Title = existingMetadata?.Series ?? seriesName,
                FolderPath = directoryPath,
                Created = existingMetadata?.Created ?? directoryInfo.CreationTime,
                LastModified = directoryInfo.LastWriteTime,
                Metadata = existingMetadata ?? new SeriesMetadata()
            };

            // Set cover image path
            if (coverImages.Length > 0)
            {
                // If we have existing metadata with a valid selected cover index, use it
                var selectedIndex = existingMetadata?.SelectedCoverIndex ?? 0;
                if (selectedIndex >= 0 && selectedIndex < coverImages.Length)
                {
                    series.CoverPath = coverImages[selectedIndex];
                }
                else
                {
                    series.CoverPath = coverImages[0];
                }
            }
            else if (File.Exists(placeholderPath))
            {
                series.CoverPath = placeholderPath;
            }

            // If no CBZ files, return series with empty chapters
            if (!cbzFiles.Any())
            {
                series.Chapters = new List<Chapter>();
                return series;
            }

            var chapters = new List<Chapter>();
            foreach (var cbzFile in cbzFiles)
            {
                var chapter = await ScanChapterFileAsync(cbzFile, series.Id);
                if (chapter != null)
                {
                    chapters.Add(chapter);
                    
                    // Try to get series metadata from first chapter if not already found
                    if (!hasMetadata)
                    {
                        firstChapterInfo = await ExtractComicInfoAsync(cbzFile);
                        if (firstChapterInfo?.Series != null)
                        {
                            series.Title = firstChapterInfo.Series;
                            hasMetadata = true;
                            series.Metadata.HasMetadata = true;

                            // Try to get total chapters from metadata
                            if (firstChapterInfo.Count != null && int.TryParse(firstChapterInfo.Count, out int count))
                            {
                                totalChapters = count;
                            }
                        }
                    }
                }
            }

            // Sort chapters by number
            chapters = chapters.OrderBy(c => c.Number).ToList();
            series.Chapters = chapters;
            
            // If no cover image was found in the directory, use the first chapter as fallback
            if (string.IsNullOrEmpty(series.CoverPath) && chapters.Any())
            {
                series.CoverPath = chapters.First().FilePath;
            }

            return series;
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

                // Update the series in memory
                var series = _series.FirstOrDefault(s => s.FolderPath == seriesPath);
                if (series != null)
                {
                    series.CoverPath = coverPath;
                    LibraryUpdated?.Invoke(this, new LibraryUpdatedEventArgs(_series));
                }
            }
        }

        private async Task<Chapter?> ScanChapterFileAsync(string filePath, Guid seriesId)
        {
            System.Diagnostics.Debug.WriteLine($"Scanning chapter file: {filePath}");
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
                Title = fileName,
                PageCount = 0
            };

            try
            {
                using var fileStream = File.OpenRead(filePath);
                using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read);

                // Count image files first
                chapter.PageCount = archive.Entries.Count(e => 
                    e.Name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                    e.Name.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                    e.Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase));
                System.Diagnostics.Debug.WriteLine($"Initial page count from archive: {chapter.PageCount}");
                
                // Try to extract chapter info from comicinfo.xml
                var comicInfo = await ExtractComicInfoAsync(filePath);
                System.Diagnostics.Debug.WriteLine($"Comic info extracted: {comicInfo != null}");
                if (comicInfo != null)
                {
                    // Only update title if we have one from metadata
                    if (!string.IsNullOrWhiteSpace(comicInfo.Title))
                    {
                        chapter.Title = comicInfo.Title;
                    }

                    // Only update page count if metadata has a non-zero value
                    if (comicInfo.PageCount > 0)
                    {
                        chapter.PageCount = comicInfo.PageCount;
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"Comic info - Title: {comicInfo.Title}, Pages: {comicInfo.PageCount}");
                    
                    // Try to get chapter number from metadata
                    if (!string.IsNullOrWhiteSpace(comicInfo.Number) && 
                        int.TryParse(comicInfo.Number, out int number))
                    {
                        chapter.Number = number;
                        System.Diagnostics.Debug.WriteLine($"Chapter number from metadata: {number}");
                    }
                    
                    if (!string.IsNullOrWhiteSpace(comicInfo.Volume) && 
                        int.TryParse(comicInfo.Volume, out int volume))
                    {
                        chapter.Volume = volume;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Final chapter info - Number: {chapter.Number}, Title: {chapter.Title}, Pages: {chapter.PageCount}");
                return chapter;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error scanning chapter: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return null;
            }
        }

        private int ExtractChapterNumberFromFileName(string fileName)
        {
            foreach (var pattern in ChapterPatterns)
            {
                var match = pattern.Match(fileName);
                if (match.Success && int.TryParse(match.Groups[1].Value, out int number))
                {
                    return number;
                }
            }
            return 1; // Default to chapter 1 if no number found
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
    }
} 