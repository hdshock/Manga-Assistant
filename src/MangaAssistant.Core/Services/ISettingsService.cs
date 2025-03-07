namespace MangaAssistant.Core.Services
{
    public interface ISettingsService
    {
        string LibraryPath { get; set; }
        void SaveSettings();
        void LoadSettings();
    }
} 