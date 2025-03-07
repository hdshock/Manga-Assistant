using System.Threading.Tasks;
using MangaAssistant.Core.Models;

namespace MangaAssistant.Core.Interfaces
{
    public interface IMetadataProvider
    {
        string ProviderName { get; }
        
        Task<Manga?> SearchMangaAsync(string title);
        Task<Manga?> GetMangaByIdAsync(string providerId);
        Task<byte[]?> GetCoverImageAsync(string imageUrl);
        Task<bool> UpdateMangaMetadataAsync(Manga manga);
    }
} 