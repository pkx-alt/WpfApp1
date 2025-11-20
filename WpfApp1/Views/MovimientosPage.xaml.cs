using System.Windows;
using System.Windows.Controls;
using WpfApp1.ViewModels;

namespace WpfApp1.Views
{
    public partial class MovimientosPage : Page
    {
        public MovimientosPage()
        {
            InitializeComponent();
            // Conectamos con el Cerebro
            this.DataContext = new MovimientosViewModel();
        }

        // Constructor opcional: Para cuando quieras ver el historial de UN SOLO producto
        public MovimientosPage(int productoId)
        {
            InitializeComponent();
            var vm = new MovimientosViewModel();
            vm.CargarMovimientos(productoId); // ¡Cargamos filtrado!
            this.DataContext = vm;
        }

        private void BtnVolver_Click(object sender, RoutedEventArgs e)
        {
            // Verificamos si el sistema tiene un historial hacia atrás
            if (this.NavigationService.CanGoBack)
            {
                // ¡Esta es la clave! GoBack() recupera la página anterior EXACTAMENTE como la dejaste
                this.NavigationService.GoBack();
            }
            else
            {
                // Si por alguna razón no hay historial (ej. entraste directo), vamos al inicio
                // (Aunque esto borraría los filtros, es un "plan B" seguro)
                this.NavigationService.Navigate(new Uri("Views/InventarioPage.xaml", UriKind.Relative));
            }
        }
    }
}