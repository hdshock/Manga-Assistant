using System;
using System.Text.Json.Serialization;

namespace MangaAssistant.Core.Models
{
    public class Chapter
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("seriesId")]
        public Guid SeriesId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("filePath")]
        public string FilePath { get; set; } = string.Empty;

        [JsonPropertyName("number")]
        public double Number { get; set; }

        [JsonPropertyName("volume")]
        public int Volume { get; set; }

        [JsonPropertyName("pageCount")]
        public int PageCount { get; set; }

        [JsonPropertyName("added")]
        public DateTime Added { get; set; }

        [JsonPropertyName("lastRead")]
        public DateTime LastRead { get; set; }

        public string ArchiveFormat { get; set; } = "CBZ";
        public bool IsRead { get; set; }
        public int LastReadPage { get; set; }
    }
} 