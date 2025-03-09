using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using MangaAssistant.Core.Interfaces;
using MangaAssistant.Core.Models;
using MangaAssistant.Core.Services;
using System.Diagnostics;

namespace MangaAssistant.Infrastructure.Services.Metadata
{
    public class MangaUpdatesMetadataProvider : Core.Services.IMetadataProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private const string BaseUrl = "https://api.mangaupdates.com/v1";
        private const string SearchUrl = $"{BaseUrl}/series/search";
        private const string SeriesUrl = $"{BaseUrl}/series";
        private const string WebsiteUrl = "https://www.mangaupdates.com/series.html?id=";

        public string Name => "MangaUpdates";

        public MangaUpdatesMetadataProvider(HttpClient httpClient, ILogger logger)
        {
            _httpClient = httpClient ?? new HttpClient();
            _logger = logger;
            
            // Configure HttpClient with appropriate headers and timeout
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MangaAssistant", "1.0"));
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<IEnumerable<SeriesSearchResult>> SearchAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Enumerable.Empty<SeriesSearchResult>();
            }

            try
            {
                Debug.WriteLine($"MangaUpdatesMetadataProvider: Searching for '{query}'");
                
                // Create search payload
                var searchPayload = new
                {
                    search = query,
                    page = 1,
                    perpage = 20,
                    include_rank_metadata = false
                };
                
                // Convert to JSON
                var content = new StringContent(
                    JsonSerializer.Serialize(searchPayload),
                    System.Text.Encoding.UTF8,
                    "application/json");
                
                Debug.WriteLine($"MangaUpdatesMetadataProvider: Sending request to {SearchUrl}");
                
                // Send request
                var response = await _httpClient.PostAsync(SearchUrl, content);
                
                // Check if request was successful
                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"MangaUpdatesMetadataProvider: Error response: {response.StatusCode}");
                    _logger?.Log($"MangaUpdates API error: {response.StatusCode}", LogLevel.Error);
                    return Enumerable.Empty<SeriesSearchResult>();
                }
                
                // Parse response
                var responseContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"MangaUpdatesMetadataProvider: Received response with length: {responseContent.Length}");
                
                var results = new List<SeriesSearchResult>();
                
                // Parse JSON
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                // Check if results exist
                if (jsonResponse.TryGetProperty("results", out var resultsElement) && 
                    resultsElement.ValueKind == JsonValueKind.Array)
                {
                    Debug.WriteLine($"MangaUpdatesMetadataProvider: Found {resultsElement.GetArrayLength()} results");
                    
                    // Process each result
                    foreach (var resultElement in resultsElement.EnumerateArray())
                    {
                        try
                        {
                            var record = resultElement.GetProperty("record");
                            var seriesId = record.GetProperty("series_id").GetInt32().ToString();
                            var title = record.GetProperty("title").GetString() ?? string.Empty;
                            
                            // Get cover image if available
                            string coverUrl = string.Empty;
                            if (record.TryGetProperty("image", out var imageElement) && 
                                imageElement.ValueKind != JsonValueKind.Null &&
                                imageElement.TryGetProperty("url", out var urlElement))
                            {
                                coverUrl = urlElement.GetString() ?? string.Empty;
                            }
                            
                            // Get status if available
                            string status = "Unknown";
                            if (record.TryGetProperty("status", out var statusElement) && 
                                statusElement.ValueKind != JsonValueKind.Null)
                            {
                                status = statusElement.GetString() ?? "Unknown";
                            }
                            
                            // Get description if available
                            string description = string.Empty;
                            if (record.TryGetProperty("description", out var descElement) && 
                                descElement.ValueKind != JsonValueKind.Null)
                            {
                                description = descElement.GetString() ?? string.Empty;
                            }
                            
                            // Get year if available
                            int? year = null;
                            if (record.TryGetProperty("year", out var yearElement) && 
                                yearElement.ValueKind != JsonValueKind.Null)
                            {
                                year = yearElement.GetInt32();
                            }
                            
                            // Get volume count if available
                            int? volumeCount = null;
                            if (record.TryGetProperty("volume_count", out var volumeElement) && 
                                volumeElement.ValueKind != JsonValueKind.Null)
                            {
                                volumeCount = volumeElement.GetInt32();
                                Debug.WriteLine($"MangaUpdatesMetadataProvider: Series has {volumeCount} volumes");
                            }
                            
                            // Create search result
                            var searchResult = new SeriesSearchResult
                            {
                                ProviderId = seriesId,
                                Title = title,
                                CoverUrl = coverUrl,
                                Status = status,
                                Description = description,
                                ReleaseYear = year,
                                ChapterCount = 0, // MangaUpdates doesn't provide this info in search results
                                VolumeCount = volumeCount ?? 0
                            };
                            
                            results.Add(searchResult);
                            Debug.WriteLine($"MangaUpdatesMetadataProvider: Added result: {title} (ID: {seriesId})");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"MangaUpdatesMetadataProvider: Error processing result: {ex.Message}");
                            _logger?.Log($"Error processing MangaUpdates result: {ex.Message}", LogLevel.Error);
                        }
                    }
                }
                
                Debug.WriteLine($"MangaUpdatesMetadataProvider: Returning {results.Count} results");
                return results;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MangaUpdatesMetadataProvider: Search error: {ex.Message}");
                Debug.WriteLine($"MangaUpdatesMetadataProvider: Stack trace: {ex.StackTrace}");
                _logger?.Log($"MangaUpdates search error: {ex.Message}", LogLevel.Error);
                return Enumerable.Empty<SeriesSearchResult>();
            }
        }

        public async Task<SeriesMetadata> GetMetadataAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return new SeriesMetadata();
            }

            try
            {
                Debug.WriteLine($"MangaUpdatesMetadataProvider: Getting metadata for ID '{id}'");
                
                // Send request to get series details
                var response = await _httpClient.GetAsync($"{SeriesUrl}/{id}");
                
                // Check if request was successful
                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"MangaUpdatesMetadataProvider: Error response: {response.StatusCode}");
                    _logger?.Log($"MangaUpdates API error: {response.StatusCode}", LogLevel.Error);
                    return new SeriesMetadata();
                }
                
                // Parse response
                var responseContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"MangaUpdatesMetadataProvider: Received response with length: {responseContent.Length}");
                
                // Parse JSON
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                // Extract metadata
                var title = jsonResponse.GetProperty("title").GetString() ?? string.Empty;
                
                // Get cover image if available
                string coverUrl = string.Empty;
                if (jsonResponse.TryGetProperty("image", out var imageElement) && 
                    imageElement.ValueKind != JsonValueKind.Null &&
                    imageElement.TryGetProperty("url", out var urlElement))
                {
                    coverUrl = urlElement.GetString() ?? string.Empty;
                }
                
                // Get status if available
                string status = "Unknown";
                if (jsonResponse.TryGetProperty("status", out var statusElement) && 
                    statusElement.ValueKind != JsonValueKind.Null)
                {
                    status = statusElement.GetString() ?? "Unknown";
                }
                
                // Get description if available
                string description = string.Empty;
                if (jsonResponse.TryGetProperty("description", out var descElement) && 
                    descElement.ValueKind != JsonValueKind.Null)
                {
                    description = descElement.GetString() ?? string.Empty;
                }
                
                // Get year if available
                int? year = null;
                if (jsonResponse.TryGetProperty("year", out var yearElement) && 
                    yearElement.ValueKind != JsonValueKind.Null)
                {
                    year = yearElement.GetInt32();
                }
                
                // Check if the series has volumes
                int? volumeCount = null;
                if (jsonResponse.TryGetProperty("volume_count", out var volumeElement) && 
                    volumeElement.ValueKind != JsonValueKind.Null)
                {
                    volumeCount = volumeElement.GetInt32();
                    Debug.WriteLine($"MangaUpdatesMetadataProvider: Series has {volumeCount} volumes");
                }
                
                // Get genres if available
                var genres = new List<string>();
                if (jsonResponse.TryGetProperty("genres", out var genresElement) && 
                    genresElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var genreElement in genresElement.EnumerateArray())
                    {
                        if (genreElement.TryGetProperty("genre", out var genreNameElement))
                        {
                            genres.Add(genreNameElement.GetString() ?? string.Empty);
                        }
                    }
                }
                
                // Create metadata object
                var metadata = new SeriesMetadata
                {
                    ProviderId = id,
                    Series = title,
                    CoverPath = coverUrl,
                    Status = status,
                    Description = description,
                    ReleaseYear = year,
                    Volumes = volumeCount ?? 0,
                    HasVolumes = volumeCount.HasValue && volumeCount.Value > 0,
                    Genres = new List<string>(genres),
                    Tags = new List<string>(),
                    ProviderName = Name,
                    ProviderUrl = $"{WebsiteUrl}{id}"
                };
                
                Debug.WriteLine($"MangaUpdatesMetadataProvider: Successfully retrieved metadata for '{title}'");
                return metadata;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MangaUpdatesMetadataProvider: Metadata error: {ex.Message}");
                Debug.WriteLine($"MangaUpdatesMetadataProvider: Stack trace: {ex.StackTrace}");
                _logger?.Log($"MangaUpdates metadata error: {ex.Message}", LogLevel.Error);
                return new SeriesMetadata();
            }
        }
        
        public async Task<byte[]?> GetCoverImageAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                Debug.WriteLine("MangaUpdatesMetadataProvider: Empty cover URL provided");
                return null;
            }
            
            try
            {
                Debug.WriteLine($"MangaUpdatesMetadataProvider: Downloading cover image from '{url}'");
                
                // Send request to download the image
                var response = await _httpClient.GetAsync(url);
                
                // Check if request was successful
                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"MangaUpdatesMetadataProvider: Error downloading cover: {response.StatusCode}");
                    _logger?.Log($"Error downloading cover image: {response.StatusCode}", LogLevel.Error);
                    return null;
                }
                
                // Read image data
                var imageData = await response.Content.ReadAsByteArrayAsync();
                Debug.WriteLine($"MangaUpdatesMetadataProvider: Downloaded {imageData.Length} bytes");
                
                return imageData;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MangaUpdatesMetadataProvider: Error downloading cover: {ex.Message}");
                _logger?.Log($"Error downloading cover image: {ex.Message}", LogLevel.Error);
                return null;
            }
        }
    }
}
