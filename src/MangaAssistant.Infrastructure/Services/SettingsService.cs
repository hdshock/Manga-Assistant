using System;
using System.IO;
using System.Text.Json;

namespace MangaAssistant.Infrastructure.Services
{
    public interface ISettingsService
    {
        string LibraryPath { get; set; }
        void SaveSettings();
        void LoadSettings();
    }

    public class SettingsService : ISettingsService
    {
        private const string SettingsFileName = "settings.json";
        private string _libraryPath = string.Empty;  // Initialize with empty string

        public string LibraryPath
        {
            get => _libraryPath;
            set
            {
                _libraryPath = value ?? string.Empty;  // Handle null values
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
                LibraryPath = _libraryPath
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
                }
            }
            catch
            {
                // If loading fails, use defaults
                _libraryPath = string.Empty;
            }
        }
    }
} 