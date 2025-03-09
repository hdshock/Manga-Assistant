using System;
using System.IO;
using System.Text.Json;
using MangaAssistant.Core.Services;

namespace MangaAssistant.Infrastructure.Services
{
    public class SettingsService : ISettingsService
    {
        private const string SettingsFileName = "settings.json";
        private string _libraryPath = string.Empty;  // Initialize with empty string
        private bool _insertCoverIntoFirstChapter = false;  // Default to false

        public string LibraryPath
        {
            get => _libraryPath;
            set
            {
                _libraryPath = value ?? string.Empty;  // Handle null values
                SaveSettings();
            }
        }

        public bool InsertCoverIntoFirstChapter
        {
            get => _insertCoverIntoFirstChapter;
            set
            {
                _insertCoverIntoFirstChapter = value;
                SaveSettings();
            }
        }

        public SettingsService()
        {
            LoadSettings();
        }

        public void SaveSettings()
        {
            var settings = new
            {
                LibraryPath = _libraryPath,
                InsertCoverIntoFirstChapter = _insertCoverIntoFirstChapter
            };

            var json = JsonSerializer.Serialize(settings);
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MangaAssistant"
            );

            Directory.CreateDirectory(appDataPath);
            File.WriteAllText(Path.Combine(appDataPath, SettingsFileName), json);
        }

        public void LoadSettings()
        {
            try
            {
                var appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MangaAssistant",
                    SettingsFileName
                );

                if (File.Exists(appDataPath))
                {
                    var json = File.ReadAllText(appDataPath);
                    var settings = JsonSerializer.Deserialize<JsonElement>(json);
                    _libraryPath = settings.GetProperty("LibraryPath").GetString() ?? string.Empty;
                    
                    // Try to get the InsertCoverIntoFirstChapter property, default to false if not found
                    if (settings.TryGetProperty("InsertCoverIntoFirstChapter", out var insertCoverProperty))
                    {
                        _insertCoverIntoFirstChapter = insertCoverProperty.GetBoolean();
                    }
                    else
                    {
                        _insertCoverIntoFirstChapter = false;
                    }
                }
            }
            catch
            {
                // If loading fails, use defaults
                _libraryPath = string.Empty;
                _insertCoverIntoFirstChapter = false;
            }
        }
    }
}