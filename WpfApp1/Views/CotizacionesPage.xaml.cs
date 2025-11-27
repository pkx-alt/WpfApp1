using OrySiPOS.ViewModels;
using OrySiPOS.Views.Dialogs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace OrySiPOS.Views // Asegúrate que este namespace sea el correcto
{
    public partial class CotizacionesPage : Page
    {
        // Texto que usamos de placeholder (marcador de posición)
        private const string PlaceholderText = "Buscar por folio, cliente....";

        public CotizacionesPage()
        {
            InitializeComponent();
            this.DataContext = new CotizacionesViewModel();
        }

        // --- 1. EVENTO DE BÚSQUEDA (El que hace la magia) ---
        private void txtBusqueda_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Obtenemos acceso al cerebro (ViewModel)
            if (this.DataContext is CotizacionesViewModel vm)
            {
                // Si el texto es el placeholder, buscamos "cadena vacía" (para ver todo)
                // Si es texto real, se lo pasamos al VM.
                if (txtBusqueda.Text == PlaceholderText)
                {
                    vm.TextoBusqueda = "";
                }
                else
                {
                    vm.TextoBusqueda = txtBusqueda.Text;
                }
            }
        }

        // --- 2. EVENTOS VISUALES (Placeholder gris) ---
        // (Estos ya los tenías o los habías pedido, los dejo aquí por si acaso)

        private void txtBusqueda_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtBusqueda.Text == PlaceholderText)
            {
                txtBusqueda.Text = "";
                txtBusqueda.Foreground = Brushes.Black;
            }
        }

        private void txtBusqueda_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBusqueda.Text))
            {
                txtBusqueda.Text = PlaceholderText;
                txtBusqueda.Foreground = Brushes.Gray;
            }
        }

        // --- LÓGICA DEL BOTÓN NUEVA COTIZACIÓN ---
        private void BtnNuevaCotizacion_Click(object sender, RoutedEventArgs e)
        {
            // 1. Creamos la instancia de la página de ventas
            var paginaVenta = new VentaPage();

            // 2. Obtenemos su ViewModel (el cerebro)
            // OJO: Asegúrate que VentaPage asigne su ViewModel en el constructor o en el XAML
            if (paginaVenta.DataContext is VentaViewModel vm)
            {
                // 3. ¡Activamos el modo cotización!
                vm.EsModoCotizacion = true;
            }

            // 4. Navegamos hacia allá
            this.NavigationService.Navigate(paginaVenta);
        }

        // --- NUEVO: LÓGICA PARA VER Y CONVERTIR ---
        private void BtnVerDetalle_Click(object sender, RoutedEventArgs e)
        {
            // 1. Obtenemos el botón que se presionó
            if (sender is Button btn)
            {
                // 2. Sacamos el ID (Folio) que guardamos en la propiedad Tag
                int idCotizacion = int.Parse(btn.Tag.ToString());

                // 3. Abrimos la ventana de detalle (La que creaste como imagen)
                var dialogo = new DetalleCotizacionDialog(idCotizacion);
                dialogo.Owner = Window.GetWindow(this); // Para que salga centrada

                // 4. Mostramos la ventana y esperamos a que se cierre
                bool? resultado = dialogo.ShowDialog();

                // 5. VERIFICAMOS: ¿El usuario le dio clic a "Convertir a venta"?
                if (resultado == true && dialogo.DeseaConvertir)
                {
                    // ¡SÍ! El usuario quiere vender esto.

                    // A. Creamos la página de ventas
                    var paginaVenta = new VentaPage();

                    // B. Accedemos a su cerebro (ViewModel)
                    if (paginaVenta.DataContext is VentaViewModel vmVenta)
                    {
                        // C. ¡Le cargamos los datos de la cotización!
                        // (Este método lo creamos en el paso anterior)
                        vmVenta.CargarDatosDeCotizacion(idCotizacion);
                    }

                    // D. Nos vamos a la pantalla de ventas
                    this.NavigationService.Navigate(paginaVenta);
                }
            }
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // El 'sender' es el DataGrid. Verificamos qué ítem se clickeó.
            if (sender is DataGrid grid && grid.SelectedItem is OrySiPOS.ViewModels.CotizacionesViewModel.CotizacionItemViewModel itemSeleccionado)
            {
                // itemSeleccionado es la fila donde hicieron clic.
                // Obtenemos el ID (Folio) directamente del objeto.
                int idCotizacion = itemSeleccionado.Folio;

                // --- AQUÍ REPETIMOS LA LÓGICA DE ABRIR VENTANA ---
                // (Es la misma lógica que tienes en BtnVerDetalle_Click, pero usando el ID directo)

                var dialogo = new DetalleCotizacionDialog(idCotizacion);
                dialogo.Owner = Window.GetWindow(this);

                bool? resultado = dialogo.ShowDialog();

                if (resultado == true && dialogo.DeseaConvertir)
                {
                    // Lógica de conversión a venta
                    var paginaVenta = new VentaPage();
                    if (paginaVenta.DataContext is VentaViewModel vmVenta)
                    {
                        vmVenta.CargarDatosDeCotizacion(idCotizacion);
                    }
                    this.NavigationService.Navigate(paginaVenta);
                }
            }
        }
    }
}