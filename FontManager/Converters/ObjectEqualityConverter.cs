using System.Globalization;
using System.Windows.Data;

namespace FontManager.Converters
{
    public class ObjectEqualityConverter : IValueConverter
    {
        public static ObjectEqualityConverter Instance { get; } = new();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value?.Equals(parameter) ?? parameter == null;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}