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
        public Title? Title { get; set; }
        public string? Description { get; set; }
        public StartDate? StartDate { get; set; }
        public string? Status { get; set; }
        public List<string>? Genres { get; set; }
        public List<Tag>? Tags { get; set; }
        public CoverImage? CoverImage { get; set; }
    }

    public class Title
    {
        public string? English { get; set; }
        public string? Romaji { get; set; }
        public string? Native { get; set; }
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
    }

    public class AniListMetadataProvider : IMetadataProvider, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly SemaphoreSlim _rateLimiter;
        private readonly JsonSerializerOptions _jsonOptions;
        private const string BaseUrl = "https://graphql.anilist.co";
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

        public AniListMetadataProvider()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
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

            // Special case for known problematic IDs
            if (string.Equals(providerId, "GUL", StringComparison.OrdinalIgnoreCase))
            {
                Logger.Log($"[AniList] Special handling for known problematic ID: {providerId}", LogLevel.Warning);
                return CreateDefaultMetadata(providerId);
            }

            try
            {
                Logger.Log($"[AniList] Getting metadata for ID: {providerId}", LogLevel.Info);

                // Validate providerId is a valid integer
                if (!int.TryParse(providerId, out int id))
                {
                    Logger.Log($"[AniList] Invalid provider ID (not an integer): {providerId}", LogLevel.Error);
                    return CreateDefaultMetadata(providerId);
                }

                var variables = new { id = id };
                var requestContent = new
                {
                    query = MediaQuery,
                    variables = variables
                };

                var jsonContent = JsonSerializer.Serialize(requestContent);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(BaseUrl, content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                Logger.Log($"[AniList] Received metadata response for ID: {providerId}", LogLevel.Info);

                var jsonResponse = JsonSerializer.Deserialize<JsonNode>(responseContent);
                var media = jsonResponse?["data"]?["Media"];

                if (media == null)
                {
                    Logger.Log($"[AniList] No metadata found for ID: {providerId}", LogLevel.Warning);
                    return CreateDefaultMetadata(providerId);
                }

                // Extract metadata
                var titles = media["title"];
                var titleText = GetBestTitle(titles);
                var localizedTitle = titles?["native"]?.GetValue<string>();

                var description = media["description"]?.GetValue<string>();
                description = CleanDescription(description);

                var statusNode = media["status"];
                var status = statusNode != null ? statusNode.GetValue<string>() : "Unknown";

                var chaptersNode = media["chapters"];
                int? chapterCount = chaptersNode != null && chaptersNode.GetValue<JsonElement?>() == null ? null : chaptersNode.GetValue<int?>();

                var volumesNode = media["volumes"];
                int? volumeCount = volumesNode != null && volumesNode.GetValue<JsonElement?>() == null ? null : volumesNode.GetValue<int?>();

                var startDateNode = media["startDate"];
                var yearNode = startDateNode?["year"];
                int? releaseYear = yearNode != null && yearNode.GetValue<JsonElement?>() == null ? null : yearNode.GetValue<int?>();

                var coverUrl = media["coverImage"]?["extraLarge"]?.GetValue<string>() ?? 
                               media["coverImage"]?["large"]?.GetValue<string>();

                // Extract genres
                var genres = new List<string>();
                var genresArray = media["genres"]?.AsArray();
                if (genresArray != null)
                {
                    foreach (var genre in genresArray)
                    {
                        if (genre != null)
                        {
                            genres.Add(genre.GetValue<string>());
                        }
                    }
                }

                // Extract tags
                var tags = new List<string>();
                var tagsArray = media["tags"]?.AsArray();
                if (tagsArray != null)
                {
                    foreach (var tag in tagsArray)
                    {
                        var tagName = tag?["name"]?.GetValue<string>();
                        if (!string.IsNullOrEmpty(tagName))
                        {
                            tags.Add(tagName);
                        }
                    }
                }

                var metadata = new SeriesMetadata
                {
                    Series = titleText,
                    LocalizedSeries = localizedTitle,
                    Summary = description,
                    Status = status,
                    Count = chapterCount ?? 0,
                    Volumes = volumeCount ?? 0,
                    ReleaseYear = releaseYear,
                    CoverPath = coverUrl,
                    ProviderId = providerId,
                    ProviderName = Name,
                    ProviderUrl = $"https://anilist.co/manga/{providerId}",
                    LastModified = DateTime.Now,
                    HasMetadata = true,
                    Genres = genres,
                    Tags = tags
                };

                Logger.Log($"[AniList] Successfully retrieved metadata for: {titleText} (ID: {providerId})", LogLevel.Info);
                return metadata;
            }
            catch (Exception ex)
            {
                Logger.Log($"[AniList] Error getting metadata for ID '{providerId}': {ex.Message}", LogLevel.Error);
                Logger.Log($"[AniList] Stack trace: {ex.StackTrace}", LogLevel.Error);
                return CreateDefaultMetadata(providerId);
            }
        }

        public async Task<byte[]?> GetCoverImageAsync(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                Logger.Log("[AniList] Empty URL provided for cover image", LogLevel.Warning);
                return null;
            }

            try
            {
                Logger.Log($"[AniList] Fetching cover image from: {url}", LogLevel.Info);
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var imageBytes = await response.Content.ReadAsByteArrayAsync();
                    Logger.Log($"[AniList] Successfully fetched cover image ({imageBytes.Length} bytes)", LogLevel.Info);
                    return imageBytes;
                }
                else
                {
                    Logger.Log($"[AniList] Failed to fetch cover image: {response.StatusCode}", LogLevel.Error);
                    return null;
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "[AniList] GetCoverImageAsync");
                return null;
            }
        }

        private string GetBestTitle(JsonNode? titles)
        {
            if (titles == null)
                return "Unknown Title";

            // Prefer English title, then romaji, then native
            var english = titles["english"]?.GetValue<string>();
            if (!string.IsNullOrEmpty(english))
                return english;

            var romaji = titles["romaji"]?.GetValue<string>();
            if (!string.IsNullOrEmpty(romaji))
                return romaji;

            var native = titles["native"]?.GetValue<string>();
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
                Series = media.Title?.English ?? media.Title?.Romaji ?? media.Title?.Native ?? "Unknown",
                Description = media.Description ?? string.Empty,
                Year = media.StartDate?.Year ?? 0,
                Status = MapStatus(media.Status ?? "UNKNOWN"),
                Genres = media.Genres?.Where(g => g != null).ToList() ?? new List<string>(),
                Tags = media.Tags?.Where(t => t?.Name != null).Select(t => t!.Name!).ToList() ?? new List<string>(),
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

        public void Dispose()
        {
            _httpClient.Dispose();
            _rateLimiter.Dispose();
        }
    }
}