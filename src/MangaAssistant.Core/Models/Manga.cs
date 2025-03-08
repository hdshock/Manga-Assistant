using System;
using System.Collections.Generic;

namespace MangaAssistant.Core.Models
{
    public class Manga
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string OriginalTitle { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CoverImagePath { get; set; } = string.Empty;
        public List<string> Authors { get; set; } = new();
        public List<string> Artists { get; set; } = new();
        public List<string> Genres { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public string Status { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string LibraryPath { get; set; } = string.Empty;
        
        // External service IDs
        public string? AniListId { get; set; }
        public string? MangaDexId { get; set; }
        
        // Metadata
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime LastScanned { get; set; }
    }
} 