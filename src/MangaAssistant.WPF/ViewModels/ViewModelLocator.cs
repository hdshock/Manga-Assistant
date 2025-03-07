using MangaAssistant.Infrastructure.Services;
using MangaAssistant.Core.Services;
using MangaAssistant.Infrastructure.Services.Metadata;
using System;

namespace MangaAssistant.WPF.ViewModels
{
    public class ViewModelLocator
    {
        private static ISettingsService? _settingsService;
        private static ILibraryService? _libraryService;
        private static IMetadataService? _metadataService;
        private static LibraryScanner? _libraryScanner;
        private static MainViewModel? _mainViewModel;
        private static bool _isInitialized;

        public static void Initialize()
        {
            if (_isInitialized) return;

            _settingsService = new SettingsService();
            _libraryScanner = new LibraryScanner(_settingsService);
            _libraryService = new LibraryService(_settingsService, _libraryScanner);
            _metadataService = new MetadataService(_libraryService);

            _isInitialized = true;
        }

        public static MainViewModel MainViewModel
        {
            get
            {
                if (!_isInitialized)
                {
                    Initialize();
                }

                if (_mainViewModel == null)
                {
                    _mainViewModel = new MainViewModel(
                        _libraryService ?? throw new InvalidOperationException("LibraryService not initialized"),
                        _settingsService ?? throw new InvalidOperationException("SettingsService not initialized"),
                        _metadataService ?? throw new InvalidOperationException("MetadataService not initialized"));
                }
                return _mainViewModel;
            }
        }

        public static IMetadataService MetadataService
        {
            get
            {
                EnsureInitialized();
                return _metadataService ?? throw new InvalidOperationException("MetadataService not initialized");
            }
        }

        private static void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                Initialize();
            }
        }
    }
} 