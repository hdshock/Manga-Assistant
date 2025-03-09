using System;
using System.IO;
using System.IO.Compression;

namespace FixKillTheHero
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Kill The Hero Cover Fixer");
            Console.WriteLine("=========================");

            try
            {
                // Ask for library path
                Console.Write("Enter your manga library path: ");
                string libraryPath = Console.ReadLine();
                
                if (string.IsNullOrWhiteSpace(libraryPath) || !Directory.Exists(libraryPath))
                {
                    Console.WriteLine($"Error: Invalid library path: {libraryPath}");
                    WaitForExit();
                    return;
                }

                // Find Kill The Hero folder
                string killTheHeroPath = Path.Combine(libraryPath, "Kill The Hero");
                if (!Directory.Exists(killTheHeroPath))
                {
                    // Try to find it by partial match
                    foreach (var dir in Directory.GetDirectories(libraryPath))
                    {
                        if (Path.GetFileName(dir).Contains("Kill", StringComparison.OrdinalIgnoreCase) &&
                            Path.GetFileName(dir).Contains("Hero", StringComparison.OrdinalIgnoreCase))
                        {
                            killTheHeroPath = dir;
                            break;
                        }
                    }
                }

                if (!Directory.Exists(killTheHeroPath))
                {
                    Console.WriteLine("Error: Could not find Kill The Hero folder");
                    WaitForExit();
                    return;
                }

                Console.WriteLine($"Found series folder: {killTheHeroPath}");

                // Find cover image
                string coverPath = Path.Combine(killTheHeroPath, "cover.jpg");
                if (!File.Exists(coverPath))
                {
                    Console.WriteLine("Error: Could not find cover.jpg in series folder");
                    
                    // Try to find any image file that might be the cover
                    string[] imageExtensions = { ".jpg", ".jpeg", ".png" };
                    foreach (var ext in imageExtensions)
                    {
                        var files = Directory.GetFiles(killTheHeroPath, $"*{ext}");
                        if (files.Length > 0)
                        {
                            coverPath = files[0];
                            Console.WriteLine($"Using alternative cover image: {coverPath}");
                            break;
                        }
                    }
                    
                    if (!File.Exists(coverPath))
                    {
                        Console.WriteLine("Error: Could not find any image file to use as cover");
                        WaitForExit();
                        return;
                    }
                }

                // Find chapter 1 CBZ
                string chapter1Path = null;
                
                // First check for direct CBZ files
                var cbzFiles = Directory.GetFiles(killTheHeroPath, "*.cbz");
                foreach (var file in cbzFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    if (fileName.Contains("1") || 
                        fileName.Contains("one", StringComparison.OrdinalIgnoreCase) || 
                        fileName.Contains("first", StringComparison.OrdinalIgnoreCase))
                    {
                        chapter1Path = file;
                        break;
                    }
                }
                
                // If not found, check subdirectories
                if (chapter1Path == null)
                {
                    foreach (var dir in Directory.GetDirectories(killTheHeroPath))
                    {
                        cbzFiles = Directory.GetFiles(dir, "*.cbz");
                        foreach (var file in cbzFiles)
                        {
                            string fileName = Path.GetFileNameWithoutExtension(file);
                            if (fileName.Contains("1") || 
                                fileName.Contains("one", StringComparison.OrdinalIgnoreCase) || 
                                fileName.Contains("first", StringComparison.OrdinalIgnoreCase))
                            {
                                chapter1Path = file;
                                break;
                            }
                        }
                        
                        if (chapter1Path != null)
                            break;
                    }
                }

                if (chapter1Path == null)
                {
                    Console.WriteLine("Error: Could not find chapter 1 CBZ file");
                    
                    // Just use the first CBZ file found
                    cbzFiles = Directory.GetFiles(killTheHeroPath, "*.cbz", SearchOption.AllDirectories);
                    if (cbzFiles.Length > 0)
                    {
                        chapter1Path = cbzFiles[0];
                        Console.WriteLine($"Using first CBZ file found: {chapter1Path}");
                    }
                    else
                    {
                        Console.WriteLine("Error: No CBZ files found");
                        WaitForExit();
                        return;
                    }
                }

                Console.WriteLine($"Found chapter 1 CBZ: {chapter1Path}");
                Console.WriteLine($"Using cover image: {coverPath}");
                
                // Create backup
                string backupPath = chapter1Path + ".backup";
                Console.WriteLine($"Creating backup at: {backupPath}");
                File.Copy(chapter1Path, backupPath, true);

                // Insert cover image
                Console.WriteLine("Inserting cover image...");
                InsertCoverImage(chapter1Path, coverPath);
                
                Console.WriteLine("Operation completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            WaitForExit();
        }

        private static void WaitForExit()
        {
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        private static void InsertCoverImage(string cbzFilePath, string coverImagePath)
        {
            // Get temp file path
            string tempFilePath = Path.Combine(
                Path.GetDirectoryName(cbzFilePath),
                $"temp_{Path.GetFileName(cbzFilePath)}"
            );

            Console.WriteLine($"Temp file path: {tempFilePath}");

            try
            {
                // Read cover image
                byte[] coverImageBytes = File.ReadAllBytes(coverImagePath);
                Console.WriteLine($"Read cover image: {coverImageBytes.Length} bytes");

                // Create new zip file with cover image first
                using (FileStream tempFileStream = File.Create(tempFilePath))
                using (ZipArchive tempArchive = new ZipArchive(tempFileStream, ZipArchiveMode.Create, true))
                {
                    // Add cover image as first entry
                    Console.WriteLine("Adding cover.jpg as first entry");
                    ZipArchiveEntry coverEntry = tempArchive.CreateEntry("cover.jpg", CompressionLevel.Optimal);
                    using (Stream entryStream = coverEntry.Open())
                    {
                        entryStream.Write(coverImageBytes, 0, coverImageBytes.Length);
                    }

                    // Copy all entries from original archive
                    Console.WriteLine($"Opening original archive: {cbzFilePath}");
                    using (FileStream originalFileStream = File.OpenRead(cbzFilePath))
                    using (ZipArchive originalArchive = new ZipArchive(originalFileStream, ZipArchiveMode.Read))
                    {
                        Console.WriteLine($"Original archive has {originalArchive.Entries.Count} entries");
                        
                        foreach (ZipArchiveEntry entry in originalArchive.Entries)
                        {
                            // Skip if it's already a cover image
                            if (string.Equals(entry.Name, "cover.jpg", StringComparison.OrdinalIgnoreCase))
                            {
                                Console.WriteLine($"Skipping existing cover image: {entry.FullName}");
                                continue;
                            }

                            Console.WriteLine($"Copying entry: {entry.FullName}");
                            
                            // Create new entry in temp archive
                            ZipArchiveEntry newEntry = tempArchive.CreateEntry(entry.FullName, CompressionLevel.Optimal);
                            
                            // Copy content
                            using (Stream originalStream = entry.Open())
                            using (Stream newStream = newEntry.Open())
                            {
                                byte[] buffer = new byte[4096];
                                int bytesRead;
                                while ((bytesRead = originalStream.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    newStream.Write(buffer, 0, bytesRead);
                                }
                            }
                        }
                    }
                }

                // Replace original with temp file
                Console.WriteLine("Replacing original file with new file");
                File.Delete(cbzFilePath);
                File.Move(tempFilePath, cbzFilePath);

                // Verify the cover was inserted
                Console.WriteLine("Verifying cover image was inserted");
                using (FileStream fs = File.OpenRead(cbzFilePath))
                using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Read))
                {
                    Console.WriteLine("Contents of updated archive:");
                    foreach (var entry in archive.Entries)
                    {
                        Console.WriteLine($" - {entry.FullName}");
                    }

                    var coverEntry = archive.GetEntry("cover.jpg");
                    if (coverEntry != null)
                    {
                        Console.WriteLine("SUCCESS: cover.jpg found in archive!");
                        
                        // Check if it's the first entry
                        if (archive.Entries[0].Name.Equals("cover.jpg", StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine("SUCCESS: cover.jpg is the first entry!");
                        }
                        else
                        {
                            Console.WriteLine("WARNING: cover.jpg is not the first entry.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("ERROR: cover.jpg not found in archive!");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during cover insertion: {ex.Message}");
                
                // Clean up the temporary file if it exists
                if (File.Exists(tempFilePath))
                {
                    try
                    {
                        File.Delete(tempFilePath);
                        Console.WriteLine($"Deleted temporary file after error: {tempFilePath}");
                    }
                    catch
                    {
                        Console.WriteLine("Failed to delete temporary file");
                    }
                }
                
                throw;
            }
        }
    }
}
