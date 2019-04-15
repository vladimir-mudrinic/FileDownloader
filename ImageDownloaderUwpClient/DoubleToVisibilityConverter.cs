using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace ImageDownloaderUwpClient
{
    public class DoubleToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            double d = (double)value;
            return d == 0.0 || d == 100.0 ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
