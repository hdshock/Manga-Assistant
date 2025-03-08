using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MangaAssistant.Core.Models;
using MangaAssistant.Core.Services;

namespace MangaAssistant.Infrastructure.Services.Metadata
{
    public class AniListMetadataProvider : IMetadataProvider
    {
        private readonly HttpClient _httpClient;
        private const string API_URL = "https://graphql.anilist.co";

        public string Name => "AniList";

        public AniListMetadataProvider()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(API_URL)
            };
        }

        public async Task<IEnumerable<SeriesSearchResult>> SearchAsync(string query)
        {
            var searchQuery = @"
            query ($search: String) {
                Page(page: 1, perPage: 10) {
                    media(search: $search, type: MANGA, sort: SEARCH_MATCH) {
                        id
                        title {
                            romaji
                            english
                            native
                        }
                        coverImage {
                            large
                        }
                        description
                        status
                        chapters
                        volumes
                        startDate {
                            year
                        }
                    }
                }
            }";

            var variables = new { search = query };
            var response = await ExecuteGraphQLQuery(searchQuery, variables);

            var results = new List<SeriesSearchResult>();
            var media = response.GetProperty("data").GetProperty("Page").GetProperty("media");

            foreach (var item in media.EnumerateArray())
            {
                var title = item.GetProperty("title");
                var coverImage = item.GetProperty("coverImage");

                results.Add(new SeriesSearchResult
                {
                    ProviderId = item.GetProperty("id").ToString(),
                    Title = title.GetProperty("english").GetString() ?? 
                           title.GetProperty("romaji").GetString() ?? 
                           title.GetProperty("native").GetString() ?? 
                           "Unknown Title",
                    CoverUrl = coverImage.GetProperty("large").GetString() ?? string.Empty,
                    Description = item.GetProperty("description").GetString() ?? string.Empty,
                    Status = ConvertStatus(item.GetProperty("status").GetString()),
                    ChapterCount = item.GetProperty("chapters").ValueKind == JsonValueKind.Null ? 
                                 0 : item.GetProperty("chapters").GetInt32(),
                    VolumeCount = item.GetProperty("volumes").ValueKind == JsonValueKind.Null ? 
                                0 : item.GetProperty("volumes").GetInt32(),
                    ReleaseYear = item.GetProperty("startDate").GetProperty("year").ValueKind == JsonValueKind.Null ?
                                null : item.GetProperty("startDate").GetProperty("year").GetInt32()
                });
            }

            return results;
        }

        public async Task<SeriesMetadata> GetMetadataAsync(string providerId)
        {
            var query = @"
            query ($id: Int) {
                Media(id: $id, type: MANGA) {
                    id
                    title {
                        romaji
                        english
                        native
                    }
                    coverImage {
                        large
                    }
                    description
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
            }";

            var variables = new { id = int.Parse(providerId) };
            var response = await ExecuteGraphQLQuery(query, variables);

            var media = response.GetProperty("data").GetProperty("Media");
            var title = media.GetProperty("title");
            var coverImage = media.GetProperty("coverImage");

            var metadata = new SeriesMetadata
            {
                Series = title.GetProperty("english").GetString() ?? 
                        title.GetProperty("romaji").GetString() ?? 
                        title.GetProperty("native").GetString() ?? 
                        "Unknown Title",
                LocalizedSeries = title.GetProperty("native").GetString() ?? string.Empty,
                AlternativeTitles = new List<string> 
                { 
                    title.GetProperty("romaji").GetString() ?? string.Empty 
                },
                Summary = media.GetProperty("description").GetString() ?? string.Empty,
                Status = ConvertStatus(media.GetProperty("status").GetString()),
                Count = media.GetProperty("chapters").ValueKind == JsonValueKind.Null ? 
                       0 : media.GetProperty("chapters").GetInt32(),
                Volumes = media.GetProperty("volumes").ValueKind == JsonValueKind.Null ? 
                         null : media.GetProperty("volumes").GetInt32(),
                ReleaseYear = media.GetProperty("startDate").GetProperty("year").ValueKind == JsonValueKind.Null ?
                             null : media.GetProperty("startDate").GetProperty("year").GetInt32(),
                CoverPath = coverImage.GetProperty("large").GetString() ?? string.Empty,
                ProviderId = providerId,
                ProviderName = Name,
                ProviderUrl = $"https://anilist.co/manga/{providerId}",
                LastModified = DateTime.Now
            };

            // Add genres
            foreach (var genre in media.GetProperty("genres").EnumerateArray())
            {
                metadata.Genres.Add(genre.GetString() ?? string.Empty);
            }

            // Add tags
            foreach (var tag in media.GetProperty("tags").EnumerateArray())
            {
                metadata.Tags.Add(tag.GetProperty("name").GetString() ?? string.Empty);
            }

            return metadata;
        }

        public async Task<byte[]?> GetCoverImageAsync(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;

            try
            {
                return await _httpClient.GetByteArrayAsync(url).ConfigureAwait(false);
            }
            catch
            {
                return null;
            }
        }

        private async Task<JsonElement> ExecuteGraphQLQuery(string query, object variables)
        {
            var request = new
            {
                query,
                variables
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("", content).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonSerializer.Deserialize<JsonElement>(responseContent);
        }

        private string ConvertStatus(string? anilistStatus)
        {
            return anilistStatus?.ToUpper() switch
            {
                "FINISHED" => "Completed",
                "RELEASING" => "Ongoing",
                "NOT_YET_RELEASED" => "Announced",
                "CANCELLED" => "Cancelled",
                "HIATUS" => "Hiatus",
                _ => "Unknown"
            };
        }
    }
} 