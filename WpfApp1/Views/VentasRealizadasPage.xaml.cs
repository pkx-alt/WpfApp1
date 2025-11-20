using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfApp1.ViewModels; // Necesario para leer la constante

namespace WpfApp1.Views
{
    public partial class VentasRealizadasPage : Page
    {
        public VentasRealizadasPage()
        {
            InitializeComponent();
            // Asegúrate de que el foco visual coincida al inicio
            RestaurarPlaceholder();
        }

        // Cuando el usuario hace clic para escribir
        private void TxtBusquedaHistorial_GotFocus(object sender, RoutedEventArgs e)
        {
            if (TxtBusquedaHistorial.Text == VentasRealizadasViewModel.PlaceholderBusqueda)
            {
                TxtBusquedaHistorial.Text = "";
                TxtBusquedaHistorial.Foreground = Brushes.Black;
            }
        }

        // Cuando el usuario se va a otra parte
        private void TxtBusquedaHistorial_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtBusquedaHistorial.Text))
            {
                RestaurarPlaceholder();
            }
        }

        // Método auxiliar para no repetir código
        private void RestaurarPlaceholder()
        {
            // Usamos la constante del ViewModel para que siempre sea idéntica
            TxtBusquedaHistorial.Text = VentasRealizadasViewModel.PlaceholderBusqueda;
            TxtBusquedaHistorial.Foreground = Brushes.Gray;
        }
    }
}