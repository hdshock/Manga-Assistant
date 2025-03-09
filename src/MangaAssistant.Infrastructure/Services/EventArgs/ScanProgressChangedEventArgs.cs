using System;

namespace MangaAssistant.Infrastructure.Services.EventArgs
{
    /// <summary>
    /// Event arguments for scan progress changes
    /// </summary>
    public class ScanProgressChangedEventArgs : System.EventArgs
    {
        /// <summary>
        /// The total number of directories to scan
        /// </summary>
        public int TotalDirectories { get; set; }

        /// <summary>
        /// The number of directories that have been scanned
        /// </summary>
        public int ScannedDirectories { get; set; }

        /// <summary>
        /// The percentage of completion (0-100)
        /// </summary>
        public int ProgressPercentage { get; set; }
    }
} 