using System.Collections.Generic;
using System.Threading.Tasks;
using MangaAssistant.Core.Models;

namespace MangaAssistant.Core.Services
{
    public interface IMetadataProvider
    {
        string Name { get; }
        Task<IEnumerable<SeriesSearchResult>> SearchAsync(string query);
        Task<SeriesMetadata> GetMetadataAsync(string providerId);
        Task<byte[]?> GetCoverImageAsync(string url);
    }
} 