using System;
using System.Globalization;
using System.Windows.Data;

namespace FontManager.Converters
{
    public class ObjectEqualityConverter : IValueConverter
    {
        public static ObjectEqualityConverter Instance { get; } = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null && parameter == null) return true;
            if (value == null || parameter == null) return false;

            if (value.GetType() != parameter.GetType())
            {
                try
                {
                    if (value is Enum && parameter is string strParam)
                    {
                        var enumVal = Enum.Parse(value.GetType(), strParam);
                        return value.Equals(enumVal);
                    }

                    var convertedParam = System.Convert.ChangeType(parameter, value.GetType(), culture);
                    return value.Equals(convertedParam);
                }
                catch
                {
                    return value.ToString() == parameter.ToString();
                }
            }

            return value.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b)
            {
                if (parameter == null) return null!;

                try
                {
                    if (targetType.IsEnum && parameter is string strParam)
                    {
                        return Enum.Parse(targetType, strParam);
                    }

                    return System.Convert.ChangeType(parameter, targetType, culture);
                }
                catch
                {
                    return parameter;
                }
            }
            return Binding.DoNothing;
        }
    }
}