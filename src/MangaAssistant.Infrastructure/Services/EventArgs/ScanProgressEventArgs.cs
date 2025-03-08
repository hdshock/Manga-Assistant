using System;

namespace MangaAssistant.Infrastructure.Services.EventArgs
{
    public class ScanProgressEventArgs : System.EventArgs
    {
        public double Progress { get; }
        public string CurrentSeriesTitle { get; }
        public int ScannedDirectories { get; }
        public int TotalDirectories { get; }

        public ScanProgressEventArgs(double progress, string currentSeriesTitle, int scannedDirectories, int totalDirectories)
        {
            Progress = progress;
            CurrentSeriesTitle = currentSeriesTitle;
            ScannedDirectories = scannedDirectories;
            TotalDirectories = totalDirectories;
        }
    }
} 