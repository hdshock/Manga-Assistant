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
using MangaAssistant.Core.Services;
using MangaAssistant.Common.Logging;

namespace MangaAssistant.Infrastructure.Services.Metadata
{
    public class MangaDexMetadataProvider : IMetadataProvider
    {
        private readonly HttpClient _httpClient;
        private readonly MangaDexScraper _scraper;
        private const string BaseUrl = "https://api.mangadex.org";
        private const string MangaUrl = "https://mangadex.org/title";
        private const string CoverUrl = "https://uploads.mangadex.org/covers";

        public string Name => "MangaDex";

        public MangaDexMetadataProvider()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MangaAssistant/1.0");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            
            _scraper = new MangaDexScraper();
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

            Debug.WriteLine($"[MangaDex] SearchAsync called with query: '{query}'");
            Logger.Log($"[MangaDex] SearchAsync called with query: '{query}'", LogLevel.Info);

            try
            {
                // First try using the API
                var apiResults = await SearchWithApiAsync(query);
                
                if (apiResults.Any())
                {
                    Debug.WriteLine($"[MangaDex] API search successful, found {apiResults.Count()} results");
                    Logger.Log($"[MangaDex] API search successful, found {apiResults.Count()} results", LogLevel.Info);
                    return apiResults;
                }
                
                // If API returns no results, try using the scraper
                Debug.WriteLine("[MangaDex] API search returned no results, trying scraper");
                Logger.Log("[MangaDex] API search returned no results, trying scraper", LogLevel.Info);
                
                var scraperResults = await _scraper.SearchAsync(query);
                Debug.WriteLine($"[MangaDex] Scraper search found {scraperResults.Count()} results");
                Logger.Log($"[MangaDex] Scraper search found {scraperResults.Count()} results", LogLevel.Info);
                
                return scraperResults;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MangaDex] Error in SearchAsync: {ex.Message}");
                Debug.WriteLine($"[MangaDex] Stack trace: {ex.StackTrace}");
                Logger.LogException(ex, "[MangaDex] SearchAsync");
                
