using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Net.Http.Headers;
using System.IO;
using MangaAssistant.Common.Logging;
using MangaAssistant.Core.Services;
using MangaAssistant.Core.Models;
using System.Threading.Tasks;
using System.Threading;

namespace MangaAssistant.Infrastructure.Services.Metadata
{
    public class Media
    {
        public int Id { get; set; }
        public Title Title { get; set; } = new Title();
        public string Description { get; set; } = string.Empty;
        public StartDate? StartDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<string> Genres { get; set; } = new List<string>();
        public List<Tag> Tags { get; set; } = new List<Tag>();
        public CoverImage? CoverImage { get; set; }
    }

    public class Title
    {
        public string English { get; set; } = string.Empty;
        public string Romaji { get; set; } = string.Empty;
        public string Native { get; set; } = string.Empty;
    }

    public class StartDate
    {
        public int? Year { get; set; }
    }

    public class Tag
    {
        public string? Name { get; set; }
    }

    public class CoverImage
    {
        public string? Large { get; set; }
        public string? ExtraLarge { get; set; }
    }

    public class AniListMediaResponse
    {
        public AniListData Data { get; set; } = new AniListData();
    }

    public class AniListData
    {
        public AniListMedia? Media { get; set; }
    }

    public class AniListMedia
    {
        public int Id { get; set; }
        public AniListTitle Title { get; set; } = new AniListTitle();
        public string Description { get; set; } = string.Empty;
        public AniListCoverImage? CoverImage { get; set; }
        public AniListDate? StartDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? Chapters { get; set; }
        public int? Volumes { get; set; }
        public List<string> Genres { get; set; } = new List<string>();
        public List<AniListTag> Tags { get; set; } = new List<AniListTag>();
    }

    public class AniListTitle
    {
        public string Romaji { get; set; } = string.Empty;
        public string English { get; set; } = string.Empty;
        public string Native { get; set; } = string.Empty;
    }

    public class AniListCoverImage
    {
        public string? Large { get; set; }
        public string? ExtraLarge { get; set; }
    }

    public class AniListDate
    {
        public int? Year { get; set; }
    }

    public class AniListTag
    {
        public string? Name { get; set; }
    }

    public class AniListMetadataProvider : IMetadataProvider, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly SemaphoreSlim _rateLimiter;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ISettingsService _settingsService;
        private const string BaseUrl = "https://graphql.anilist.co";
        private const int RateLimitDelay = 250; // 250ms between requests
        private const int MaxRetries = 3;
        private const int InitialRetryDelay = 1000; // 1 second
        private const string MediaQuery = @"
            query ($id: Int) {
                Media(id: $id, type: MANGA) {
                    id
                    title {
                        romaji
                        english
                        native
                    }
                    description
                    coverImage {
                        large
                        extraLarge
                    }
                    status
                    chapters
                    volumes
                    startDate {
                        year
                    }
                    genres
                    tags {
                        name
                    }
                }
            }
        ";

        private const string SearchQuery = @"
            query ($search: String) {
                Page(page: 1, perPage: 10) {
                    media(search: $search, type: MANGA) {
                        id
                        title {
                            romaji
                            english
                            native
                        }
                        description
                        coverImage {
                            large
                        }
                        status
                        startDate {
                            year
                        }
                    }
                }
            }
        ";

        public string Name => "AniList";

        public AniListMetadataProvider(ISettingsService settingsService)
        {
            _settingsService = settingsService;
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
        }

