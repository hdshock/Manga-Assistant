using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using MangaAssistant.Core.Models;
using MangaAssistant.Common.Logging;

namespace MangaAssistant.Infrastructure.Services.Metadata
{
    /// <summary>
    /// A fallback scraper for MangaDex when the API fails to return results
    /// </summary>
    public class MangaDexScraper
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://mangadex.org";
        private const string SearchUrl = "https://mangadex.org/search?q={0}";
        private const string MangaUrl = "https://mangadex.org/title";
        private const string CoverUrl = "https://uploads.mangadex.org/covers";

        public MangaDexScraper()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// Scrapes search results from MangaDex website
        /// </summary>
        /// <param name="query">The search query</param>
        /// <returns>A list of search results</returns>
        public async Task<IEnumerable<SeriesSearchResult>> SearchAsync(string query)
        {
            try
            {
                Logger.Log($"[MangaDexScraper] Searching for: {query}", LogLevel.Info);
                
                var searchUrl = string.Format(SearchUrl, Uri.EscapeDataString(query));
                Logger.Log($"[MangaDexScraper] Search URL: {searchUrl}", LogLevel.Info);
                
                var response = await _httpClient.GetAsync(searchUrl);
                response.EnsureSuccessStatusCode();
                
                var html = await response.Content.ReadAsStringAsync();
                Logger.Log($"[MangaDexScraper] Received HTML response, length: {html.Length}", LogLevel.Info);
                
                return ParseSearchResults(html, query);
            }
            catch (Exception ex)
            {
                Logger.Log($"[MangaDexScraper] Error during search: {ex.Message}", LogLevel.Error);
                return new List<SeriesSearchResult>();
            }
        }

