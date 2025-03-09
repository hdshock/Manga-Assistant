using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MangaAssistant.Core.Models.MangaDex
{
    public class MangaDexResponse<T>
    {
        [JsonPropertyName("result")]
        public string Result { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public T Data { get; set; } = default!;

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("offset")]
        public int Offset { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }
    }

    public class MangaDexManga
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("attributes")]
        public MangaDexMangaAttributes Attributes { get; set; } = new();

        [JsonPropertyName("relationships")]
        public List<MangaDexRelationship> Relationships { get; set; } = new();
    }

    public class MangaDexMangaAttributes
    {
        [JsonPropertyName("title")]
        public Dictionary<string, string> Title { get; set; } = new();

        [JsonPropertyName("altTitles")]
        public List<Dictionary<string, string>> AltTitles { get; set; } = new();

        [JsonPropertyName("description")]
        public Dictionary<string, string> Description { get; set; } = new();

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("year")]
        public int? Year { get; set; }

        [JsonPropertyName("contentRating")]
        public string ContentRating { get; set; } = string.Empty;

        [JsonPropertyName("tags")]
        public List<MangaDexTag> Tags { get; set; } = new();

        [JsonPropertyName("originalLanguage")]
        public string OriginalLanguage { get; set; } = string.Empty;

        [JsonPropertyName("lastVolume")]
        public string? LastVolume { get; set; }

        [JsonPropertyName("lastChapter")]
        public string? LastChapter { get; set; }

        [JsonPropertyName("publicationDemographic")]
        public string? PublicationDemographic { get; set; }
    }

    public class MangaDexTag
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("attributes")]
        public MangaDexTagAttributes Attributes { get; set; } = new();
    }

    public class MangaDexTagAttributes
    {
        [JsonPropertyName("name")]
        public Dictionary<string, string> Name { get; set; } = new();

        [JsonPropertyName("group")]
        public string Group { get; set; } = string.Empty;

        [JsonPropertyName("version")]
        public int Version { get; set; }
    }

    public class MangaDexChapter
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("attributes")]
        public MangaDexChapterAttributes Attributes { get; set; } = new();

        [JsonPropertyName("relationships")]
        public List<MangaDexRelationship> Relationships { get; set; } = new();
    }

    public class MangaDexChapterAttributes
    {
        [JsonPropertyName("volume")]
        public string? Volume { get; set; }

        [JsonPropertyName("chapter")]
        public string? Chapter { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("translatedLanguage")]
        public string TranslatedLanguage { get; set; } = string.Empty;

        [JsonPropertyName("externalUrl")]
        public string? ExternalUrl { get; set; }

        [JsonPropertyName("publishAt")]
        public string PublishAt { get; set; } = string.Empty;

        [JsonPropertyName("pages")]
        public int Pages { get; set; }

        [JsonPropertyName("version")]
        public int Version { get; set; }
    }

    public class MangaDexRelationship
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("attributes")]
        public object? Attributes { get; set; }
    }

    public class MangaDexCoverArt
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("attributes")]
        public MangaDexCoverArtAttributes Attributes { get; set; } = new();
    }

    public class MangaDexCoverArtAttributes
    {
        [JsonPropertyName("fileName")]
        public string FileName { get; set; } = string.Empty;

        [JsonPropertyName("volume")]
        public string? Volume { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("version")]
        public int Version { get; set; }
    }
} 