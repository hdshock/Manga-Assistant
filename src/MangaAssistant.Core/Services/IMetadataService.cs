using System.Collections.Generic;
using System.Threading.Tasks;
using MangaAssistant.Core.Models;

namespace MangaAssistant.Core.Services
{
    public interface IMetadataService
    {
        IReadOnlyList<IMetadataProvider> Providers { get; }
        Task<IEnumerable<SeriesSearchResult>> SearchAsync(string providerName, string query);
        Task<SeriesMetadata> GetMetadataAsync(string providerName, string providerId);
        Task UpdateSeriesMetadataAsync(Series series, string providerName, string providerId);
    }
}