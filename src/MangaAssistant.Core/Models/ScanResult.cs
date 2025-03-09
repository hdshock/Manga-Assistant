using System.Collections.Generic;

namespace MangaAssistant.Core.Models
{
    /// <summary>
    /// Represents the result of a library scan operation
    /// </summary>
    public class ScanResult
    {
        /// <summary>
        /// Indicates whether the scan was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// A message describing the result of the scan
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// The list of series found during the scan
        /// </summary>
        public List<Series> Series { get; set; } = new List<Series>();
    }
} 