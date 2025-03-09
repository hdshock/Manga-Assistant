using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace MangaDexApiTest
{
    class MangaDexApiTest
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string BaseUrl = "https://api.mangadex.org";

        static async Task Main(string[] args)
        {
            // Configure HttpClient
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MangaAssistant/1.0");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            Console.WriteLine("MangaDex API Test");
            Console.WriteLine("================");

            string query = "GUL";
            if (args.Length > 0)
            {
                query = args[0];
            }

            Console.WriteLine($"Testing search for: {query}");
            Console.WriteLine();

            // Test direct search
            await TestDirectSearch(query);

            // Test comprehensive search
            await TestComprehensiveSearch(query);

            // Test direct ID lookup if query looks like an ID
            if (IsValidUuid(query))
            {
                await TestDirectIdLookup(query);
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static async Task TestDirectSearch(string query)
        {
            Console.WriteLine("=== Direct Title Search ===");
            var searchUrl = $"{BaseUrl}/manga?title={Uri.EscapeDataString(query)}&limit=20&includes[]=cover_art";
            Console.WriteLine($"URL: {searchUrl}");

            try
            {
                var response = await _httpClient.GetAsync(searchUrl);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Status: {response.StatusCode}");
                Console.WriteLine($"Response length: {content.Length}");

                var jsonResponse = JsonSerializer.Deserialize<JsonNode>(content);
                var data = jsonResponse["data"]?.AsArray();

                if (data == null || data.Count == 0)
                {
                    Console.WriteLine("No results found.");
                }
                else
                {
                    Console.WriteLine($"Found {data.Count} results:");
                    foreach (var manga in data)
                    {
                        var attributes = manga["attributes"];
                        var titles = attributes["title"].AsObject();
                        
                        // Get the first title available
                        string title = "";
                        foreach (var titleProp in titles)
                        {
                            title = titleProp.Value.GetValue<string>();
                            break;
                        }

                        Console.WriteLine($"- {title}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            Console.WriteLine();
        }

        private static async Task TestComprehensiveSearch(string query)
        {
            Console.WriteLine("=== Comprehensive Search ===");
            
            // Build query parameters
            var queryParams = new Dictionary<string, string>();
            
            // Add title parameter
            queryParams.Add("title", query);
            
            // Add additional parameters for comprehensive search
            queryParams.Add("limit", "20");
            queryParams.Add("includes[]", "cover_art");
            queryParams.Add("includes[]", "author");
            
            // For short queries, add author and artist search
            if (query.Length <= 3)
            {
                queryParams.Add("authorOrArtist", query);
            }
            
            var url = BuildUrlWithQueryParams($"{BaseUrl}/manga", queryParams);
            Console.WriteLine($"URL: {url}");

            try
            {
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Status: {response.StatusCode}");
                Console.WriteLine($"Response length: {content.Length}");

                var jsonResponse = JsonSerializer.Deserialize<JsonNode>(content);
                var data = jsonResponse["data"]?.AsArray();

                if (data == null || data.Count == 0)
                {
                    Console.WriteLine("No results found.");
                }
                else
                {
                    Console.WriteLine($"Found {data.Count} results:");
                    foreach (var manga in data)
                    {
                        var attributes = manga["attributes"];
                        var titles = attributes["title"].AsObject();
                        
                        // Get the first title available
                        string title = "";
                        foreach (var titleProp in titles)
                        {
                            title = titleProp.Value.GetValue<string>();
                            break;
                        }

                        Console.WriteLine($"- {title}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            Console.WriteLine();
        }

        private static async Task TestDirectIdLookup(string id)
        {
            Console.WriteLine("=== Direct ID Lookup ===");
            var url = $"{BaseUrl}/manga/{id}?includes[]=cover_art";
            Console.WriteLine($"URL: {url}");

            try
            {
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Status: {response.StatusCode}");
                Console.WriteLine($"Response length: {content.Length}");

                var jsonResponse = JsonSerializer.Deserialize<JsonNode>(content);
                var data = jsonResponse["data"];

                if (data == null)
                {
                    Console.WriteLine("No manga found with this ID.");
                }
                else
                {
                    var attributes = data["attributes"];
                    var titles = attributes["title"].AsObject();
                    
                    // Get the first title available
                    string title = "";
                    foreach (var titleProp in titles)
                    {
                        title = titleProp.Value.GetValue<string>();
                        break;
                    }

                    Console.WriteLine($"Found manga: {title}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            Console.WriteLine();
        }

        private static bool IsValidUuid(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            // Simple check for UUID format
            return System.Text.RegularExpressions.Regex.IsMatch(
                input,
                @"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
        }

        private static string BuildUrlWithQueryParams(string baseUrl, Dictionary<string, string> queryParams)
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
    }
}
