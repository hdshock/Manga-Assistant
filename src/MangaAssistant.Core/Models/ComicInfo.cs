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

        [XmlElement("Writer")]
        public string? Writer { get; set; }

        [XmlElement("Publisher")]
        public string? Publisher { get; set; }

        [XmlElement("Genre")]
        public string? Genre { get; set; }

        [XmlElement("PageCount")]
        public int PageCount { get; set; }

        [XmlElement("Manga")]
        public string? Manga { get; set; }

        [XmlElement("Language")]
        public string? Language { get; set; }

        [XmlElement("Count")]
        public string? Count { get; set; }
    }
} 