using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MangaAssistant.Core.Models
{
    public class SeriesMetadata
    {
        [JsonPropertyName("series")]
        public string Series { get; set; } = string.Empty;

        [JsonPropertyName("localizedSeries")]
        public string LocalizedSeries { get; set; } = string.Empty;

        [JsonPropertyName("alternativeTitles")]
        public List<string> AlternativeTitles { get; set; } = new();

        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;

        [JsonPropertyName("publisher")]
        public string Publisher { get; set; } = string.Empty;

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("volumes")]
        public int? Volumes { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("genres")]
        public List<string> Genres { get; set; } = new();

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new();

        [JsonPropertyName("ageRating")]
        public string AgeRating { get; set; } = string.Empty;

        [JsonPropertyName("releaseYear")]
        public int? ReleaseYear { get; set; }

        [JsonPropertyName("language")]
        public string Language { get; set; } = string.Empty;

        [JsonPropertyName("created")]
        public DateTime Created { get; set; } = DateTime.Now;

        [JsonPropertyName("lastModified")]
        public DateTime LastModified { get; set; } = DateTime.Now;

        [JsonPropertyName("coverPath")]
        public string CoverPath { get; set; } = string.Empty;

        [JsonPropertyName("availableCovers")]
        public string[] AvailableCovers { get; set; } = Array.Empty<string>();

        [JsonPropertyName("selectedCoverIndex")]
        public int SelectedCoverIndex { get; set; }

        [JsonPropertyName("hasMetadata")]
        public bool HasMetadata { get; set; }

        [JsonPropertyName("providerId")]
        public string ProviderId { get; set; } = string.Empty;

        [JsonPropertyName("providerName")]
        public string ProviderName { get; set; } = string.Empty;

        [JsonPropertyName("providerUrl")]
        public string ProviderUrl { get; set; } = string.Empty;

        [JsonPropertyName("writers")]
        public List<string> Writers { get; set; } = new List<string>();

        [JsonPropertyName("publicationStatus")]
        public string PublicationStatus { get; set; } = string.Empty;
    }
} 