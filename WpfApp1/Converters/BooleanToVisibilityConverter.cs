using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WpfApp1.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 1. Si es booleano (true/false)
            if (value is bool b)
            {
                return b ? Visibility.Visible : Visibility.Collapsed;
            }

            // 2. ¡LO NUEVO! Si es un número (int), como el "Count" de una lista
            if (value is int i)
            {
                return i > 0 ? Visibility.Visible : Visibility.Collapsed;
            }

            // Por defecto, ocultar si no entendemos qué es
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}