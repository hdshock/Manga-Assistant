using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Windows;
using MangaAssistant.Core.Models;
using System.Linq;
using System.Collections.Generic;

namespace MangaAssistant.Infrastructure.Utilities
{
    public class CoverInsertionUtility
    {
        /// <summary>
        /// Processes all series in the library and inserts cover images into the first chapter's CBZ file
        /// </summary>
        public static async Task ProcessAllSeriesCoversAsync(List<Series> allSeries)
        {
            if (allSeries == null || !allSeries.Any())
            {
                Debug.WriteLine("No series found to process covers");
                return;
            }

            int successCount = 0;
            int errorCount = 0;
            int skippedCount = 0;

            foreach (var series in allSeries)
            {
                try
                {
                    Debug.WriteLine($"Processing covers for series: {series.Title}");
                    
                    // Skip if no chapters
                    if (series.Chapters == null || !series.Chapters.Any())
                    {
                        Debug.WriteLine($"Series has no chapters: {series.Title}");
                        skippedCount++;
                        continue;
                    }

                    // Get the first chapter
                    var firstChapter = series.Chapters.OrderBy(c => c.Number).FirstOrDefault();
                    if (firstChapter == null)
                    {
                        Debug.WriteLine($"Could not determine first chapter for series: {series.Title}");
                        skippedCount++;
                        continue;
                    }

                    // Check if cover image exists
                    string coverPath = Path.Combine(series.FolderPath, "cover.jpg");
                    if (!File.Exists(coverPath))
                    {
                        Debug.WriteLine($"Cover image not found for series: {series.Title}");
                        skippedCount++;
                        continue;
                    }

                    // Insert cover into first chapter
                    bool success = await InsertCoverIntoChapterAsync(firstChapter.FilePath, coverPath);
                    if (success)
                    {
                        successCount++;
                        Debug.WriteLine($"Successfully processed cover for series: {series.Title}");
                    }
                    else
                    {
                        errorCount++;
                        Debug.WriteLine($"Failed to process cover for series: {series.Title}");
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    Debug.WriteLine($"Error processing cover for series {series.Title}: {ex.Message}");
                }
            }

            // Log summary
            Debug.WriteLine($"Cover processing complete. Success: {successCount}, Errors: {errorCount}, Skipped: {skippedCount}");
        }

        /// <summary>
        /// Inserts a cover image into a CBZ file as the first entry
        /// </summary>
        /// <param name="cbzFilePath">Path to the CBZ file</param>
        /// <param name="coverImagePath">Path to the cover image</param>
        /// <returns>True if successful, false otherwise</returns>
        public static async Task<bool> InsertCoverIntoChapterAsync(string cbzFilePath, string coverImagePath)
        {
            string tempFilePath = null;
            
            try
            {
                Debug.WriteLine($"Starting cover insertion for: {cbzFilePath}");
                
                if (!File.Exists(cbzFilePath))
                {
                    Debug.WriteLine($"CBZ file does not exist: {cbzFilePath}");
                    return false;
                }
                
                if (!File.Exists(coverImagePath))
                {
                    Debug.WriteLine($"Cover image does not exist: {coverImagePath}");
                    return false;
                }
                
                // Create a temporary file for the new archive
                tempFilePath = Path.Combine(
                    Path.GetDirectoryName(cbzFilePath) ?? string.Empty,
                    $"temp_{Path.GetFileName(cbzFilePath)}"
                );
                
                Debug.WriteLine($"Created temporary file at: {tempFilePath}");

                // Read the cover image into memory
                byte[] coverImageBytes = await File.ReadAllBytesAsync(coverImagePath);
                Debug.WriteLine($"Read cover image, size: {coverImageBytes.Length} bytes");

                // Create a new archive with the cover image as the first file
                using (var tempFileStream = File.Create(tempFilePath))
                using (var tempArchive = new ZipArchive(tempFileStream, ZipArchiveMode.Create, true))
                {
                    // Add cover image as the first file with name "cover.jpg" for Kavita compatibility
                    var coverEntry = tempArchive.CreateEntry("cover.jpg");
                    using (var entryStream = coverEntry.Open())
                    {
                        await entryStream.WriteAsync(coverImageBytes, 0, coverImageBytes.Length);
                        Debug.WriteLine("Added cover image to temporary archive");
                    }

                    // Copy all entries from the original archive
                    Debug.WriteLine($"Opening original archive: {cbzFilePath}");
                    using (var originalFileStream = File.OpenRead(cbzFilePath))
                    using (var originalArchive = new ZipArchive(originalFileStream, ZipArchiveMode.Read))
                    {
                        Debug.WriteLine($"Original archive has {originalArchive.Entries.Count} entries");
                        foreach (var entry in originalArchive.Entries)
                        {
                            // Skip if it's already a cover image with the same name
                            if (entry.Name.Equals("cover.jpg", StringComparison.OrdinalIgnoreCase))
                            {
                                Debug.WriteLine($"Skipping existing cover image: {entry.Name}");
                                continue;
                            }

                            // Copy the entry to the new archive
                            Debug.WriteLine($"Copying entry: {entry.Name}");
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
                Debug.WriteLine($"Deleting original file: {cbzFilePath}");
                File.Delete(cbzFilePath);
                Debug.WriteLine($"Moving temporary file to original location");
                File.Move(tempFilePath, cbzFilePath);

                Debug.WriteLine($"Successfully inserted cover image into {cbzFilePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during cover insertion: {ex.Message}");
                
                // Clean up the temporary file if it exists
                if (tempFilePath != null && File.Exists(tempFilePath))
                {
                    try
                    {
                        File.Delete(tempFilePath);
                        Debug.WriteLine($"Deleted temporary file after error: {tempFilePath}");
                    }
                    catch (Exception cleanupEx)
                    {
                        Debug.WriteLine($"Error cleaning up temporary file: {cleanupEx.Message}");
                    }
                }
                
                return false;
            }
        }
    }
}
