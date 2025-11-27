// --- Converters/EnumToBooleanConverter.cs ---
using System;
using System.Globalization;
using System.Windows.Data;

namespace OrySiPOS.Converters
{
    public class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            string enumValue = value.ToString();
            string targetValue = parameter.ToString();

            return enumValue.Equals(targetValue, StringComparison.InvariantCultureIgnoreCase);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool == false || parameter == null)
                return null;

            if ((bool)value)
            {
                // Convierte el string del parámetro de vuelta al tipo Enum
                return Enum.Parse(targetType, parameter.ToString());
            }

            return null; // No hacer nada si es false
        }
    }
}