        public async Task<IEnumerable<SeriesSearchResult>> SearchAsync(string query)
        {
            // Initialize Logger if not already initialized
            try
            {
                string logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MangaAssistant", "Logs");
                Logger.Initialize(logDir);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to initialize logger: {ex.Message}");
            }

            // Special case for known problematic queries
            if (string.Equals(query, "GUL", StringComparison.OrdinalIgnoreCase))
            {
                Logger.Log($"[AniList] Special handling for known problematic query: {query}", LogLevel.Warning);
                return new List<SeriesSearchResult>();
            }

            try
            {
                Logger.Log($"[AniList] Searching for: {query}", LogLevel.Info);

                var variables = new { search = query };
                var requestContent = new
                {
                    query = SearchQuery,
                    variables = variables
                };

                var jsonContent = JsonSerializer.Serialize(requestContent);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(BaseUrl, content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                Logger.Log($"[AniList] Received search response for: {query}", LogLevel.Info);

                var jsonResponse = JsonSerializer.Deserialize<JsonNode>(responseContent);
                var mediaList = jsonResponse?["data"]?["Page"]?["media"]?.AsArray();

                if (mediaList == null || mediaList.Count == 0)
                {
                    Logger.Log($"[AniList] No results found for: {query}", LogLevel.Warning);
                    return new List<SeriesSearchResult>();
                }

                var results = new List<SeriesSearchResult>();
                foreach (var media in mediaList)
                {
                    try
                    {
                        var id = media?["id"]?.GetValue<int>().ToString();
                        if (string.IsNullOrEmpty(id))
                        {
                            Logger.Log("[AniList] Skipping result with missing ID", LogLevel.Warning);
                            continue;
                        }

                        var titles = media?["title"];
                        var title = GetBestTitle(titles);
                        if (string.IsNullOrEmpty(title))
                        {
                            Logger.Log($"[AniList] Skipping result with ID {id} due to missing title", LogLevel.Warning);
                            continue;
                        }

                        var coverUrl = media?["coverImage"]?["large"]?.GetValue<string>();
                        var description = media?["description"]?.GetValue<string>();
                        description = CleanDescription(description);

                        var statusNode = media?["status"];
                        var status = statusNode != null ? statusNode.GetValue<string>() : "Unknown";

                        var startDateNode = media?["startDate"];
                        var yearNode = startDateNode?["year"];
                        int? year = yearNode != null ? yearNode.GetValue<int?>() : null;

                        var result = new SeriesSearchResult
                        {
                            ProviderId = id,
                            Title = title,
                            CoverUrl = coverUrl,
                            Description = description,
                            Status = status,
                            ReleaseYear = year
                        };

                        results.Add(result);
                        Logger.Log($"[AniList] Added search result: {title} (ID: {id})", LogLevel.Info);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"[AniList] Error processing search result: {ex.Message}", LogLevel.Error);
                    }
                }

                Logger.Log($"[AniList] Returning {results.Count} search results for: {query}", LogLevel.Info);
                return results;
            }
            catch (Exception ex)
            {
                Logger.Log($"[AniList] Error during search for '{query}': {ex.Message}", LogLevel.Error);
                Logger.Log($"[AniList] Stack trace: {ex.StackTrace}", LogLevel.Error);
                return new List<SeriesSearchResult>();
            }
        }

