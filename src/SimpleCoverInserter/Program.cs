using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace SimpleCoverInserter
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Simple Cover Inserter Tool");
            Console.WriteLine("=========================");

            try
            {
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

                // Validate inputs
                if (string.IsNullOrWhiteSpace(cbzFilePath))
                {
                    Console.WriteLine("Error: CBZ file path cannot be empty.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(coverImagePath))
                {
                    Console.WriteLine("Error: Cover image path cannot be empty.");
                    return;
                }

                // Ensure paths are absolute
                cbzFilePath = Path.GetFullPath(cbzFilePath);
                coverImagePath = Path.GetFullPath(coverImagePath);

                Console.WriteLine($"CBZ file: {cbzFilePath}");
                Console.WriteLine($"Cover image: {coverImagePath}");

                if (!File.Exists(cbzFilePath))
                {
                    Console.WriteLine($"Error: CBZ file does not exist: {cbzFilePath}");
                    return;
                }

                if (!File.Exists(coverImagePath))
                {
                    Console.WriteLine($"Error: Cover image does not exist: {coverImagePath}");
                    return;
                }

                // Create backup of original file
                string backupPath = cbzFilePath + ".backup";
                Console.WriteLine($"Creating backup at: {backupPath}");
                File.Copy(cbzFilePath, backupPath, true);

                // Get temp file path
                string tempFilePath = Path.Combine(
                    Path.GetDirectoryName(cbzFilePath),
                    $"temp_{Path.GetFileName(cbzFilePath)}"
                );

                Console.WriteLine($"Temp file path: {tempFilePath}");

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

                Console.WriteLine("Operation completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
