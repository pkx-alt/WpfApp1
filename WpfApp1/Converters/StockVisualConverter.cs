using System;
using System.Globalization;
using System.Windows.Data;
using OrySiPOS.Models; // Asegúrate de tener este using

namespace OrySiPOS.Converters
{
    public class StockVisualConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // El valor que recibimos es todo el objeto 'Producto' (gracias al Binding que haremos)
            if (value is Producto producto)
            {
                // Si es servicio, mostramos el guion o un infinito "∞"
                if (producto.EsServicio)
                    return "-"; // O "∞" si prefieres

                // Si es producto normal, mostramos su stock real
                return producto.Stock.ToString();
            }

            return "0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}