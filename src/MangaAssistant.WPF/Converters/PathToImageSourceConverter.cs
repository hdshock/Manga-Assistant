using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Collections.Concurrent;

namespace MangaAssistant.WPF.Converters
{
    public class PathToImageSourceConverter : IValueConverter
    {
        private static readonly BitmapImage DefaultImage = new BitmapImage(new Uri("pack://application:,,,/MangaAssistant.WPF;component/Assets/placeholder-cover.jpg"));
        private static readonly ConcurrentDictionary<string, ImageSource> _imageCache = new ConcurrentDictionary<string, ImageSource>();

        /// <summary>
        /// Clears the image cache to force reloading of all images
        /// </summary>
        public void ClearCache()
        {
            _imageCache.Clear();
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string path && !string.IsNullOrEmpty(path))
            {
                try
                {
                    if (!File.Exists(path))
                    {
                        return DefaultImage;
                    }

                    // Check if the image is already in the cache
                    if (_imageCache.TryGetValue(path, out var cachedImage) && cachedImage != null)
                    {
                        return cachedImage;
                    }

                    // Load the image into memory first to verify it's valid
                    using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                    {
                        var image = new BitmapImage();
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.StreamSource = stream;
                        image.EndInit();
                        image.Freeze();
                        
                        // Add to cache
                        _imageCache[path] = image;
                        
                        return image;
                    }
                }
                catch
                {
                    // Return default image if loading fails for any reason
                    return DefaultImage;
                }
            }

            return DefaultImage;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}