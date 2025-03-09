namespace MangaAssistant.Core.Models
{
    public class SeriesSearchResult
    {
        public string ProviderId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CoverUrl { get; set; } = string.Empty;
        public int? Year { get; set; }
        public string Url { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int ChapterCount { get; set; }
        public int VolumeCount { get; set; }
        public int? ReleaseYear { get; set; }
        public string ProviderName { get; set; } = string.Empty;
    }
} 