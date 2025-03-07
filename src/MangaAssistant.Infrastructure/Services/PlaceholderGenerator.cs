using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;

namespace MangaAssistant.Infrastructure.Services
{
    internal static class PlaceholderGenerator
    {
        public static void CreatePlaceholder()
        {
            var width = 350;
            var height = 500;

            // Create drawing visual
            var drawingVisual = new DrawingVisual();
            using (var context = drawingVisual.RenderOpen())
            {
                // Fill background
                context.DrawRectangle(
                    new SolidColorBrush(Color.FromRgb(51, 51, 55)), // Dark gray background
                    null,
                    new Rect(0, 0, width, height));

                // Draw text
                var text = new FormattedText(
                    "No Cover",
                    System.Globalization.CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"),
                    24,
                    Brushes.White,
                    VisualTreeHelper.GetDpi(new Border()).PixelsPerDip);

                // Center the text
                var x = (width - text.Width) / 2;
                var y = (height - text.Height) / 2;
                context.DrawText(text, new Point(x, y));
            }

            // Create RenderTargetBitmap
            var bitmap = new RenderTargetBitmap(
                width, height,
                96, 96,
                PixelFormats.Pbgra32);
            bitmap.Render(drawingVisual);

            // Create directory if it doesn't exist
            var assetsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
            Directory.CreateDirectory(assetsDir);

            // Save the image
            var encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = File.Create(Path.Combine(assetsDir, "folder-placeholder.jpg"));
            encoder.Save(stream);
        }
    }
} 