using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using MangaAssistant.Core.Interfaces;
using MangaAssistant.Core.Models;
using MangaAssistant.Core.Services;
using MangaAssistant.Common.Logging;
using MangaAssistant.Infrastructure.Logging;
using MangaAssistant.Infrastructure.Services.Metadata;
using System.Diagnostics;
using System.Net.Http.Headers;

namespace MangaAssistant.Infrastructure.Services
{
    public class MetadataService : IMetadataService
    {
        private readonly Dictionary<string, Core.Services.IMetadataProvider> _providers;
        private readonly ISettingsService _settingsService;
        public ILibraryService LibraryService { get; }

        public IReadOnlyList<Core.Services.IMetadataProvider> Providers => _providers.Values.ToList().AsReadOnly();

        public MetadataService(ILibraryService libraryService, ISettingsService settingsService)
        {
            LibraryService = libraryService ?? throw new ArgumentNullException(nameof(libraryService));
            _settingsService = settingsService;
            
            // Create a logger adapter and HTTP client for providers
            var logger = new LoggerAdapter();
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MangaAssistant/1.0");
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            
            _providers = new Dictionary<string, Core.Services.IMetadataProvider>
            {
                { "MangaDex", new MangaDexMetadataProvider(_settingsService) },
                { "AniList", new AniListMetadataProvider(_settingsService) },
                { "MangaUpdates", new MangaUpdatesMetadataProvider(httpClient, logger) }
            };
            
            Debug.WriteLine($"MetadataService initialized with {_providers.Count} providers:");
            foreach (var provider in _providers.Values)
            {
                Debug.WriteLine($"- {provider.Name}");
            }
        }

        public async Task<IEnumerable<SeriesSearchResult>> SearchAsync(string providerName, string query)
        {
            Debug.WriteLine($"MetadataService.SearchAsync called with provider: '{providerName}', query: '{query}'");
            Debug.WriteLine($"Available providers: {string.Join(", ", _providers.Values.Select(p => p.Name))}");
            
            if (!_providers.TryGetValue(providerName, out var provider))
            {
                Debug.WriteLine($"Provider not found: {providerName}");
                return Enumerable.Empty<SeriesSearchResult>();
            }

            Debug.WriteLine($"Using provider: {provider.Name}");
            
            try
            {
                var results = await provider.SearchAsync(query);
                Debug.WriteLine($"Search completed with {results?.Count() ?? 0} results");
                return results ?? Enumerable.Empty<SeriesSearchResult>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SearchAsync: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return Enumerable.Empty<SeriesSearchResult>();
            }
        }

        public async Task<SeriesMetadata> GetMetadataAsync(string providerName, string providerId)
        {
            Debug.WriteLine($"MetadataService.GetMetadataAsync called with provider: '{providerName}', id: '{providerId}'");
            
            if (!_providers.TryGetValue(providerName, out var provider))
            {
                Debug.WriteLine($"Provider not found: {providerName}");
                return new SeriesMetadata();
            }

            Debug.WriteLine($"Using provider: {provider.Name}");
            
            try
            {
                var metadata = await provider.GetMetadataAsync(providerId);
                Debug.WriteLine($"Metadata retrieval completed: {(metadata != null ? "Success" : "Failed")}");
                return metadata;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetMetadataAsync: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return new SeriesMetadata();
            }
        }

        public async Task<byte[]?> DownloadCoverImageAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                Debug.WriteLine("DownloadCoverImageAsync: URL is empty");
                return null;
            }

            Debug.WriteLine($"Downloading cover image from: {url}");
            
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);
                
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                var imageData = await response.Content.ReadAsByteArrayAsync();
                Debug.WriteLine($"Downloaded image: {imageData.Length} bytes");
                
                return imageData;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error downloading cover image: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> SaveMetadataAsync(SeriesMetadata metadata, string seriesFolder)
        {
            if (metadata == null)
            {
                Debug.WriteLine("SaveMetadataAsync: Metadata is null");
                return false;
            }

            Debug.WriteLine($"Saving metadata for series: {metadata.Series}");
            
            try
            {
                // Save cover image if available
                if (!string.IsNullOrWhiteSpace(metadata.CoverPath) && metadata.CoverPath.StartsWith("http"))
                {
                    var imageData = await DownloadCoverImageAsync(metadata.CoverPath);
                    if (imageData != null)
                    {
                        var coverPath = Path.Combine(seriesFolder, "cover.jpg");
                        await File.WriteAllBytesAsync(coverPath, imageData);
                        metadata.CoverPath = coverPath;
                    }
                }
                
                // Set HasVolumes flag based on volume count
                if (metadata.Volumes.HasValue && metadata.Volumes.Value > 0)
                {
                    metadata.HasVolumes = true;
                    Logger.Log($"Series has {metadata.Volumes.Value} volumes", Common.Logging.LogLevel.Info);
                }
                
                // Create a temporary Series object to save the metadata
                var series = new Series
                {
                    FolderPath = seriesFolder,
                    Metadata = metadata
                };
                
                // Update the library with the new metadata
                await LibraryService.SaveSeriesMetadataAsync(series);
                
                Debug.WriteLine($"Metadata saved successfully for: {metadata.Series}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving metadata: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
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