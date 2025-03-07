using System;

namespace MangaAssistant.Core.Models
{
    public class Chapter
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string ArchiveFormat { get; set; } = "CBZ";
        public int Number { get; set; }
        public int Volume { get; set; }
        public DateTime Added { get; set; }
        public DateTime LastRead { get; set; }
        public bool IsRead { get; set; }
        public int PageCount { get; set; }
        public int LastReadPage { get; set; }
        public Guid SeriesId { get; set; }
    }
} 