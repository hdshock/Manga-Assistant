using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Diagnostics;

namespace CoverInsertionTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Cover Insertion Test Utility");
            Console.WriteLine("============================");
            
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: CoverInsertionTest <seriesPath> <cbzFilePath>");
                Console.WriteLine("Example: CoverInsertionTest \"C:\\Manga\\OnePiece\" \"C:\\Manga\\OnePiece\\Chapter1.cbz\"");
                return;
            }
            
            string seriesPath = args[0];
            string cbzFilePath = args[1];
            
            if (!Directory.Exists(seriesPath))
            {
                Console.WriteLine($"Error: Series directory does not exist: {seriesPath}");
                return;
            }
            
            if (!File.Exists(cbzFilePath))
            {
                Console.WriteLine($"Error: CBZ file does not exist: {cbzFilePath}");
                return;
            }
            
            string coverPath = Path.Combine(seriesPath, "cover.jpg");
            if (!File.Exists(coverPath))
            {
                Console.WriteLine($"Error: Cover image not found at: {coverPath}");
                return;
            }
            
            Console.WriteLine($"Series Path: {seriesPath}");
            Console.WriteLine($"CBZ File: {cbzFilePath}");
            Console.WriteLine($"Cover Image: {coverPath}");
            Console.WriteLine();
            
            Console.WriteLine("Starting cover insertion process...");
            bool success = await InsertCoverIntoChapterAsync(cbzFilePath, coverPath);
            
            if (success)
            {
                Console.WriteLine("Cover insertion completed successfully!");
            }
            else
            {
                Console.WriteLine("Cover insertion failed. Check the logs for details.");
            }
        }
        
        private static async Task<bool> InsertCoverIntoChapterAsync(string cbzFilePath, string coverImagePath)
        {
            try
            {
                Console.WriteLine($"Starting cover insertion for: {cbzFilePath}");
                
                if (!File.Exists(cbzFilePath))
                {
                    Console.WriteLine($"CBZ file does not exist: {cbzFilePath}");
                    return false;
                }
                
                if (!File.Exists(coverImagePath))
                {
                    Console.WriteLine($"Cover image does not exist: {coverImagePath}");
                    return false;
                }
                
                // Create a temporary file for the new archive
                string tempFilePath = Path.Combine(
                    Path.GetDirectoryName(cbzFilePath) ?? string.Empty,
                    $"temp_{Path.GetFileName(cbzFilePath)}"
                );
                
                Console.WriteLine($"Created temporary file at: {tempFilePath}");

                // Read the cover image into memory
                byte[] coverImageBytes = await File.ReadAllBytesAsync(coverImagePath);
                Console.WriteLine($"Read cover image, size: {coverImageBytes.Length} bytes");

                // Create a new archive with the cover image as the first file
                using (var tempFileStream = File.Create(tempFilePath))
                using (var tempArchive = new ZipArchive(tempFileStream, ZipArchiveMode.Create, true))
                {
                    // Add cover image as the first file with name "cover.jpg" for Kavita compatibility
                    var coverEntry = tempArchive.CreateEntry("cover.jpg");
                    using (var entryStream = coverEntry.Open())
                    {
                        await entryStream.WriteAsync(coverImageBytes, 0, coverImageBytes.Length);
                        Console.WriteLine("Added cover image to temporary archive");
                    }

                    // Copy all entries from the original archive
                    Console.WriteLine($"Opening original archive: {cbzFilePath}");
                    using (var originalFileStream = File.OpenRead(cbzFilePath))
                    using (var originalArchive = new ZipArchive(originalFileStream, ZipArchiveMode.Read))
                    {
                        Console.WriteLine($"Original archive has {originalArchive.Entries.Count} entries");
                        foreach (var entry in originalArchive.Entries)
                        {
                            // Skip if it's already a cover image with the same name
                            if (entry.Name.Equals("cover.jpg", StringComparison.OrdinalIgnoreCase))
                            {
                                Console.WriteLine($"Skipping existing cover image: {entry.Name}");
                                continue;
                            }

                            // Copy the entry to the new archive
                            Console.WriteLine($"Copying entry: {entry.Name}");
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
                Console.WriteLine($"Deleting original file: {cbzFilePath}");
                File.Delete(cbzFilePath);
                Console.WriteLine($"Moving temporary file to original location");
                File.Move(tempFilePath, cbzFilePath);

                Console.WriteLine($"Successfully inserted cover image into {cbzFilePath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inserting cover image: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }
    }
}
