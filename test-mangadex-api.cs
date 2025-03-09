using System;
using System.Net.Http;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        using var client = new HttpClient();
        
        // Test direct search for GUL
        string searchUrl = "https://api.mangadex.org/manga?title=GUL&limit=20&includes[]=cover_art";
        Console.WriteLine($"Testing URL: {searchUrl}");
        
        var response = await client.GetAsync(searchUrl);
        var content = await response.Content.ReadAsStringAsync();
        
        Console.WriteLine($"Status: {response.StatusCode}");
        Console.WriteLine($"Response length: {content.Length}");
        Console.WriteLine("First 500 chars of response:");
        Console.WriteLine(content.Substring(0, Math.Min(500, content.Length)));
        
        // Check if response contains data
        if (content.Contains("\"data\":[]"))
        {
            Console.WriteLine("API returned empty data array");
        }
        else if (content.Contains("\"data\":["))
        {
            Console.WriteLine("API returned results in data array");
        }
    }
}
