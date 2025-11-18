using System.Windows;
using System.Windows.Controls;

namespace WpfApp1.Views
{
    public partial class AjustesPage : Page
    {
        public AjustesPage()
        {
            InitializeComponent();
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // Aquí es donde en el futuro conectaremos con Properties.Settings.Default
            // o con la base de datos para guardar el nombre de la tienda, impresora, etc.

            MessageBox.Show("¡Configuración guardada correctamente!", "Sistema", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}