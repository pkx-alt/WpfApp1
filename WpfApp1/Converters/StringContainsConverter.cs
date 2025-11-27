using System;
using System.Globalization;
using System.Windows.Data;

namespace OrySiPOS.Converters
{
    public class StringContainsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Verificamos que el valor sea texto y el parámetro también
            if (value is string texto && parameter is string palabraClave)
            {
                // Devuelve TRUE si el texto contiene la palabra clave (ignorando mayúsculas/minúsculas)
                return texto.IndexOf(palabraClave, StringComparison.OrdinalIgnoreCase) >= 0;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}