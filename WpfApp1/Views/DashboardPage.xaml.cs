using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OrySiPOS.ViewModels; // <--- ¡No olvides este using!

namespace OrySiPOS.Views
{
    public partial class DashboardPage : Page
    {
        // Guardamos una referencia al ViewModel para poder llamarlo luego
        private DashboardViewModel _vm;

        public DashboardPage()
        {
            InitializeComponent();

            // 1. Instanciamos el ViewModel
            _vm = new DashboardViewModel();

            // 2. Se lo asignamos a la página
            this.DataContext = _vm;

            // 3. Nos suscribimos al evento "Loaded".
            // Esto se dispara CADA VEZ que la página aparece en pantalla.
            this.Loaded += DashboardPage_Loaded;
        }

        private void DashboardPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Recargamos los números para que estén frescos
            _vm.CargarMetricas();
        }

        // --- TUS MÉTODOS DE NAVEGACIÓN (Déjalos tal cual estaban) ---
        private void Navegar(string pageIdentifier)
        {
            Window parentWindow = Window.GetWindow(this);
            if (parentWindow is MainWindow mainWindow)
            {
                mainWindow.NavigateToPage(pageIdentifier);
            }
        }

        private void CardNuevaVenta_Click(object sender, MouseButtonEventArgs e) => Navegar("NuevaVenta");
        private void CardCotizacion_Click(object sender, MouseButtonEventArgs e) => Navegar("Cotizaciones");
        private void CardInventario_Click(object sender, MouseButtonEventArgs e) => Navegar("Inventario");
        private void CardVentasRealizadas_Click(object sender, MouseButtonEventArgs e) => Navegar("Ventas realizadas");
        private void CardCaja_Click(object sender, MouseButtonEventArgs e) => Navegar("Caja");
        private void CardAjustes_Click(object sender, MouseButtonEventArgs e) => Navegar("Ajustes");
    }
}