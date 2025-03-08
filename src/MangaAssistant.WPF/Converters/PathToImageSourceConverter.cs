using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;

namespace MangaAssistant.WPF.Converters
{
    public class PathToImageSourceConverter : IValueConverter
    {
        private static readonly BitmapImage DefaultImage = new BitmapImage(new Uri("pack://application:,,,/MangaAssistant.WPF;component/Assets/placeholder-cover.jpg"));

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

                    // Load the image into memory first to verify it's valid
                    using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                    {
                        var image = new BitmapImage();
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.StreamSource = stream;
                        image.EndInit();
                        image.Freeze();
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