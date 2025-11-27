using OrySiPOS.ViewModels; // Necesario para leer la constante
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace OrySiPOS.Views
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

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // 1. Verificamos que el clic fue en una fila válida
            if (sender is DataGrid grid && grid.SelectedItem is VentaHistorialItem ventaSeleccionada)
            {
                // 2. Obtenemos el ViewModel de la página
                if (this.DataContext is VentasRealizadasViewModel vm)
                {
                    // 3. Ejecutamos el comando del ViewModel, pasándole la venta seleccionada
                    if (vm.VerDetalleCommand.CanExecute(ventaSeleccionada))
                    {
                        vm.VerDetalleCommand.Execute(ventaSeleccionada);
                    }
                }
            }
        }
    }
}