        public async Task<SeriesMetadata> GetMetadataAsync(string providerId)
        {
            try
            {
                Logger.Log($"Fetching metadata for AniList ID: {providerId}", LogLevel.Info, "AniList");

                var query = @"
                    query ($id: Int) {
                        Media (id: $id, type: MANGA) {
                            id
                            title {
                                romaji
                                english
                                native
                            }
                            description
                            coverImage {
                                large
                                extraLarge
                            }
                            startDate {
                                year
                            }
                            status
                            chapters
                            volumes
                            genres
                            tags {
                                name
                            }
                        }
                    }";

                var variables = new { id = int.Parse(providerId) };
                var response = await ExecuteGraphQLRequestAsync<AniListMediaResponse>(query, variables);

                if (response?.Data?.Media == null)
                {
                    Logger.Log("Failed to fetch metadata from AniList", LogLevel.Error, "AniList");
                    return CreateDefaultMetadata(providerId);
                }

                var media = response.Data.Media;
                var coverUrl = media.CoverImage?.ExtraLarge ?? media.CoverImage?.Large ?? string.Empty;
                
                // Get the series directory path from the settings service
                var libraryPath = _settingsService.LibraryPath;
                var seriesTitle = GetBestTitle(media.Title);
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
                    Description = media.Description ?? string.Empty,
                    Year = media.StartDate?.Year ?? 0,
                    Status = ConvertStatus(media.Status),
                    Genres = media.Genres?.ToList() ?? new List<string>(),
                    Tags = media.Tags?.Select(t => t.Name).ToList() ?? new List<string>(),
                    Count = media.Chapters ?? 0,
                    Volumes = media.Volumes ?? 0,
                    HasVolumes = media.Volumes.HasValue && media.Volumes.Value > 0,
                    ProviderId = providerId,
                    ProviderName = Name,
                    ProviderUrl = $"https://anilist.co/manga/{providerId}",
                    CoverUrl = coverUrl,
                    CoverPath = !string.IsNullOrEmpty(localCoverPath) ? localCoverPath : coverUrl,
                    LastModified = DateTime.Now
                };

                return metadata;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error fetching metadata: {ex.Message}", LogLevel.Error, "AniList");
                return CreateDefaultMetadata(providerId);
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

                    if (!response.IsSuccessStatusCode)
                    {
                        Logger.Log($"Failed to fetch cover image: {response.StatusCode}", LogLevel.Warning, "AniList");
                        return response;
                    }

                    return response;
                });

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error fetching cover image: {ex.Message}", LogLevel.Error, "AniList");
                return null;
            }
        }

        private string GetBestTitle(JsonNode? titles)
        {
            if (titles == null)
                return "Unknown Title";

            var english = titles["english"]?.GetValue<string>() ?? string.Empty;
            if (!string.IsNullOrEmpty(english))
                return english;

            var romaji = titles["romaji"]?.GetValue<string>() ?? string.Empty;
            if (!string.IsNullOrEmpty(romaji))
                return romaji;

            var native = titles["native"]?.GetValue<string>() ?? string.Empty;
            if (!string.IsNullOrEmpty(native))
                return native;

            return "Unknown Title";
        }

        private string CleanDescription(string? description)
        {
            if (string.IsNullOrEmpty(description))
                return string.Empty;

            // Remove HTML tags
            description = Regex.Replace(description, "<.*?>", string.Empty);
            
            // Replace escaped characters
            description = description.Replace("&nbsp;", " ")
                                   .Replace("&amp;", "&")
                                   .Replace("&lt;", "<")
                                   .Replace("&gt;", ">");

            return description;
        }

        private SeriesMetadata CreateDefaultMetadata(string providerId)
        {
            return new SeriesMetadata
            {
                Series = "Unknown Series",
                LocalizedSeries = string.Empty,
                Summary = "No description available",
                Status = "Unknown",
                Count = 0,
                Volumes = 0,
                ReleaseYear = null,
                CoverPath = string.Empty,
                ProviderId = providerId,
                ProviderName = Name,
                ProviderUrl = !string.IsNullOrEmpty(providerId) && int.TryParse(providerId, out _) 
                    ? $"https://anilist.co/manga/{providerId}" 
                    : string.Empty,
                LastModified = DateTime.Now,
                HasMetadata = false,
                Genres = new List<string>(),
                Tags = new List<string>()
            };
        }

        private SeriesMetadata MapToSeriesMetadata(Media media)
        {
            if (media == null)
                throw new ArgumentNullException(nameof(media));

            var metadata = new SeriesMetadata
            {
                Series = media.Title.English ?? media.Title.Romaji ?? media.Title.Native ?? "Unknown",
                Description = media.Description,
                Year = media.StartDate?.Year ?? 0,
                Status = MapStatus(media.Status),
                Genres = media.Genres.Where(g => !string.IsNullOrEmpty(g)).ToList(),
                Tags = media.Tags.Where(t => t?.Name != null).Select(t => t.Name).ToList(),
                ProviderId = media.Id.ToString(),
                ProviderName = Name,
                ProviderUrl = $"https://anilist.co/manga/{media.Id}",
                CoverUrl = media.CoverImage?.Large ?? string.Empty
            };

            return metadata;
        }

        private string MapStatus(string status)
        {
            return status.ToUpper() switch
            {
                "FINISHED" => "Completed",
                "RELEASING" => "Ongoing",
                "NOT_YET_RELEASED" => "Not Released",
                "CANCELLED" => "Cancelled",
                "HIATUS" => "Hiatus",
                _ => "Unknown"
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
                    Logger.Log($"Request failed, retrying in {delay}ms: {ex.Message}", LogLevel.Warning, "AniList");
                    await Task.Delay((int)delay);
                }
                finally
                {
                    _rateLimiter.Release();
                }
            }

            throw new Exception($"Failed after {MaxRetries} retries");
        }

        private async Task<T> ExecuteGraphQLRequestAsync<T>(string query, object variables) where T : class
        {
            var requestContent = new
            {
                query = query,
                variables = variables
            };

            var jsonContent = JsonSerializer.Serialize(requestContent);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(BaseUrl, content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var id = ((dynamic)variables).id;
            Logger.Log($"Received metadata response for ID: {id}", LogLevel.Info, "AniList");

            return JsonSerializer.Deserialize<T>(responseContent, _jsonOptions) ?? 
                throw new InvalidOperationException("Failed to deserialize response");
        }

        private async Task HandleRateLimitsAsync(HttpResponseMessage response)
        {
            var retryAfter = response.Headers.RetryAfter;
            if (retryAfter != null)
            {
                var delay = retryAfter.Delta?.TotalMilliseconds ?? RateLimitDelay;
                await Task.Delay((int)delay);
            }
        }

        private async Task<string> HandleCoverDownloadAsync(string seriesPath, string coverUrl)
        {
            try
            {
                Logger.Log($"Downloading cover from {coverUrl}", LogLevel.Info, "AniList");
                
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
                                Logger.Log($"Deleted old cover file: {file}", LogLevel.Info, "AniList");
                            }
                            catch (Exception ex)
                            {
                                Logger.Log($"Error deleting old cover file {file}: {ex.Message}", LogLevel.Error, "AniList");
                            }
                        }
                    }
                    
                    // Save the new cover
                    await File.WriteAllBytesAsync(coverPath, coverData);
                    Logger.Log($"Saved cover to {coverPath}", LogLevel.Info, "AniList");
                    
                    // Update first chapter if it exists
                    var cbzFiles = Directory.GetFiles(seriesPath, "*.cbz");
                    if (cbzFiles.Length > 0)
                    {
                        var firstChapter = cbzFiles.OrderBy(f => f).First();
                        var scanner = new LibraryScanner(_settingsService);
                        await scanner.InsertCoverIntoChapterAsync(firstChapter, coverPath);
                    }
                    
                    return coverPath;
                }
                
                Logger.Log("Failed to download cover image", LogLevel.Warning, "AniList");
                return string.Empty;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error handling cover download: {ex.Message}", LogLevel.Error, "AniList");
                return string.Empty;
            }
        }

        private string GetBestTitle(AniListTitle? title)
        {
            if (title == null) return "Unknown";
            return title.English ?? title.Romaji ?? title.Native ?? "Unknown";
        }

        private string ConvertStatus(string? status)
        {
            if (string.IsNullOrEmpty(status)) return "Unknown";
            
            return status.ToLower() switch
            {
                "finished" => "Completed",
                "releasing" => "Ongoing",
                "not_yet_released" => "Not Released",
                "cancelled" => "Cancelled",
                "hiatus" => "Hiatus",
                _ => "Unknown"
            };
        }

        public void Dispose()
        {
            _httpClient.Dispose();
            _rateLimiter.Dispose();
        }
    }
}