using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MangaAssistant.Core.Models;
using System.ComponentModel;

namespace MangaAssistant.Infrastructure.Services
{
    public interface ILibraryService
    {
        event EventHandler<LibraryUpdatedEventArgs> LibraryUpdated;
        List<Series> Series { get; }
        Task ScanLibraryAsync();
    }

    public class LibraryUpdatedEventArgs : EventArgs
    {
        public List<Series> Series { get; }

        public LibraryUpdatedEventArgs(List<Series> series)
        {
            Series = series;
        }
    }

    public class LibraryService : ILibraryService, INotifyPropertyChanged
    {
        private readonly ISettingsService _settingsService;
        private readonly LibraryScanner _libraryScanner;
        private List<Series> _series = new();

        public event EventHandler<LibraryUpdatedEventArgs>? LibraryUpdated;
        public event PropertyChangedEventHandler? PropertyChanged;

        public List<Series> Series
        {
            get => _series;
            private set
            {
                _series = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Series)));
            }
        }

        public LibraryService(ISettingsService settingsService, LibraryScanner libraryScanner)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _libraryScanner = libraryScanner ?? throw new ArgumentNullException(nameof(libraryScanner));
        }

        public async Task ScanLibraryAsync()
        {
            if (string.IsNullOrWhiteSpace(_settingsService.LibraryPath))
                return;

            await _libraryScanner.ScanLibraryAsync();
            Series = _libraryScanner.Series;
            LibraryUpdated?.Invoke(this, new LibraryUpdatedEventArgs(Series));
        }
    }
} 