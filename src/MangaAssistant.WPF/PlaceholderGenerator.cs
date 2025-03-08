using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MangaAssistant.WPF
{
    public static class PlaceholderGenerator
    {
        public static void CreatePlaceholder()
        {
            var width = 400;
            var height = 600;
            var dpi = 96;

            var renderTarget = new RenderTargetBitmap(width, height, dpi, dpi, PixelFormats.Pbgra32);
            var visual = new DrawingVisual();

            using (var context = visual.RenderOpen())
            {
                // Draw background
                context.DrawRectangle(
                    new SolidColorBrush(Color.FromRgb(51, 51, 55)), // #333337
                    null,
                    new Rect(0, 0, width, height));

                // Draw placeholder icon
                var text = "📚";
                var typeface = new Typeface("Segoe UI");
                var fontSize = 100;
                var formattedText = new FormattedText(
                    text,
                    System.Globalization.CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    fontSize,
                    Brushes.White,
                    dpi);

                var x = (width - formattedText.Width) / 2;
                var y = (height - formattedText.Height) / 2;
                context.DrawText(formattedText, new Point(x, y));
            }

            renderTarget.Render(visual);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderTarget));

            var assetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
            Directory.CreateDirectory(assetsPath);
            var placeholderPath = Path.Combine(assetsPath, "placeholder-cover.jpg");

            // Ensure the directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(placeholderPath)!);

            // Save the image
            using (var stream = File.Create(placeholderPath))
            {
                encoder.Save(stream);
            }

            // Also save a copy in the application directory
            var appPlaceholderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "placeholder-cover.jpg");
            File.Copy(placeholderPath, appPlaceholderPath, true);
        }
    }
} 