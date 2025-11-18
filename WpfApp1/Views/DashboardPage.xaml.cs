using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
// ¡Ya no necesitamos 'using WpfApp1;' aquí!

namespace WpfApp1.Views
{
    /// <summary>
    /// Lógica de interacción para DashboardPage.xaml
    /// </summary>
    public partial class DashboardPage : Page
    {
        public DashboardPage()
        {
            InitializeComponent();
        }

        // --- ¡ESTA ES LA NUEVA LÓGICA DE "ATAQUE DIRECTO"! ---

        /// <summary>
        /// Método de ayuda para buscar la MainWindow y llamar su método público.
        /// </summary>
        private void Navegar(string pageIdentifier)
        {
            // 1. Buscamos la Ventana (Window) que contiene esta Página (Page)
            Window parentWindow = Window.GetWindow(this);

            // 2. Comprobamos que sea nuestra 'MainWindow'
            if (parentWindow is MainWindow mainWindow)
            {
                // 3. ¡Llamamos al método PÚBLICO que creamos en MainWindow!
                mainWindow.NavigateToPage(pageIdentifier);
            }
        }

        // --- Métodos de Clic de las Tarjetas ---
        // (El XAML no cambia, sigue siendo 'MouseLeftButtonDown')

        private void CardNuevaVenta_Click(object sender, MouseButtonEventArgs e)
        {
            Navegar("NuevaVenta");
        }

        private void CardCotizacion_Click(object sender, MouseButtonEventArgs e)
        {
            Navegar("Cotizaciones");
        }

        private void CardInventario_Click(object sender, MouseButtonEventArgs e)
        {
            Navegar("Inventario");
        }

        private void CardVentasRealizadas_Click(object sender, MouseButtonEventArgs e)
        {
            Navegar("Ventas realizadas");
        }

        private void CardCaja_Click(object sender, MouseButtonEventArgs e)
        {
            Navegar("Caja");
        }

        private void CardAjustes_Click(object sender, MouseButtonEventArgs e)
        {
            Navegar("Ajustes");
        }
    }
}