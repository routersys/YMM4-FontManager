using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FontManager.Converters
{
    public class WidthToVisibilityConverter : IValueConverter
    {
        public double Threshold { get; set; } = 850;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double width)
            {
                return width < Threshold ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}