                // Try scraper as fallback for any API errors
                try
                {
                    Debug.WriteLine("[MangaDex] Trying scraper as fallback after API error");
                    Logger.Log("[MangaDex] Trying scraper as fallback after API error", LogLevel.Warning);
                    
                    var scraperResults = await _scraper.SearchAsync(query);
                    Debug.WriteLine($"[MangaDex] Fallback scraper search found {scraperResults.Count()} results");
                    Logger.Log($"[MangaDex] Fallback scraper search found {scraperResults.Count()} results", LogLevel.Info);
                    
                    return scraperResults;
                }
                catch (Exception scraperEx)
                {
                    Debug.WriteLine($"[MangaDex] Scraper fallback also failed: {scraperEx.Message}");
                    Logger.LogException(scraperEx, "[MangaDex] SearchWithScraperAsync");
                    return Enumerable.Empty<SeriesSearchResult>();
                }
            }
        }
        
        private async Task<IEnumerable<SeriesSearchResult>> SearchWithApiAsync(string query)
        {
            // For short queries (3 characters or less), we need a special approach
            bool isShortQuery = !string.IsNullOrEmpty(query) && query.Length <= 3;
            
            // Try direct ID lookup first if the query looks like it might be an ID
            if (IsValidUuid(query))
            {
                Logger.Log($"[MangaDex] Query appears to be a UUID, trying direct lookup: {query}", LogLevel.Info);
                var directResults = await GetMangaByIdAsync(query);
                if (directResults.Any())
                {
                    Logger.Log($"[MangaDex] Direct ID lookup successful for: {query}", LogLevel.Info);
                    return directResults;
                }
            }

            // Try title search first
            var titleResults = await SearchByTitleAsync(query, isShortQuery);
            if (titleResults.Any())
            {
                Logger.Log($"[MangaDex] Title search successful for: {query}, found {titleResults.Count()} results", LogLevel.Info);
                return titleResults;
            }

            // If title search fails and it's a short query, try a more comprehensive search
            if (isShortQuery)
            {
                Logger.Log($"[MangaDex] Title search failed for short query: {query}, trying comprehensive search", LogLevel.Info);
                return await ComprehensiveSearchAsync(query);
            }

            Logger.Log($"[MangaDex] No results found with API for: {query}", LogLevel.Warning);
            return new List<SeriesSearchResult>();
        }

        private async Task<IEnumerable<SeriesSearchResult>> GetMangaByIdAsync(string id)
        {
            try
            {
                var url = $"{BaseUrl}/manga/{id}?includes[]=cover_art";
                Logger.Log($"[MangaDex] Direct manga lookup URL: {url}", LogLevel.Info);
                
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    Logger.Log($"[MangaDex] Direct manga lookup failed with status: {response.StatusCode}", LogLevel.Warning);
                    return new List<SeriesSearchResult>();
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonSerializer.Deserialize<JsonNode>(responseContent);
                
                var manga = jsonResponse?["data"];
                if (manga == null)
                {
                    Logger.Log($"[MangaDex] Direct manga lookup returned no data", LogLevel.Warning);
                    return new List<SeriesSearchResult>();
                }

                var results = new List<SeriesSearchResult>();
                var result = ParseMangaNode(manga);
                if (result != null)
                {
                    results.Add(result);
                    Logger.Log($"[MangaDex] Direct manga lookup successful: {result.Title} (ID: {result.ProviderId})", LogLevel.Info);
                }

                return results;
            }
            catch (Exception ex)
            {
                Logger.Log($"[MangaDex] Error during direct manga lookup: {ex.Message}", LogLevel.Error);
                return new List<SeriesSearchResult>();
            }
        }

        private async Task<IEnumerable<SeriesSearchResult>> SearchByTitleAsync(string query, bool isShortQuery)
        {
            try
            {
                var searchUrl = $"{BaseUrl}/manga";
                var queryParams = new Dictionary<string, string>();
                
                // Add title parameter
                queryParams.Add("title", query);
                
                // Set higher limit for short queries
                queryParams.Add("limit", isShortQuery ? "30" : "20");
                
                // Order by relevance
                queryParams.Add("order[relevance]", "desc");
                
                // Include cover art
                queryParams.Add("includes[]", "cover_art");
                
                // Include all content ratings
                queryParams.Add("contentRating[]", "safe");
                queryParams.Add("contentRating[]", "suggestive");
                queryParams.Add("contentRating[]", "erotica");
                
                var requestUrl = BuildUrlWithQueryParams(searchUrl, queryParams);
                Logger.Log($"[MangaDex] Title search URL: {requestUrl}", LogLevel.Info);
                
                var response = await _httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonSerializer.Deserialize<JsonNode>(responseContent);
                var results = new List<SeriesSearchResult>();

                var mangaList = jsonResponse?["data"]?.AsArray();
                if (mangaList == null || mangaList.Count == 0)
                {
                    Logger.Log($"[MangaDex] No results found in title search for: {query}", LogLevel.Warning);
                    return results;
                }

                foreach (var manga in mangaList)
                {
                    var result = ParseMangaNode(manga);
                    if (result != null)
                    {
                        results.Add(result);
                    }
                }

                Logger.Log($"[MangaDex] Title search returned {results.Count} results for: {query}", LogLevel.Info);
                return results;
            }
            catch (Exception ex)
            {
                Logger.Log($"[MangaDex] Error during title search: {ex.Message}", LogLevel.Error);
                return new List<SeriesSearchResult>();
            }
        }

        private async Task<IEnumerable<SeriesSearchResult>> ComprehensiveSearchAsync(string query)
        {
            try
            {
                Logger.Log($"[MangaDex] Starting comprehensive search for: {query}", LogLevel.Info);
                
                var searchUrl = $"{BaseUrl}/manga";
                var queryParams = new Dictionary<string, string>();
                
                // Try with authorOrArtist parameter instead of title
                queryParams.Add("authorOrArtist", query);
                
                // Set high limit
                queryParams.Add("limit", "50");
                
                // Include cover art and related entities
                queryParams.Add("includes[]", "cover_art");
                queryParams.Add("includes[]", "author");
                queryParams.Add("includes[]", "artist");
                
                // Include all content ratings
                queryParams.Add("contentRating[]", "safe");
                queryParams.Add("contentRating[]", "suggestive");
                queryParams.Add("contentRating[]", "erotica");
                
                var requestUrl = BuildUrlWithQueryParams(searchUrl, queryParams);
                Logger.Log($"[MangaDex] Comprehensive search URL: {requestUrl}", LogLevel.Info);
                
                var response = await _httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonSerializer.Deserialize<JsonNode>(responseContent);
                var results = new List<SeriesSearchResult>();

                var mangaList = jsonResponse?["data"]?.AsArray();
                if (mangaList == null || mangaList.Count == 0)
                {
                    Logger.Log($"[MangaDex] No results found in comprehensive search for: {query}", LogLevel.Warning);
                    
                    // Try one more approach - search by exact match in all fields
                    return await FinalFallbackSearchAsync(query);
                }

                foreach (var manga in mangaList)
                {
                    var result = ParseMangaNode(manga);
                    if (result != null)
                    {
                        results.Add(result);
                    }
                }

                Logger.Log($"[MangaDex] Comprehensive search returned {results.Count} results for: {query}", LogLevel.Info);
                return results;
            }
            catch (Exception ex)
            {
                Logger.Log($"[MangaDex] Error during comprehensive search: {ex.Message}", LogLevel.Error);
                return new List<SeriesSearchResult>();
            }
        }

        private async Task<IEnumerable<SeriesSearchResult>> FinalFallbackSearchAsync(string query)
        {
            try
            {
                Logger.Log($"[MangaDex] Starting final fallback search for: {query}", LogLevel.Info);
                
                // For final fallback, try a different approach - use title parameter but with exact match
                var searchUrl = $"{BaseUrl}/manga";
                var queryParams = new Dictionary<string, string>();
                
                // Add title parameter with exact match
                queryParams.Add("title", query);
                
                // Use highest limit
                queryParams.Add("limit", "100");
                
                // Include cover art and related entities
                queryParams.Add("includes[]", "cover_art");
                queryParams.Add("includes[]", "author");
                queryParams.Add("includes[]", "artist");
                
                // Include all content ratings
                queryParams.Add("contentRating[]", "safe");
                queryParams.Add("contentRating[]", "suggestive");
                queryParams.Add("contentRating[]", "erotica");
                
                var requestUrl = BuildUrlWithQueryParams(searchUrl, queryParams);
                Logger.Log($"[MangaDex] Final fallback search URL: {requestUrl}", LogLevel.Info);
                
                var response = await _httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                
                // Log the entire response for debugging
                Logger.Log($"[MangaDex] Final fallback response: {responseContent}", LogLevel.Debug);

                var jsonResponse = JsonSerializer.Deserialize<JsonNode>(responseContent);
                var results = new List<SeriesSearchResult>();

                var mangaList = jsonResponse?["data"]?.AsArray();
                if (mangaList == null || mangaList.Count == 0)
                {
                    Logger.Log($"[MangaDex] No results found in final fallback search for: {query}", LogLevel.Warning);
                    return results;
                }
                
                foreach (var manga in mangaList)
                {
                    var result = ParseMangaNode(manga);
                    if (result != null)
                    {
                        // For final fallback, do additional title matching to find exact matches
                        var attributes = manga["attributes"];
                        if (attributes != null)
                        {
                            var titleObj = attributes["title"];
                            var titles = new Dictionary<string, string>();
                            
                            if (titleObj != null)
                            {
                                foreach (var property in titleObj.AsObject())
                                {
                                    titles[property.Key] = property.Value?.GetValue<string>() ?? string.Empty;
                                }
                            }
                            
                            // Check if any title contains our query as a standalone word
                            bool foundExactMatch = false;
                            foreach (var title in titles.Values)
                            {
                                if (title.Equals(query, StringComparison.OrdinalIgnoreCase) || 
                                    title.Contains($" {query} ", StringComparison.OrdinalIgnoreCase) ||
                                    title.StartsWith($"{query} ", StringComparison.OrdinalIgnoreCase) ||
                                    title.EndsWith($" {query}", StringComparison.OrdinalIgnoreCase))
                                {
                                    foundExactMatch = true;
                                    break;
                                }
                            }
                            
                            if (foundExactMatch)
                            {
                                results.Add(result);
                                Logger.Log($"[MangaDex] Found exact match in final fallback: {result.Title} (ID: {result.ProviderId})", LogLevel.Info);
                            }
                        }
                    }
                }

                Logger.Log($"[MangaDex] Final fallback search returned {results.Count} results for: {query}", LogLevel.Info);
                return results;
            }
            catch (Exception ex)
            {
                Logger.Log($"[MangaDex] Error during final fallback search: {ex.Message}", LogLevel.Error);
                return new List<SeriesSearchResult>();
            }
        }

        private SeriesSearchResult ParseMangaNode(JsonNode manga)
        {
            try
            {
                var id = manga?["id"]?.GetValue<string>();
                if (string.IsNullOrEmpty(id))
                {
                    Logger.Log("[MangaDex] Skipping result with missing ID", LogLevel.Warning);
                    return null;
                }

                var attributes = manga?["attributes"];
                if (attributes == null)
                {
                    Logger.Log($"[MangaDex] Skipping result with ID {id} due to missing attributes", LogLevel.Warning);
                    return null;
                }

                var titleObj = attributes["title"];
                var titles = new Dictionary<string, string>();
                
                if (titleObj != null)
                {
                    foreach (var property in titleObj.AsObject())
                    {
                        titles[property.Key] = property.Value?.GetValue<string>() ?? string.Empty;
                    }
                }

                var title = GetBestTitle(titles);
                if (string.IsNullOrEmpty(title))
                {
                    Logger.Log($"[MangaDex] Skipping result with ID {id} due to missing title", LogLevel.Warning);
                    return null;
                }

                // Get description
                var descriptionObj = attributes["description"];
                var descriptions = new Dictionary<string, string>();
                
                if (descriptionObj != null)
                {
                    foreach (var property in descriptionObj.AsObject())
                    {
                        descriptions[property.Key] = property.Value?.GetValue<string>() ?? string.Empty;
                    }
                }
                
                var description = GetBestDescription(descriptions);

                // Get status
                var status = attributes["status"]?.GetValue<string>() ?? "unknown";
                status = ConvertStatus(status);

                // Get year
                var yearString = attributes["year"]?.GetValue<string>();
                int? year = null;
                if (!string.IsNullOrEmpty(yearString) && int.TryParse(yearString, out int parsedYear))
                {
                    year = parsedYear;
                }

                // Get cover URL
                var coverUrl = string.Empty;
                var relationships = manga["relationships"]?.AsArray();
                if (relationships != null)
                {
                    foreach (var rel in relationships)
                    {
                        if (rel?["type"]?.GetValue<string>() == "cover_art")
                        {
                            var fileName = rel["attributes"]?["fileName"]?.GetValue<string>();
                            if (!string.IsNullOrEmpty(fileName))
                            {
                                coverUrl = $"{CoverUrl}/{id}/{fileName}.512.jpg";
                                break;
                            }
                        }
                    }
                }

                var result = new SeriesSearchResult
                {
                    ProviderId = id,
                    Title = title,
                    Description = description,
                    CoverUrl = coverUrl,
                    Status = status,
                    ReleaseYear = year,
                    Url = $"{MangaUrl}/{id}"
                };

                return result;
            }
            catch (Exception ex)
            {
                Logger.Log($"[MangaDex] Error parsing manga node: {ex.Message}", LogLevel.Error);
                return null;
            }
        }

        public async Task<SeriesMetadata> GetMetadataAsync(string providerId)
        {
            try
            {
                Logger.Log($"[MangaDex] Getting metadata for ID: {providerId}", LogLevel.Info);
                
                // Try the API first
                var apiMetadata = await GetMetadataWithApiAsync(providerId);
                
                // If we got a valid title from the API, return it
                if (!string.IsNullOrEmpty(apiMetadata.Series) && apiMetadata.Series != "Unknown")
                {
                    Logger.Log($"[MangaDex] API metadata successful for: {providerId}", LogLevel.Info);
                    return apiMetadata;
                }
                
                // If API fails, fall back to scraper
                Logger.Log($"[MangaDex] API metadata failed for: {providerId}, falling back to scraper", LogLevel.Warning);
                var scraperMetadata = await _scraper.GetMetadataAsync(providerId);
                
                if (!string.IsNullOrEmpty(scraperMetadata.Series) && scraperMetadata.Series != "Unknown")
                {
                    Logger.Log($"[MangaDex] Scraper metadata successful for: {providerId}", LogLevel.Info);
                    return scraperMetadata;
                }
                
                // If both API and scraper fail, try to find by search
                Logger.Log($"[MangaDex] No metadata found for: {providerId} with either API or scraper, trying search", LogLevel.Warning);
                
                // Use the ID as a search term
                var searchResults = await SearchAsync(providerId);
                var firstResult = searchResults.FirstOrDefault();
                
                if (firstResult != null)
                {
                    Logger.Log($"[MangaDex] Found metadata via search for: {providerId}", LogLevel.Info);
                    return ConvertToSeriesMetadata(firstResult);
                }
                
                Logger.Log($"[MangaDex] No metadata found for: {providerId} with any method", LogLevel.Warning);
                return CreateDefaultMetadata(providerId);
            }
            catch (Exception ex)
            {
                Logger.Log($"[MangaDex] Error getting metadata for '{providerId}': {ex.Message}", LogLevel.Error);
                
                // Try scraper as a last resort if there was an exception
                try
                {
                    Logger.Log($"[MangaDex] Trying scraper after exception for: {providerId}", LogLevel.Info);
                    return await _scraper.GetMetadataAsync(providerId);
                }
                catch (Exception scraperEx)
                {
                    Logger.Log($"[MangaDex] Scraper also failed: {scraperEx.Message}", LogLevel.Error);
                    return CreateDefaultMetadata(providerId);
                }
            }
        }
        
        private async Task<SeriesMetadata> GetMetadataWithApiAsync(string providerId)
        {
            try
            {
                var url = $"{BaseUrl}/manga/{providerId}?includes[]=cover_art&includes[]=author&includes[]=artist";
                Logger.Log($"[MangaDex] Metadata URL: {url}", LogLevel.Info);
                
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonSerializer.Deserialize<JsonNode>(responseContent);
                
                var data = jsonResponse?["data"];
                if (data == null)
                {
                    Logger.Log($"[MangaDex] No data found in metadata response for: {providerId}", LogLevel.Warning);
                    return CreateDefaultMetadata(providerId);
                }

                var attributes = data["attributes"];
                
                // Get title
                var titleObj = attributes?["title"];
                var titles = new Dictionary<string, string>();
                
                if (titleObj != null)
                {
                    foreach (var property in titleObj.AsObject())
                    {
                        titles[property.Key] = property.Value?.GetValue<string>() ?? string.Empty;
                    }
                }
                
                var title = GetBestTitle(titles);
                
                // Get description
                var descriptionObj = attributes?["description"];
                var descriptions = new Dictionary<string, string>();
                
                if (descriptionObj != null)
                {
                    foreach (var property in descriptionObj.AsObject())
                    {
                        descriptions[property.Key] = property.Value?.GetValue<string>() ?? string.Empty;
                    }
                }
                
                var description = GetBestDescription(descriptions);
                
                // Get status
                var status = attributes?["status"]?.GetValue<string>() ?? "unknown";
                status = ConvertStatus(status);
                
                // Get year
                var yearString = attributes?["year"]?.GetValue<string>();
                int? year = null;
                if (!string.IsNullOrEmpty(yearString) && int.TryParse(yearString, out int parsedYear))
                {
                    year = parsedYear;
                }
                
                // Get cover URL
                var coverUrl = string.Empty;
                var relationships = data["relationships"]?.AsArray();
                if (relationships != null)
                {
                    foreach (var rel in relationships)
                    {
                        if (rel?["type"]?.GetValue<string>() == "cover_art")
                        {
                            var fileName = rel["attributes"]?["fileName"]?.GetValue<string>();
                            if (!string.IsNullOrEmpty(fileName))
                            {
                                coverUrl = $"{CoverUrl}/{providerId}/{fileName}.512.jpg";
                                break;
                            }
                        }
                    }
                }
                
                // Get author
                var author = string.Empty;
                if (relationships != null)
                {
                    foreach (var rel in relationships)
                    {
                        if (rel?["type"]?.GetValue<string>() == "author")
                        {
                            author = rel["attributes"]?["name"]?.GetValue<string>() ?? string.Empty;
                            break;
                        }
                    }
                }
                
                var metadata = new SeriesMetadata
                {
                    Series = title,
                    Summary = description,
                    Description = description,
                    CoverPath = coverUrl,
                    Status = status,
                    Author = author,
                    ReleaseYear = year,
                    ProviderId = providerId,
                    ProviderName = Name,
                    ProviderUrl = $"{MangaUrl}/{providerId}",
                    HasMetadata = true,
                    LastModified = DateTime.Now
                };
                
                Logger.Log($"[MangaDex] Successfully retrieved metadata for: {title}", LogLevel.Info);
                return metadata;
            }
            catch (Exception ex)
            {
                Logger.Log($"[MangaDex] Error getting metadata with API: {ex.Message}", LogLevel.Error);
                return CreateDefaultMetadata(providerId);
            }
        }

        public async Task<byte[]?> GetCoverImageAsync(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                Logger.Log("[MangaDex] Empty URL provided for cover image", LogLevel.Warning);
                return null;
            }

            try
            {
                Logger.Log($"[MangaDex] Fetching cover image from: {url}", LogLevel.Info);
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var imageBytes = await response.Content.ReadAsByteArrayAsync();
                    Logger.Log($"[MangaDex] Successfully fetched cover image ({imageBytes.Length} bytes)", LogLevel.Info);
                    return imageBytes;
                }
                else
                {
                    Logger.Log($"[MangaDex] Failed to fetch cover image: {response.StatusCode}", LogLevel.Error);
                    return null;
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "[MangaDex] GetCoverImageAsync");
                return null;
            }
        }

        private string GetBestTitle(Dictionary<string, string> titles)
        {
            // Prefer English title, then Japanese, then the first available
            if (titles.ContainsKey("en") && !string.IsNullOrEmpty(titles["en"]))
                return titles["en"];
            
            if (titles.ContainsKey("ja") && !string.IsNullOrEmpty(titles["ja"]))
                return titles["ja"];
            
            if (titles.ContainsKey("jp") && !string.IsNullOrEmpty(titles["jp"]))
                return titles["jp"];
            
            if (titles.Count > 0)
                return titles.Values.FirstOrDefault(v => !string.IsNullOrEmpty(v)) ?? "Unknown Title";
            
            return "Unknown Title";
        }

        private string GetBestDescription(Dictionary<string, string> descriptions)
        {
            if (descriptions == null || descriptions.Count == 0)
                return string.Empty;

            // Prioritize English description
            if (descriptions.ContainsKey("en") && !string.IsNullOrEmpty(descriptions["en"]))
                return descriptions["en"];

            // Fall back to any non-empty description
            foreach (var desc in descriptions.Values)
            {
                if (!string.IsNullOrEmpty(desc))
                    return desc;
            }

            return string.Empty;
        }

        private string ConvertStatus(string mangaDexStatus)
        {
            return mangaDexStatus?.ToLower() switch
            {
                "completed" => "Completed",
                "ongoing" => "Ongoing",
                "cancelled" => "Cancelled",
                "hiatus" => "Hiatus",
                _ => "Unknown"
            };
        }

        private string BuildUrlWithQueryParams(string baseUrl, Dictionary<string, string> queryParams)
        {
            if (queryParams == null || queryParams.Count == 0)
                return baseUrl;

            var queryParts = new List<string>();
            
            foreach (var param in queryParams)
            {
                // For array parameters (ending with []), don't escape the brackets
                string key = param.Key;
                string value = param.Value;
                
                if (key.EndsWith("[]"))
                {
                    // Split the key to escape the name part but keep the brackets
                    string name = key.Substring(0, key.Length - 2);
                    queryParts.Add($"{Uri.EscapeDataString(name)}[]={Uri.EscapeDataString(value)}");
                }
                else
                {
                    queryParts.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}");
                }
            }
            
            var queryString = string.Join("&", queryParts);
            return $"{baseUrl}?{queryString}";
        }

        private bool IsValidUuid(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            // Check if the input matches the UUID format
            Guid guid;
            return Guid.TryParse(input, out guid);
        }

        private SeriesMetadata CreateDefaultMetadata(string providerId)
        {
            return new SeriesMetadata
            {
                Series = "Unknown",
                Summary = string.Empty,
                Description = string.Empty,
                CoverPath = string.Empty,
                Status = "Unknown",
                Author = string.Empty,
                ProviderId = providerId,
                ProviderName = Name,
                ProviderUrl = $"{MangaUrl}/{providerId}",
                HasMetadata = false,
                LastModified = DateTime.Now
            };
        }

        /// <summary>
        /// Converts a SeriesSearchResult to a SeriesMetadata object
        /// </summary>
        private SeriesMetadata ConvertToSeriesMetadata(SeriesSearchResult searchResult)
        {
            if (searchResult == null)
                return CreateDefaultMetadata(string.Empty);
                
            return new SeriesMetadata
            {
                Series = searchResult.Title,
                Summary = searchResult.Description,
                Description = searchResult.Description,
                CoverPath = searchResult.CoverUrl,
                Status = searchResult.Status,
                ReleaseYear = searchResult.ReleaseYear,
                ProviderId = searchResult.ProviderId,
                ProviderName = Name,
                ProviderUrl = searchResult.Url,
                HasMetadata = true,
                LastModified = DateTime.Now
            };
        }
    }
}
