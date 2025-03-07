using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace MangaAssistant.Core.Models
{
    public class Series
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string FolderPath { get; set; } = string.Empty;
        public string CoverPath { get; set; } = string.Empty;
        public DateTime Created { get; set; }
        public DateTime LastModified { get; set; }
        public List<Chapter> Chapters { get; set; } = new List<Chapter>();
        public SeriesMetadata Metadata { get; set; } = new SeriesMetadata();

        // Calculated properties
        public int ChapterCount => Chapters?.Count ?? 0;
        public int ReadChapterCount => Chapters?.Count(c => c.IsRead) ?? 0;
        public double Progress => ChapterCount > 0 ? (ReadChapterCount * 100.0) / ChapterCount : 0;
    }
} 