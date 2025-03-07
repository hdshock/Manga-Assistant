using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MangaAssistant.Core.Models;

namespace MangaAssistant.Core.Interfaces
{
    public interface ILibraryService
    {
        // Library management
        Task<IEnumerable<string>> GetWatchedFoldersAsync();
        Task AddWatchedFolderAsync(string path);
        Task RemoveWatchedFolderAsync(string path);
        
        // Manga operations
        Task<IEnumerable<Manga>> GetAllMangaAsync();
        Task<Manga?> GetMangaByIdAsync(Guid id);
        Task<Manga> AddMangaAsync(Manga manga);
        Task UpdateMangaAsync(Manga manga);
        Task DeleteMangaAsync(Guid id);
        
        // Chapter operations
        Task<IEnumerable<Chapter>> GetChaptersByMangaIdAsync(Guid mangaId);
        Task<Chapter?> GetChapterByIdAsync(Guid id);
        Task<Chapter> AddChapterAsync(Chapter chapter);
        Task UpdateChapterAsync(Chapter chapter);
        Task DeleteChapterAsync(Guid id);
        
        // Scanning operations
        Task ScanLibraryAsync(bool forceUpdate = false);
        Task ScanMangaAsync(Guid mangaId, bool forceUpdate = false);
        
        // Metadata operations
        Task FetchMetadataAsync(Guid mangaId, string provider);
        Task UpdateAllMetadataAsync(string provider);
    }
} 