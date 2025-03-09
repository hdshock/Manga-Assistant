using System;
using MangaAssistant.Core.Services;
using MangaAssistant.Infrastructure.Services;
using MangaAssistant.Common.Logging;

namespace MangaAssistant.WPF.ViewModels
{
    public class ViewModelLocator
    {
        private static readonly Lazy<MainViewModel> _mainViewModel = new Lazy<MainViewModel>(() => new MainViewModel(_libraryService, _settingsService, _metadataService));
        private static ILibraryService _libraryService;
        private static ISettingsService _settingsService;
        private static IMetadataService _metadataService;
        
        public static MainViewModel MainViewModel => _mainViewModel.Value;
        
        public static void RegisterServices(ILibraryService libraryService, ISettingsService settingsService, IMetadataService metadataService)
        {
            _libraryService = libraryService ?? throw new ArgumentNullException(nameof(libraryService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _metadataService = metadataService ?? throw new ArgumentNullException(nameof(metadataService));
            
            Logger.Log("Services registered with ViewModelLocator", LogLevel.Info);
        }
    }
} 