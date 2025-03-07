using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using MangaAssistant.Core.Models;

namespace MangaAssistant.Core.Services
{
    public interface ILibraryService : INotifyPropertyChanged
    {
        event EventHandler<LibraryUpdatedEventArgs> LibraryUpdated;
        event EventHandler<Series> SeriesAdded;
        List<Series> Series { get; }
        Task ScanLibraryAsync();
        Task SaveSeriesMetadataAsync(Series series);
        Task<SeriesMetadata?> LoadSeriesMetadataAsync(string directoryPath);
        Task UpdateSeriesCover(string seriesPath, string coverPath);
        Task ClearCacheAsync();
        Task ClearAndRescanLibraryAsync();
    }

    public class LibraryUpdatedEventArgs : EventArgs
    {
        public List<Series> Series { get; }

        public LibraryUpdatedEventArgs(List<Series> series)
        {
            Series = series;
        }
    }
} 