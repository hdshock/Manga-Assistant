using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaAssistant.Core.Interfaces
{
    public interface IConfigurationService
    {
        // General settings
        Task<string> GetDatabasePathAsync();
        Task<string> GetMetadataFolderAsync();
        Task<string> GetCoverImagesFolderAsync();
        Task<IEnumerable<string>> GetWatchedFoldersAsync();
        
        // Provider settings
        Task<IDictionary<string, string>> GetProviderSettingsAsync(string providerName);
        Task SaveProviderSettingsAsync(string providerName, IDictionary<string, string> settings);
        
        // File monitoring settings
        Task<int> GetScanIntervalMinutesAsync();
        Task<bool> GetAutoScanEnabledAsync();
        Task<bool> GetAutoMetadataUpdateEnabledAsync();
        
        // Save all settings
        Task SaveSettingsAsync();
        
        // Load all settings
        Task LoadSettingsAsync();
    }
} 