using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using MangaAssistant.Core.Models;
using MangaAssistant.Core.Services;
using MangaAssistant.Infrastructure.Services.Metadata;
using MangaAssistant.Common.Logging;

namespace MangaAssistant.Tests
{
    public class MetadataSearchTest
    {
        public static async Task RunTest()
        {
            // Initialize logger
            TestLogger.Initialize("./logs");
            TestLogger.Log("Starting metadata search test for 'GUL'", LogLevel.Info);

            try
            {
                // Create instances of metadata providers
                var mangaDexProvider = new MangaDexMetadataProvider();
                var aniListProvider = new AniListMetadataProvider();

                // Test MangaDex search
                await TestMangaDexProvider(mangaDexProvider);

                // Test AniList search
                await TestAniListProvider(aniListProvider);

                TestLogger.Log("Metadata search test completed", LogLevel.Info);
            }
            catch (Exception ex)
            {
                TestLogger.LogException(ex, "RunTest");
            }
        }

        private static async Task TestMangaDexProvider(MangaDexMetadataProvider provider)
        {
            TestLogger.Log("Testing MangaDex search for 'GUL'", LogLevel.Info);
            try
            {
                // Test search functionality
                var results = await provider.SearchAsync("GUL");
                int resultCount = results.Count();
                TestLogger.Log($"MangaDex search returned {resultCount} results", LogLevel.Info);
                
                foreach (var result in results)
                {
                    TestLogger.Log($"MangaDex result: {result.Title} (ID: {result.ProviderId})", LogLevel.Info);
                    
                    // Try to get detailed metadata for each result
                    await GetDetailedMetadata(provider, result.ProviderId, "MangaDex");
                }

                // Even if search returns no results, test direct metadata retrieval
                if (resultCount == 0)
                {
                    TestLogger.Log("Testing direct metadata retrieval for 'GUL' from MangaDex", LogLevel.Info);
                    await GetDetailedMetadata(provider, "GUL", "MangaDex");
                }
            }
            catch (Exception ex)
            {
                TestLogger.LogException(ex, "TestMangaDexProvider");
            }
        }

        private static async Task TestAniListProvider(AniListMetadataProvider provider)
        {
            TestLogger.Log("Testing AniList search for 'GUL'", LogLevel.Info);
            try
            {
                // Test search functionality
                var results = await provider.SearchAsync("GUL");
                int resultCount = results.Count();
                TestLogger.Log($"AniList search returned {resultCount} results", LogLevel.Info);
                
                foreach (var result in results)
                {
                    TestLogger.Log($"AniList result: {result.Title} (ID: {result.ProviderId})", LogLevel.Info);
                    
                    // Try to get detailed metadata for each result
                    await GetDetailedMetadata(provider, result.ProviderId, "AniList");
                }

                // Even if search returns no results, test direct metadata retrieval
                if (resultCount == 0)
                {
                    TestLogger.Log("Testing direct metadata retrieval for 'GUL' from AniList", LogLevel.Info);
                    await GetDetailedMetadata(provider, "GUL", "AniList");
                }
            }
            catch (Exception ex)
            {
                TestLogger.LogException(ex, "TestAniListProvider");
            }
        }

        private static async Task GetDetailedMetadata(IMetadataProvider provider, string providerId, string providerName)
        {
            try
            {
                TestLogger.Log($"Getting detailed metadata from {providerName} for ID: {providerId}", LogLevel.Info);
                var metadata = await provider.GetMetadataAsync(providerId);
                
                if (metadata != null && metadata.HasMetadata)
                {
                    TestLogger.Log($"Successfully retrieved metadata for {metadata.Series}", LogLevel.Info);
                    LogMetadataDetails(metadata);
                }
                else
                {
                    TestLogger.Log($"No metadata found or incomplete metadata for ID: {providerId}", LogLevel.Warning);
                }
            }
            catch (Exception ex)
            {
                TestLogger.LogException(ex, $"GetDetailedMetadata_{providerName}_{providerId}");
            }
        }

        private static void LogMetadataDetails(SeriesMetadata metadata)
        {
            try
            {
                TestLogger.Log("Metadata Details:", LogLevel.Info);
                TestLogger.Log($"  Series: {metadata.Series}", LogLevel.Info);
                TestLogger.Log($"  Provider: {metadata.ProviderName} (ID: {metadata.ProviderId})", LogLevel.Info);
                TestLogger.Log($"  Status: {metadata.Status}", LogLevel.Info);
                
                if (metadata.Genres != null && metadata.Genres.Count > 0)
                {
                    TestLogger.Log($"  Genres: {string.Join(", ", metadata.Genres)}", LogLevel.Info);
                }
                
                if (metadata.Tags != null && metadata.Tags.Count > 0)
                {
                    TestLogger.Log($"  Tags: {string.Join(", ", metadata.Tags)}", LogLevel.Info);
                }
                
                TestLogger.Log($"  Has Cover Image: {!string.IsNullOrEmpty(metadata.CoverPath)}", LogLevel.Info);
            }
            catch (Exception ex)
            {
                TestLogger.LogException(ex, "LogMetadataDetails");
            }
        }
    }
}
