using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MangaAssistant.Core.Models;
using MangaAssistant.Core.Services;
using MangaAssistant.Infrastructure.Services.Metadata;
using System.Diagnostics;

namespace MangaAssistant.Infrastructure.Services
{
    public class MetadataService : IMetadataService
    {
        private readonly List<IMetadataProvider> _providers;
        public ILibraryService LibraryService { get; }

        public IReadOnlyList<IMetadataProvider> Providers => _providers.AsReadOnly();

        public MetadataService(ILibraryService libraryService)
        {
            LibraryService = libraryService ?? throw new ArgumentNullException(nameof(libraryService));
            _providers = new List<IMetadataProvider>
            {
                new AniListMetadataProvider()
            };
            // TODO: Add metadata providers
        }

        public async Task<IEnumerable<SeriesSearchResult>> SearchAsync(string providerName, string query)
        {
            var provider = _providers.FirstOrDefault(p => p.Name.Equals(providerName, StringComparison.OrdinalIgnoreCase));
            if (provider == null)
                return new List<SeriesSearchResult>();

            return await provider.SearchAsync(query);
        }

        public async Task UpdateSeriesMetadataAsync(Series series, string providerName, string providerId)
        {
            if (series == null) throw new ArgumentNullException(nameof(series));
            
            try
            {
                // Ensure metadata exists
                if (series.Metadata == null)
                {
                    series.Metadata = new SeriesMetadata();
                }

                // Update provider information
                series.Metadata.ProviderName = providerName;
                series.Metadata.ProviderId = providerId;
                series.Metadata.LastModified = DateTime.Now;
                series.Metadata.HasMetadata = true;

                // Save metadata using LibraryService
                await LibraryService.SaveSeriesMetadataAsync(series);
                var metadataPath = Path.Combine(series.FolderPath, "series-info.json");
                Debug.WriteLine($"Metadata saved for series: {series.Title} at {metadataPath}");

                // Verify the file was written
                if (File.Exists(metadataPath))
                {
                    var fileContent = await File.ReadAllTextAsync(metadataPath);
                    Debug.WriteLine($"Metadata file size: {fileContent.Length} bytes");
                }
                else
                {
                    Debug.WriteLine("Warning: Metadata file was not created!");
                }

                // Trigger a library update
                await LibraryService.ScanLibraryAsync();
                Debug.WriteLine($"Library scan completed after metadata update for {series.Title}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating metadata: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
} 