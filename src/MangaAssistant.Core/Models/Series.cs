using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace MangaAssistant.Core.Models
{
    public class Series
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("folderPath")]
        public string FolderPath { get; set; } = string.Empty;

        [JsonPropertyName("coverPath")]
        public string CoverPath { get; set; } = string.Empty;

        [JsonPropertyName("created")]
        public DateTime Created { get; set; }

        [JsonPropertyName("lastModified")]
        public DateTime LastModified { get; set; }

        [JsonPropertyName("chapters")]
        public List<Chapter> Chapters { get; set; } = new();

        [JsonPropertyName("metadata")]
        public SeriesMetadata Metadata { get; set; } = new();

        [JsonPropertyName("progress")]
        public double Progress { get; set; }

        [JsonPropertyName("chapterCount")]
        public int ChapterCount { get; set; }

        [JsonPropertyName("readChapterCount")]
        public int ReadChapterCount => Chapters?.Count(c => c.LastRead > DateTime.MinValue) ?? 0;
    }
} 