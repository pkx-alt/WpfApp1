// ActivePageToBooleanConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;

namespace OrySiPOS // Asegúrate que el namespace sea el correcto
{
    public class ActivePageToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // value es el ActivePage (ej: "Ventas realizadas")
            // parameter es el contenido del RadioButton (ej: "Ventas realizadas")
            return value?.ToString().Equals(parameter?.ToString(), StringComparison.OrdinalIgnoreCase) ?? false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Si el RadioButton está marcado (value = true), devuelve su contenido (parameter)
            if ((bool)value)
                return parameter;

            return null; // O Binding.DoNothing;
        }
    }
}