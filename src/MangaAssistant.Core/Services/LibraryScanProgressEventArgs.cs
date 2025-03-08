using System;

namespace MangaAssistant.Core.Services
{
    public class LibraryScanProgressEventArgs : EventArgs
    {
        public double Progress { get; set; }
        public string CurrentSeries { get; set; } = string.Empty;
        public int TotalSeries { get; set; }
        public int ScannedSeries { get; set; }
    }
} 