        /// <summary>
        /// Parses the HTML search results page
        /// </summary>
        private IEnumerable<SeriesSearchResult> ParseSearchResults(string html, string query)
        {
            var results = new List<SeriesSearchResult>();
            
            try
            {
                // Extract manga cards from the search results page
                var mangaCardPattern = @"<div[^>]*?class=""manga-card[^""]*""[^>]*?>(.*?)<\/div>\s*<\/div>\s*<\/div>";
                var mangaCards = Regex.Matches(html, mangaCardPattern, RegexOptions.Singleline);
                
                Logger.Log($"[MangaDexScraper] Found {mangaCards.Count} manga cards", LogLevel.Info);
                
                foreach (Match card in mangaCards)
                {
                    try
                    {
                        var cardHtml = card.Groups[1].Value;
                        
                        // Extract manga ID
                        var idPattern = @"href=""\/title\/([a-f0-9-]+)""";
                        var idMatch = Regex.Match(cardHtml, idPattern);
                        if (!idMatch.Success) continue;
                        
                        var id = idMatch.Groups[1].Value;
                        
                        // Extract title
                        var titlePattern = @"<div[^>]*?class=""manga-card-title""[^>]*?>\s*<span[^>]*?>(.*?)<\/span>";
                        var titleMatch = Regex.Match(cardHtml, titlePattern, RegexOptions.Singleline);
                        if (!titleMatch.Success) continue;
                        
                        var title = HttpUtility.HtmlDecode(titleMatch.Groups[1].Value.Trim());
                        
                        // Extract cover URL
                        var coverPattern = @"<img[^>]*?src=""([^""]+)""[^>]*?class=""rounded""";
                        var coverMatch = Regex.Match(cardHtml, coverPattern);
                        var coverUrl = coverMatch.Success ? coverMatch.Groups[1].Value : "";
                        
                        // If we have a short query, perform additional filtering
                        if (!string.IsNullOrEmpty(query) && query.Length <= 3)
                        {
                            // Check if title contains the query as a standalone word
                            if (!title.Equals(query, StringComparison.OrdinalIgnoreCase) &&
                                !title.Contains($" {query} ", StringComparison.OrdinalIgnoreCase) &&
                                !title.StartsWith($"{query} ", StringComparison.OrdinalIgnoreCase) &&
                                !title.EndsWith($" {query}", StringComparison.OrdinalIgnoreCase))
                            {
                                Logger.Log($"[MangaDexScraper] Filtering out result '{title}' for short query '{query}'", LogLevel.Debug);
                                continue;
                            }
                        }
                        
                        var result = new SeriesSearchResult
                        {
                            ProviderId = id,
                            Title = title,
                            CoverUrl = coverUrl,
                            Description = "", // We don't get description from search results
                            Status = "Unknown", // We don't get status from search results
                            ReleaseYear = null // We don't get year from search results
                        };
                        
                        results.Add(result);
                        Logger.Log($"[MangaDexScraper] Added result: {title} (ID: {id})", LogLevel.Info);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"[MangaDexScraper] Error parsing manga card: {ex.Message}", LogLevel.Error);
                    }
                }
                
                Logger.Log($"[MangaDexScraper] Returning {results.Count} results", LogLevel.Info);
                return results;
            }
            catch (Exception ex)
            {
                Logger.Log($"[MangaDexScraper] Error parsing search results: {ex.Message}", LogLevel.Error);
                return results;
            }
        }

        /// <summary>
        /// Gets metadata for a specific manga by ID
        /// </summary>
        public async Task<SeriesMetadata> GetMetadataAsync(string id)
        {
            try
            {
                Logger.Log($"[MangaDexScraper] Getting metadata for ID: {id}", LogLevel.Info);
                
                var mangaUrl = $"{MangaUrl}/{id}";
                Logger.Log($"[MangaDexScraper] Manga URL: {mangaUrl}", LogLevel.Info);
                
                var response = await _httpClient.GetAsync(mangaUrl);
                response.EnsureSuccessStatusCode();
                
                var html = await response.Content.ReadAsStringAsync();
                Logger.Log($"[MangaDexScraper] Received HTML response, length: {html.Length}", LogLevel.Info);
                
                return ParseMangaPage(html, id);
            }
            catch (Exception ex)
            {
                Logger.Log($"[MangaDexScraper] Error getting metadata: {ex.Message}", LogLevel.Error);
                return CreateDefaultMetadata(id);
            }
        }

        /// <summary>
        /// Parses the HTML manga page
        /// </summary>
        private SeriesMetadata ParseMangaPage(string html, string id)
        {
            try
            {
                // Extract title
                var titlePattern = @"<h3[^>]*?class=""mx-0""[^>]*?>(.*?)<\/h3>";
                var titleMatch = Regex.Match(html, titlePattern, RegexOptions.Singleline);
                var title = titleMatch.Success ? HttpUtility.HtmlDecode(titleMatch.Groups[1].Value.Trim()) : "";
                
                // Extract description
                var descPattern = @"<div[^>]*?class=""description[^""]*""[^>]*?>\s*<div[^>]*?>(.*?)<\/div>";
                var descMatch = Regex.Match(html, descPattern, RegexOptions.Singleline);
                var description = descMatch.Success ? HttpUtility.HtmlDecode(descMatch.Groups[1].Value.Trim()) : "";
                
                // Clean up description (remove HTML tags)
                description = Regex.Replace(description, @"<[^>]+>", "").Trim();
                
                // Extract cover URL
                var coverPattern = @"<img[^>]*?src=""([^""]+)""[^>]*?class=""rounded""";
                var coverMatch = Regex.Match(html, coverPattern);
                var coverUrl = coverMatch.Success ? coverMatch.Groups[1].Value : "";
                
                // Extract status
                var statusPattern = @"<div[^>]*?>\s*Publication\s*<\/div>\s*<div[^>]*?>\s*([^<]+)";
                var statusMatch = Regex.Match(html, statusPattern, RegexOptions.Singleline);
                var status = statusMatch.Success ? ConvertStatus(statusMatch.Groups[1].Value.Trim()) : "Unknown";
                
                // Extract year
                var yearPattern = @"<div[^>]*?>\s*Year\s*<\/div>\s*<div[^>]*?>\s*(\d{4})";
                var yearMatch = Regex.Match(html, yearPattern, RegexOptions.Singleline);
                int? year = yearMatch.Success && int.TryParse(yearMatch.Groups[1].Value, out int parsedYear) ? parsedYear : null;
                
                // Extract author
                var authorPattern = @"<div[^>]*?>\s*Author\s*<\/div>\s*<div[^>]*?>\s*<a[^>]*?>(.*?)<\/a>";
                var authorMatch = Regex.Match(html, authorPattern, RegexOptions.Singleline);
                var author = authorMatch.Success ? HttpUtility.HtmlDecode(authorMatch.Groups[1].Value.Trim()) : "";
                
                var metadata = new SeriesMetadata
                {
                    Series = title,
                    Summary = description,
                    Description = description,
                    CoverPath = coverUrl,
                    Status = status,
                    ReleaseYear = year,
                    Author = author,
                    ProviderId = id,
                    ProviderName = "MangaDex",
                    ProviderUrl = $"{MangaUrl}/{id}",
                    HasMetadata = true,
                    LastModified = DateTime.Now
                };
                
                Logger.Log($"[MangaDexScraper] Parsed metadata for: {title}", LogLevel.Info);
                return metadata;
            }
            catch (Exception ex)
            {
                Logger.Log($"[MangaDexScraper] Error parsing manga page: {ex.Message}", LogLevel.Error);
                return CreateDefaultMetadata(id);
            }
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

        private SeriesMetadata CreateDefaultMetadata(string providerId)
        {
            return new SeriesMetadata
            {
                Series = "Unknown",
                Summary = "",
                Description = "",
                CoverPath = "",
                Status = "Unknown",
                Author = "",
                ProviderId = providerId,
                ProviderName = "MangaDex",
                ProviderUrl = $"{MangaUrl}/{providerId}",
                HasMetadata = false,
                LastModified = DateTime.Now
            };
        }
    }
}
