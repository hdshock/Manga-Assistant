using System;
using System.Xml.Serialization;

namespace MangaAssistant.Core.Models
{
    [XmlRoot("ComicInfo", Namespace = "")]
    public class ComicInfo
    {
        [XmlElement("Series")]
        public string? Series { get; set; }

        [XmlElement("LocalizedSeries")]
        public string? LocalizedSeries { get; set; }

        [XmlElement("SeriesSort")]
        public string? SeriesSort { get; set; }

        [XmlElement("Title")]
        public string? Title { get; set; }

        [XmlElement("Number")]
        public string? Number { get; set; }

        [XmlElement("Volume")]
        public string? Volume { get; set; }

        [XmlElement("Summary")]
        public string? Summary { get; set; }

        [XmlElement("Notes")]
        public string? Notes { get; set; }

        [XmlElement("Year")]
        public int? Year { get; set; }

        [XmlElement("Month")]
        public int? Month { get; set; }

        [XmlElement("Day")]
        public int? Day { get; set; }

        [XmlElement("Writer")]
        public string? Writer { get; set; }

        [XmlElement("Penciller")]
        public string? Penciller { get; set; }

        [XmlElement("Inker")]
        public string? Inker { get; set; }

        [XmlElement("Colorist")]
        public string? Colorist { get; set; }

        [XmlElement("Letterer")]
        public string? Letterer { get; set; }

        [XmlElement("CoverArtist")]
        public string? CoverArtist { get; set; }

        [XmlElement("Editor")]
        public string? Editor { get; set; }

        [XmlElement("Translator")]
        public string? Translator { get; set; }

        [XmlElement("Publisher")]
        public string? Publisher { get; set; }

        [XmlElement("Imprint")]
        public string? Imprint { get; set; }

        [XmlElement("Genre")]
        public string? Genre { get; set; }

        [XmlElement("Tags")]
        public string? Tags { get; set; }

        [XmlElement("Web")]
        public string? Web { get; set; }

        [XmlElement("PageCount")]
        public int PageCount { get; set; }

        [XmlElement("LanguageISO")]
        public string? LanguageISO { get; set; }

        [XmlElement("Format")]
        public string? Format { get; set; }

        [XmlElement("AgeRating")]
        public string? AgeRating { get; set; }

        [XmlElement("Count")]
        public string? Count { get; set; }

        [XmlElement("Manga")]
        public YesNo Manga { get; set; }

        [XmlElement("Characters")]
        public string? Characters { get; set; }

        [XmlElement("Teams")]
        public string? Teams { get; set; }

        [XmlElement("Locations")]
        public string? Locations { get; set; }

        [XmlElement("ScanInformation")]
        public string? ScanInformation { get; set; }

        [XmlElement("CommunityRating")]
        public float? CommunityRating { get; set; }

        [XmlElement("GTIN")]
        public string? GTIN { get; set; }
    }

    public enum YesNo
    {
        Unknown = 0,
        No = 1,
        Yes = 2
    }
} 