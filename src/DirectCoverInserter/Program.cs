using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace DirectCoverInserter
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Direct Cover Inserter Tool");
            Console.WriteLine("=========================");

            string cbzFilePath;
            string coverImagePath;

            if (args.Length >= 2)
            {
                cbzFilePath = args[0];
                coverImagePath = args[1];
            }
            else
            {
                Console.Write("Enter the path to the CBZ file: ");
                cbzFilePath = Console.ReadLine();

                Console.Write("Enter the path to the cover image: ");
                coverImagePath = Console.ReadLine();
            }

            if (!File.Exists(cbzFilePath))
            {
                Console.WriteLine($"Error: CBZ file does not exist: {cbzFilePath}");
                WaitForExit();
                return;
            }

            if (!File.Exists(coverImagePath))
            {
                Console.WriteLine($"Error: Cover image does not exist: {coverImagePath}");
                WaitForExit();
                return;
            }

            Console.WriteLine($"CBZ file: {cbzFilePath}");
            Console.WriteLine($"Cover image: {coverImagePath}");
            Console.WriteLine("Inserting cover image...");

            try
            {
                bool success = await InsertCoverIntoChapterAsync(cbzFilePath, coverImagePath);
                
                if (success)
                {
                    Console.WriteLine("Cover image inserted successfully!");
                    
                    // Verify the cover image was inserted
                    bool verified = await VerifyCoverImageInCbzAsync(cbzFilePath);
                    if (verified)
                    {
                        Console.WriteLine("Verification successful: cover.jpg found in the CBZ file.");
                    }
                    else
                    {
                        Console.WriteLine("Verification failed: cover.jpg not found in the CBZ file!");
                    }
                }
                else
                {
                    Console.WriteLine("Failed to insert cover image.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            WaitForExit();
        }

        private static void WaitForExit()
        {
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        /// <summary>
        /// Verifies that the cover.jpg file exists in the CBZ file
        /// </summary>
        private static async Task<bool> VerifyCoverImageInCbzAsync(string cbzFilePath)
        {
            try
            {
                using (var fileStream = File.OpenRead(cbzFilePath))
                using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Read))
                {
                    // List all entries in the archive for debugging
                    Console.WriteLine("Archive contents:");
                    foreach (var entry in archive.Entries)
                    {
                        Console.WriteLine($" - {entry.FullName}");
                    }

                    // Check if cover.jpg exists
                    var coverEntry = archive.GetEntry("cover.jpg");
                    return coverEntry != null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error verifying cover image: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Inserts a cover image into a CBZ file as the first entry
        /// </summary>
        private static async Task<bool> InsertCoverIntoChapterAsync(string cbzFilePath, string coverImagePath)
        {
            string tempFilePath = null;
            
            try
            {
                Console.WriteLine($"Starting cover insertion for: {cbzFilePath}");
                
                // Create a temporary file for the new archive
                tempFilePath = Path.Combine(
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
                Console.WriteLine($"Error during cover insertion: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Clean up the temporary file if it exists
                if (tempFilePath != null && File.Exists(tempFilePath))
                {
                    try
                    {
                        File.Delete(tempFilePath);
                        Console.WriteLine($"Deleted temporary file after error: {tempFilePath}");
                    }
                    catch (Exception cleanupEx)
                    {
                        Console.WriteLine($"Error cleaning up temporary file: {cleanupEx.Message}");
                    }
                }
                
                return false;
            }
        }
    }
}
