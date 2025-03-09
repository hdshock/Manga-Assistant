using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using MangaAssistant.Core.Models;

namespace MangaAssistant.Core
{
    public class LibraryUpdatedEventArgs : EventArgs
    {
        public List<Series> Series { get; }

        public LibraryUpdatedEventArgs(List<Series> series)
        {
            Series = series;
        }
    }

    public class ScanProgressEventArgs : EventArgs
    {
        public double Progress { get; }
        public string CurrentSeriesTitle { get; }
        public int ScannedDirectories { get; }
        public int TotalDirectories { get; }
        public double ProgressPercentage => TotalDirectories > 0 ? (double)ScannedDirectories / TotalDirectories * 100 : 0;

        public ScanProgressEventArgs(int totalDirectories, int scannedDirectories)
        {
            TotalDirectories = totalDirectories;
            ScannedDirectories = scannedDirectories;
        }
        
        public ScanProgressEventArgs(double progress, string currentSeriesTitle, int scannedDirectories, int totalDirectories)
        {
            Progress = progress;
            CurrentSeriesTitle = currentSeriesTitle;
            ScannedDirectories = scannedDirectories;
            TotalDirectories = totalDirectories;
        }
    }
}

namespace MangaAssistant.Core.Services
{
    public interface ILibraryService : INotifyPropertyChanged
    {
        event EventHandler<MangaAssistant.Core.LibraryUpdatedEventArgs>? LibraryUpdated;
        event EventHandler<Series>? SeriesAdded;
        event EventHandler<MangaAssistant.Core.ScanProgressEventArgs>? ScanProgress;
        List<Series> Series { get; }
        Task ScanLibraryAsync();
        Task SaveSeriesMetadataAsync(Series series);
        Task<SeriesMetadata?> LoadSeriesMetadataAsync(string directoryPath);
        Task LoadLibraryCacheAsync();
        Task SaveLibraryCacheAsync();
        Task UpdateSeriesCover(string seriesPath, string coverPath);
        Task ClearCacheAsync();
        Task ClearAndRescanLibraryAsync();
        Task ProcessAllCoversAsync();
    }
}