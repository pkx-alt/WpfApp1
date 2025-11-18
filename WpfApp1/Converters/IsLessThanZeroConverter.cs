// --- Converters/IsLessThanZeroConverter.cs ---

using System;
using System.Globalization;
using System.Windows.Data;

namespace WpfApp1.Converters
{
    /// <summary>
    /// Este Converter devuelve 'true' si el valor (decimal) es menor que cero.
    /// </summary>
    public class IsLessThanZeroConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Intentamos convertir el valor de entrada a un decimal
            if (value is decimal d)
            {
                // La lógica principal: ¿es menor que 0?
                return d < 0;
            }

            // Si no es un decimal, no hacemos nada
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // No necesitamos convertir de vuelta, así que no hacemos nada
            throw new NotImplementedException();
        }
    }
}