using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using MangaAssistant.Core.Models;
using System.Linq;
using System.Collections.Generic;
using MangaAssistant.Common.Logging;

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
                Logger.Log("No series found to process covers", LogLevel.Warning);
                return;
            }

            int successCount = 0;
            int errorCount = 0;
            int skippedCount = 0;

            foreach (var series in allSeries)
            {
                try
                {
                    Logger.Log($"Processing covers for series: {series.Title}", LogLevel.Info);
                    
                    // Skip if no chapters
                    if (series.Chapters == null || !series.Chapters.Any())
                    {
                        Logger.Log($"Series has no chapters: {series.Title}", LogLevel.Warning);
                        skippedCount++;
                        continue;
                    }

                    // Get the first chapter
                    var firstChapter = series.Chapters.OrderBy(c => c.Number).FirstOrDefault();
                    if (firstChapter == null)
                    {
                        Logger.Log($"Could not determine first chapter for series: {series.Title}", LogLevel.Warning);
                        skippedCount++;
                        continue;
                    }

                    // Check if cover image exists
                    string coverPath = Path.Combine(series.FolderPath, "cover.jpg");
                    if (!File.Exists(coverPath))
                    {
                        Logger.Log($"Cover image not found for series: {series.Title}", LogLevel.Warning);
                        skippedCount++;
                        continue;
                    }

                    // Insert cover into first chapter
                    bool success = await InsertCoverIntoChapterAsync(firstChapter.FilePath, coverPath);
                    if (success)
                    {
                        successCount++;
                        Logger.Log($"Successfully processed cover for series: {series.Title}", LogLevel.Info);
                    }
                    else
                    {
                        errorCount++;
                        Logger.Log($"Failed to process cover for series: {series.Title}", LogLevel.Error);
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    Logger.Log($"Error processing cover for series {series.Title}: {ex.Message}", LogLevel.Error);
                }
            }

            // Log summary
            Logger.Log($"Cover processing complete. Success: {successCount}, Errors: {errorCount}, Skipped: {skippedCount}", LogLevel.Info);
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
                Logger.Log($"Starting cover insertion for: {cbzFilePath}", LogLevel.Info);
                
                if (!File.Exists(cbzFilePath))
                {
                    Logger.Log($"CBZ file does not exist: {cbzFilePath}", LogLevel.Error);
                    return false;
                }
                
                if (!File.Exists(coverImagePath))
                {
                    Logger.Log($"Cover image does not exist: {coverImagePath}", LogLevel.Error);
                    return false;
                }
                
                // Create a temporary file for the new archive
                tempFilePath = Path.Combine(
                    Path.GetDirectoryName(cbzFilePath) ?? string.Empty,
                    $"temp_{Path.GetFileName(cbzFilePath)}"
                );
                
                Logger.Log($"Created temporary file at: {tempFilePath}", LogLevel.Debug);

                // Read the cover image into memory
                byte[] coverImageBytes = await File.ReadAllBytesAsync(coverImagePath);
                Logger.Log($"Read cover image, size: {coverImageBytes.Length} bytes", LogLevel.Debug);

                // Create a new archive with the cover image as the first file
                using (var tempFileStream = File.Create(tempFilePath))
                using (var tempArchive = new ZipArchive(tempFileStream, ZipArchiveMode.Create, true))
                {
                    // Add cover image as the first file
                    var coverEntry = tempArchive.CreateEntry("cover.jpg");
                    using (var entryStream = coverEntry.Open())
                    {
                        await entryStream.WriteAsync(coverImageBytes, 0, coverImageBytes.Length);
                        Logger.Log("Added cover image to temporary archive", LogLevel.Debug);
                    }

                    // Copy all entries from the original archive
                    Logger.Log($"Opening original archive: {cbzFilePath}", LogLevel.Debug);
                    using (var originalFileStream = File.OpenRead(cbzFilePath))
                    using (var originalArchive = new ZipArchive(originalFileStream, ZipArchiveMode.Read))
                    {
                        Logger.Log($"Original archive has {originalArchive.Entries.Count} entries", LogLevel.Debug);
                        foreach (var entry in originalArchive.Entries)
                        {
                            // Skip if it's already a cover image with the same name
                            if (entry.Name.Equals("cover.jpg", StringComparison.OrdinalIgnoreCase))
                            {
                                Logger.Log($"Skipping existing cover image: {entry.Name}", LogLevel.Debug);
                                continue;
                            }

                            // Copy the entry to the new archive
                            Logger.Log($"Copying entry: {entry.Name}", LogLevel.Debug);
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
                Logger.Log($"Replacing original file: {cbzFilePath}", LogLevel.Debug);
                File.Delete(cbzFilePath);
                File.Move(tempFilePath, cbzFilePath);

                Logger.Log($"Successfully inserted cover image into {cbzFilePath}", LogLevel.Info);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error during cover insertion: {ex.Message}", LogLevel.Error);
                
                // Clean up the temporary file if it exists
                if (tempFilePath != null && File.Exists(tempFilePath))
                {
                    try
                    {
                        File.Delete(tempFilePath);
                        Logger.Log($"Deleted temporary file after error: {tempFilePath}", LogLevel.Debug);
                    }
                    catch (Exception cleanupEx)
                    {
                        Logger.Log($"Error cleaning up temporary file: {cleanupEx.Message}", LogLevel.Error);
                    }
                }
                
                return false;
            }
        }
    }
}
