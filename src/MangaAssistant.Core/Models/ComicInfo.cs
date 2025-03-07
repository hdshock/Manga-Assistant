using System;
using System.Xml.Serialization;

namespace MangaAssistant.Core.Models
{
    [XmlRoot("ComicInfo")]
    public class ComicInfo
    {
        [XmlElement("Series")]
        public string? Series { get; set; }

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

        [XmlElement("Writer")]
        public string? Writer { get; set; }

        [XmlElement("Publisher")]
        public string? Publisher { get; set; }

        [XmlElement("Genre")]
        public string? Genre { get; set; }

        [XmlElement("PageCount")]
        public int PageCount { get; set; }

        [XmlElement("Count")]
        public string? Count { get; set; }

        [XmlElement("Manga")]
        public string? Manga { get; set; }

        [XmlElement("Characters")]
        public string? Characters { get; set; }

        [XmlElement("Teams")]
        public string? Teams { get; set; }

        [XmlElement("Locations")]
        public string? Locations { get; set; }

        [XmlElement("CommunityRating")]
        public float? CommunityRating { get; set; }
    }
} 