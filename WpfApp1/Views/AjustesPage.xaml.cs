using System.Windows;
using System.Windows.Controls;
using OrySiPOS.ViewModels;

namespace OrySiPOS.Views
{
    public partial class AjustesPage : Page
    {
        public AjustesPage()
        {
            InitializeComponent();
            // Conectamos el cerebro a la cara
            this.DataContext = new AjustesViewModel();
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // Aquí es donde en el futuro conectaremos con Properties.Settings.Default
            // o con la base de datos para guardar el nombre de la tienda, impresora, etc.

            MessageBox.Show("¡Configuración guardada correctamente!", "Sistema", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}