namespace MangaAssistant.Core.Services
{
    public interface ISettingsService
    {
        string LibraryPath { get; set; }
        bool InsertCoverIntoFirstChapter { get; set; }
        void SaveSettings();
        void LoadSettings();
    }
} 