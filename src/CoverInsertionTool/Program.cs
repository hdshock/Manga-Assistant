using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MangaAssistant.Core.Models;
using MangaAssistant.Infrastructure.Utilities;

namespace CoverInsertionTool
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Manga Assistant - Cover Insertion Tool");
            Console.WriteLine("======================================");
            
            string libraryPath = string.Empty;
            
            // Get library path from command line or prompt user
            if (args.Length > 0)
            {
                libraryPath = args[0];
            }
            else
            {
                Console.Write("Enter the path to your manga library: ");
                libraryPath = Console.ReadLine();
            }
            
            if (string.IsNullOrWhiteSpace(libraryPath) || !Directory.Exists(libraryPath))
            {
                Console.WriteLine($"Error: The specified library path does not exist: {libraryPath}");
                return;
            }
            
            Console.WriteLine($"Scanning library at: {libraryPath}");
            
            try
            {
                // Scan the library to find all series
                var allSeries = await ScanLibraryAsync(libraryPath);
                
                if (allSeries.Count == 0)
                {
                    Console.WriteLine("No series found in the library.");
                    return;
                }
                
                Console.WriteLine($"Found {allSeries.Count} series in the library.");
                
                // Process covers for all series
                Console.WriteLine("Processing covers...");
                await CoverInsertionUtility.ProcessAllSeriesCoversAsync(allSeries);
                
                Console.WriteLine("Cover processing complete!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Debug.WriteLine($"Error details: {ex}");
            }
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
        
        /// <summary>
        /// Scans the library directory to find all series and chapters
        /// </summary>
        private static async Task<List<Series>> ScanLibraryAsync(string libraryPath)
        {
            var allSeries = new List<Series>();
            
            // Get all directories in the library (each directory is a series)
            var seriesDirs = Directory.GetDirectories(libraryPath);
            
            foreach (var seriesDir in seriesDirs)
            {
                try
                {
                    var seriesName = Path.GetFileName(seriesDir);
                    Console.WriteLine($"Scanning series: {seriesName}");
                    
                    var series = new Series
                    {
                        Title = seriesName,
                        FolderPath = seriesDir,
                        Chapters = new List<Chapter>()
                    };
                    
                    // Look for CBZ files in the series directory
                    var cbzFiles = Directory.GetFiles(seriesDir, "*.cbz", SearchOption.TopDirectoryOnly);
                    
                    // Also check for CBZ files in subdirectories (for series with volume/chapter folders)
                    var subdirs = Directory.GetDirectories(seriesDir);
                    foreach (var subdir in subdirs)
                    {
                        var subCbzFiles = Directory.GetFiles(subdir, "*.cbz", SearchOption.TopDirectoryOnly);
                        cbzFiles = cbzFiles.Concat(subCbzFiles).ToArray();
                    }
                    
                    // Create Chapter objects for each CBZ file
                    foreach (var cbzFile in cbzFiles)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(cbzFile);
                        
                        // Try to extract chapter number from filename
                        double chapterNumber = 0;
                        if (TryParseChapterNumber(fileName, out chapterNumber))
                        {
                            var chapter = new Chapter
                            {
                                Title = fileName,
                                Number = chapterNumber,
                                FilePath = cbzFile
                            };
                            
                            series.Chapters.Add(chapter);
                        }
                    }
                    
                    if (series.Chapters.Count > 0)
                    {
                        series.ChapterCount = series.Chapters.Count;
                        allSeries.Add(series);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error scanning series {Path.GetFileName(seriesDir)}: {ex.Message}");
                    Debug.WriteLine($"Error details: {ex}");
                }
            }
            
            return allSeries;
        }
        
        /// <summary>
        /// Tries to extract a chapter number from a filename
        /// </summary>
        private static bool TryParseChapterNumber(string fileName, out double chapterNumber)
        {
            chapterNumber = 0;
            
            // Common patterns for chapter numbers in filenames
            var patterns = new[]
            {
                @"ch(?:apter)?\.?\s*(\d+(?:\.\d+)?)",
                @"ch(?:apter)?\.?(\d+(?:\.\d+)?)",
                @"c\.?(\d+(?:\.\d+)?)",
                @"#(\d+(?:\.\d+)?)",
                @"(\d+(?:\.\d+)?)"
            };
            
            foreach (var pattern in patterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(fileName, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success && match.Groups.Count > 1)
                {
                    if (double.TryParse(match.Groups[1].Value, out chapterNumber))
                    {
                        return true;
                    }
                }
            }
            
            // If no pattern matched, just use the first number found in the filename
            var numberMatch = System.Text.RegularExpressions.Regex.Match(fileName, @"(\d+(?:\.\d+)?)");
            if (numberMatch.Success)
            {
                if (double.TryParse(numberMatch.Value, out chapterNumber))
                {
                    return true;
                }
            }
            
            return false;
        }
    }
}
