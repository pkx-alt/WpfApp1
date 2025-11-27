using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using OrySiPOS.Properties; // Para leer tus Ajustes

namespace OrySiPOS.Converters
{
    public class StockToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int stock)
            {
                // 1. Leemos tu configuración
                int limiteBajo = Settings.Default.NivelBajoStock;

                // Definimos un "limite de advertencia" (ej. el doble del bajo)
                int limiteAdvertencia = limiteBajo * 2;

                // 2. Decidimos el color
                if (stock <= limiteBajo)
                {
                    // CRÍTICO: Rojo (DangerColor)
                    return Application.Current.Resources["DangerColor"] as Brush;
                }
                else if (stock <= limiteAdvertencia)
                {
                    // ADVERTENCIA: Naranja (WarningColor)
                    return Application.Current.Resources["WarningColor"] as Brush;
                }
            }

            // 3. Si está bien, devolvemos el color normal (Negro/Gris)
            return Application.Current.Resources["TextPrimary"] as Brush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}