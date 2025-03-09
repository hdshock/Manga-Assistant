using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MangaAssistant.Core.Models;
using MangaAssistant.Core.Models.MangaDex;
using MangaAssistant.Core.Services;
using MangaAssistant.Common.Logging;
using System.Threading;

namespace MangaAssistant.Infrastructure.Services.Metadata
{
    public class MangaDexMetadataProvider : IMetadataProvider, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly SemaphoreSlim _rateLimiter;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly MangaDexScraper _scraper;
        private readonly ISettingsService _settingsService;
        private const string BaseUrl = "https://api.mangadex.org";
        private const string MangaUrl = "https://mangadex.org/title";
        private const string CoverUrl = "https://uploads.mangadex.org/covers";
        private const int RateLimitDelay = 250; // 250ms between requests
        private const int MaxRetries = 3;
        private const int InitialRetryDelay = 1000; // 1 second

        public string Name => "MangaDex";

        public MangaDexMetadataProvider(ISettingsService settingsService)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MangaAssistant/1.0");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            
            _rateLimiter = new SemaphoreSlim(1, 1);
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
            
            _scraper = new MangaDexScraper();
            _settingsService = settingsService;
        }

        public async Task<IEnumerable<SeriesSearchResult>> SearchAsync(string query)
        {
            try
            {
                Logger.Log($"Searching MangaDex for: {query}", LogLevel.Info, "MangaDex");
                
                var parameters = new List<KeyValuePair<string, string>>
                {
                    new("title", query),
                    new("limit", "20"),
                    new("offset", "0"),
                    new("order[relevance]", "desc"),
                    // Include both cover art and author data
                    new("includes[]", "cover_art"),
                    new("includes[]", "author"),
                    // Add all content ratings
                    new("contentRating[]", "safe"),
                    new("contentRating[]", "suggestive"),
                    new("contentRating[]", "erotica"),
                    new("contentRating[]", "pornographic")
                };

                var results = await ExecuteWithRetryAsync(async () =>
                {
                    var url = BuildUrlWithQueryParams($"{BaseUrl}/manga", parameters);
                    Logger.Log($"Making request to: {url}", LogLevel.Debug, "MangaDex");
                    var response = await _httpClient.GetAsync(url);
                    await HandleRateLimitsAsync(response);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        var requestId = response.Headers.GetValues("X-Request-ID").FirstOrDefault();
                        Logger.Log($"MangaDex API error (Request ID: {requestId}): {response.StatusCode}", LogLevel.Error, "MangaDex");
                        var errorContent = await response.Content.ReadAsStringAsync();
                        Logger.Log($"Error response: {errorContent}", LogLevel.Error, "MangaDex");
                        return Enumerable.Empty<SeriesSearchResult>();
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    Logger.Log($"Response content: {content}", LogLevel.Debug, "MangaDex");
                    var mangaResponse = JsonSerializer.Deserialize<MangaDexResponse<List<MangaDexManga>>>(content, _jsonOptions);

                    if (mangaResponse?.Data == null)
                    {
                        Logger.Log("Failed to deserialize MangaDex response", LogLevel.Warning, "MangaDex");
                        return Enumerable.Empty<SeriesSearchResult>();
                    }

                    var results = mangaResponse.Data.Select(manga =>
                    {
                        var title = GetBestTitle(manga.Attributes.Title);
                        var coverUrl = GetCoverUrl(manga);
                        Logger.Log($"Found manga: {title} (ID: {manga.Id}, Cover: {coverUrl})", LogLevel.Debug, "MangaDex");
                        return new SeriesSearchResult
                        {
                            ProviderId = manga.Id,
                            Title = title,
                            Description = GetBestDescription(manga.Attributes.Description),
                            ProviderName = Name,
                            Year = manga.Attributes.Year,
                            Status = ConvertStatus(manga.Attributes.Status),
                            Url = $"{MangaUrl}/{manga.Id}",
                            ChapterCount = 0,
                            VolumeCount = 0,
                            CoverUrl = coverUrl
                        };
                    }).ToList();

                    return results;
                });

                Logger.Log($"Found {results.Count()} results", LogLevel.Info, "MangaDex");
                return results;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error searching MangaDex: {ex.Message}", LogLevel.Error, "MangaDex");
                return Enumerable.Empty<SeriesSearchResult>();
            }
        }

        public async Task<SeriesMetadata> GetMetadataAsync(string providerId)
        {
            try
            {
                Logger.Log($"Fetching metadata for MangaDex ID: {providerId}", LogLevel.Info, "MangaDex");

                var metadata = await ExecuteWithRetryAsync(async () =>
                {
                    var url = $"{BaseUrl}/manga/{providerId}?includes[]=cover_art&includes[]=author&includes[]=artist";
                    var response = await _httpClient.GetAsync(url);
                    await HandleRateLimitsAsync(response);

                    if (!response.IsSuccessStatusCode)
                    {
                        var requestId = response.Headers.GetValues("X-Request-ID").FirstOrDefault();
                        Logger.Log($"MangaDex API error (Request ID: {requestId}): {response.StatusCode}", LogLevel.Error, "MangaDex");
                        return CreateDefaultMetadata(providerId);
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    var mangaResponse = JsonSerializer.Deserialize<MangaDexResponse<MangaDexManga>>(content, _jsonOptions);

                    if (mangaResponse?.Data == null)
                    {
                        Logger.Log("Failed to deserialize manga metadata", LogLevel.Error, "MangaDex");
                        return CreateDefaultMetadata(providerId);
                    }

                    var manga = mangaResponse.Data;
                    var coverUrl = GetCoverUrl(manga);
                    
                    // Get the series directory path from the settings service
                    var libraryPath = _settingsService.LibraryPath;
                    var seriesTitle = GetBestTitle(manga.Attributes.Title);
                    var seriesPath = Path.Combine(libraryPath, seriesTitle);
                    
                    // Create directory if it doesn't exist
                    if (!Directory.Exists(seriesPath))
                    {
                        Directory.CreateDirectory(seriesPath);
                    }
                    
                    // Download and handle cover
                    var localCoverPath = await HandleCoverDownloadAsync(seriesPath, coverUrl);
                    
                    var metadata = new SeriesMetadata
                    {
                        Series = seriesTitle,
                        Description = GetBestDescription(manga.Attributes.Description),
                        Year = manga.Attributes.Year ?? 0,
                        Status = ConvertStatus(manga.Attributes.Status),
                        Genres = manga.Attributes.Tags
                            .Where(t => t.Attributes.Group == "genre")
                            .Select(t => GetBestTagName(t.Attributes.Name))
                            .ToList(),
                        Tags = manga.Attributes.Tags
                            .Where(t => t.Attributes.Group != "genre")
                            .Select(t => GetBestTagName(t.Attributes.Name))
                            .ToList(),
                        ProviderId = providerId,
                        ProviderName = Name,
                        ProviderUrl = $"{MangaUrl}/{providerId}",
                        CoverUrl = coverUrl,
                        CoverPath = !string.IsNullOrEmpty(localCoverPath) ? localCoverPath : coverUrl,
                        LastModified = DateTime.Now
                    };

                    await EnrichWithChapterMetadata(metadata, providerId);
                    
                    Logger.Log($"Updated cover for manga {metadata.Series}: {coverUrl}", LogLevel.Info, "MangaDex");
            
                    return metadata;
                });

                return metadata ?? CreateDefaultMetadata(providerId);
            }
            catch (Exception ex)
            {
                Logger.Log($"Error fetching metadata: {ex.Message}", LogLevel.Error, "MangaDex");
                return CreateDefaultMetadata(providerId);
            }
        }

        private async Task EnrichWithChapterMetadata(SeriesMetadata metadata, string mangaId)
        {
            try
            {
                var parameters = new List<KeyValuePair<string, string>>
                {
                    new("order[chapter]", "asc"),
                    new("limit", "500"),
                    new("offset", "0"),
                    // Include all languages to ensure we get a complete chapter count
                    new("includes[]", "scanlation_group"),
                    new("includes[]", "user")
                };

                await ExecuteWithRetryAsync<object>(async () =>
                {
                    var url = BuildUrlWithQueryParams($"{BaseUrl}/manga/{mangaId}/feed", parameters);
                    Logger.Log($"Fetching chapters from: {url}", LogLevel.Debug, "MangaDex");
                    var response = await _httpClient.GetAsync(url);
                    await HandleRateLimitsAsync(response);

                    if (!response.IsSuccessStatusCode)
                    {
                        var requestId = response.Headers.GetValues("X-Request-ID").FirstOrDefault();
                        Logger.Log($"Failed to fetch chapters (Request ID: {requestId}): {response.StatusCode}", LogLevel.Warning, "MangaDex");
                        var errorContent = await response.Content.ReadAsStringAsync();
                        Logger.Log($"Error response: {errorContent}", LogLevel.Warning, "MangaDex");
                        return null;
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    Logger.Log($"Chapter response: {content}", LogLevel.Debug, "MangaDex");
                    var chaptersResponse = JsonSerializer.Deserialize<MangaDexResponse<List<MangaDexChapter>>>(content, _jsonOptions);

                    if (chaptersResponse?.Data == null)
                    {
                        Logger.Log("Failed to deserialize chapter data", LogLevel.Warning, "MangaDex");
                        return null;
                    }

                    var chapters = chaptersResponse.Data
                        .Where(c => !string.IsNullOrWhiteSpace(c.Attributes.Chapter))
                        .ToList();

                    metadata.Count = chapters.Count;

                    var volumes = chapters
                        .Where(c => !string.IsNullOrWhiteSpace(c.Attributes.Volume))
                        .Select(c => 
                        {
                            if (int.TryParse(c.Attributes.Volume!, out var vol))
                                return vol;
                            return 0;
                        })
                        .Where(v => v > 0)
                        .Distinct()
                        .OrderBy(v => v)
                        .ToList();

                    metadata.Volumes = volumes.Count > 0 ? volumes.Max() : 0;
                    metadata.HasVolumes = volumes.Any();

                    Logger.Log($"Found {chapters.Count} chapters and {volumes.Count} volumes", LogLevel.Info, "MangaDex");
                    return null;
                });
            }
            catch (Exception ex)
            {
                Logger.Log($"Error enriching chapter metadata: {ex.Message}", LogLevel.Error, "MangaDex");
            }
        }

        public async Task<byte[]?> GetCoverImageAsync(string url)
        {
            try
            {
                var response = await ExecuteWithRetryAsync<HttpResponseMessage>(async () =>
                {
                    var response = await _httpClient.GetAsync(url);
                    await HandleRateLimitsAsync(response);
                    return response;
                });

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }

                Logger.Log($"Failed to fetch cover image: {response.StatusCode}", LogLevel.Warning, "MangaDex");
                return null;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error fetching cover image: {ex.Message}", LogLevel.Error, "MangaDex");
                return null;
            }
        }

        private string GetBestTitle(Dictionary<string, string> titles)
        {
            return titles.GetValueOrDefault("en") ?? 
                   titles.GetValueOrDefault("en-us") ?? 
                   titles.GetValueOrDefault("ja-ro") ?? 
                   titles.GetValueOrDefault("ja") ?? 
                   titles.Values.FirstOrDefault() ?? 
                   "Unknown Title";
        }

        private string GetBestDescription(Dictionary<string, string> descriptions)
        {
            return descriptions.GetValueOrDefault("en") ?? 
                   descriptions.GetValueOrDefault("en-us") ?? 
                   descriptions.Values.FirstOrDefault() ?? 
                   string.Empty;
        }

        private string GetBestTagName(Dictionary<string, string> names)
        {
            return names.GetValueOrDefault("en") ?? 
                   names.Values.FirstOrDefault() ?? 
                   string.Empty;
        }

        private string ConvertStatus(string mangaDexStatus)
        {
            return mangaDexStatus.ToLower() switch
            {
                "ongoing" => "Ongoing",
                "completed" => "Completed",
                "hiatus" => "Hiatus",
                "cancelled" => "Cancelled",
                _ => "Unknown"
            };
        }

        private string BuildUrlWithQueryParams(string baseUrl, List<KeyValuePair<string, string>> queryParams)
        {
            if (!queryParams.Any())
                return baseUrl;

            var queryString = string.Join("&", queryParams.Select(kvp => 
                $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
            
            return $"{baseUrl}?{queryString}";
        }

        private SeriesMetadata CreateDefaultMetadata(string providerId)
        {
            return new SeriesMetadata
            {
                ProviderId = providerId,
                ProviderName = Name,
                ProviderUrl = $"{MangaUrl}/{providerId}",
                Series = "Unknown Series",
                Status = "Unknown",
                CoverUrl = string.Empty,
                CoverPath = string.Empty,
                LastModified = DateTime.Now
            };
        }

        private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action) where T : class
        {
            for (int i = 0; i < MaxRetries; i++)
            {
                try
                {
                    await _rateLimiter.WaitAsync();
                    var result = await action();
                    return result ?? throw new InvalidOperationException("Action returned null result");
                }
                catch (HttpRequestException ex)
                {
                    if (i == MaxRetries - 1) throw;
                    
                    var delay = InitialRetryDelay * Math.Pow(2, i);
                    Logger.Log($"Request failed, retrying in {delay}ms: {ex.Message}", LogLevel.Warning, "MangaDex");
                    await Task.Delay((int)delay);
                }
                finally
                {
                    _rateLimiter.Release();
                }
            }

            throw new Exception($"Failed after {MaxRetries} retries");
        }

        private async Task HandleRateLimitsAsync(HttpResponseMessage response)
        {
            if (response.Headers.TryGetValues("X-RateLimit-Remaining", out var remaining) &&
                int.TryParse(remaining.FirstOrDefault(), out var remainingRequests))
            {
                if (remainingRequests <= 1)
                {
                    if (response.Headers.TryGetValues("X-RateLimit-Reset", out var reset) &&
                        int.TryParse(reset.FirstOrDefault(), out var resetTime))
                    {
                        var delay = resetTime * 1000; // Convert to milliseconds
                        Logger.Log($"Rate limit nearly exceeded, waiting {delay}ms", LogLevel.Warning, "MangaDex");
                        await Task.Delay(delay);
                    }
                }
            }
        }

        private string GetCoverUrl(MangaDexManga manga)
        {
            var coverArt = manga.Relationships
                .FirstOrDefault(r => r.Type == "cover_art")?
                .Attributes as JsonElement?;

            if (coverArt == null)
                return string.Empty;

            var fileName = coverArt.Value
                .GetProperty("fileName")
                .GetString();

            if (string.IsNullOrEmpty(fileName))
                return string.Empty;

            return $"{CoverUrl}/{manga.Id}/{fileName}";
        }

        private async Task<string> HandleCoverDownloadAsync(string seriesPath, string coverUrl)
        {
            try
            {
                Logger.Log($"Downloading cover from {coverUrl}", LogLevel.Info, "MangaDex");
                
                // Create the target path
                var coverPath = Path.Combine(seriesPath, "cover.jpg");
                
                // Download and save the cover
                var coverData = await GetCoverImageAsync(coverUrl);
                if (coverData != null)
                {
                    // Clean up existing cover files
                    foreach (var file in Directory.GetFiles(seriesPath, "cover.*"))
                    {
                        if (file != coverPath)
                        {
                            try
                            {
                                File.Delete(file);
                                Logger.Log($"Deleted old cover file: {file}", LogLevel.Info, "MangaDex");
                            }
                            catch (Exception ex)
                            {
                                Logger.Log($"Error deleting old cover file {file}: {ex.Message}", LogLevel.Error, "MangaDex");
                            }
                        }
                    }
                    
                    // Save the new cover
                    await File.WriteAllBytesAsync(coverPath, coverData);
                    Logger.Log($"Saved cover to {coverPath}", LogLevel.Info, "MangaDex");
                    
                    // Update first chapter if it exists
                    var cbzFiles = Directory.GetFiles(seriesPath, "*.cbz");
                    if (cbzFiles.Length > 0)
                    {
                        var firstChapter = cbzFiles.OrderBy(f => f).First();
                        var scanner = new LibraryScanner(null); // We'll need to inject this
                        await scanner.InsertCoverIntoChapterAsync(firstChapter, coverPath);
                    }
                    
                    return coverPath;
                }
                
                Logger.Log("Failed to download cover image", LogLevel.Warning, "MangaDex");
                return string.Empty;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error handling cover download: {ex.Message}", LogLevel.Error, "MangaDex");
                return string.Empty;
            }
        }

        public void Dispose()
        {
            _httpClient.Dispose();
            _rateLimiter.Dispose();
        }
    }
}
