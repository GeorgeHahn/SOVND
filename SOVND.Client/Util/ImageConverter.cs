using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace System.Windows.Media
{
    /// <summary>
    /// One-way converter from System.Drawing.Image to System.Windows.Media.ImageSource
    /// </summary>
    [ValueConversion(typeof(Image), typeof(ImageSource))]
    public class ImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            // empty images are empty...
            if (value == null) { return null; }

            if (value is BitmapImage)
                return value;

            var image = (Image)value;
            // Winforms Image we want to get the WPF Image from...
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            MemoryStream memoryStream = new MemoryStream();
            // Save to a memory stream...
            image.Save(memoryStream, ImageFormat.Bmp);
            // Rewind the stream...
            memoryStream.Seek(0, SeekOrigin.Begin);
            bitmap.StreamSource = memoryStream;
            bitmap.EndInit();
            return bitmap;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}