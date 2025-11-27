using System;
using System.Windows;
using System.Windows.Navigation; // <-- ¡Puede que necesites este!

namespace OrySiPOS
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Carga la página inicial al arrancar
            // (Usamos el nuevo método para que el sidebar también se pinte)
            NavigateToPage("Dashboard");
        }

        // -----------------------------------------------------------------
        // --- ¡AQUÍ ESTÁ LA LÓGICA NUEVA Y CENTRALIZADA! ---
        // -----------------------------------------------------------------

        /// <summary>
        /// Este es AHORA el único método que realiza la navegación.
        /// Es público para que las páginas (como Dashboard) puedan llamarlo.
        /// </summary>
        /// <param name="pageIdentifier">El 'case' del switch. Ej: "Dashboard", "Inventario", etc.</param>
        public void NavigateToPage(string pageIdentifier)
        {
            Uri pageUri = null;

            // Mapeamos el nombre del botón a la URI de la página
            switch (pageIdentifier)
            {
                case "Dashboard":
                    pageUri = new Uri("Views/DashboardPage.xaml", UriKind.Relative);
                    break;
                case "NuevaVenta":
                    pageUri = new Uri("Views/VentaPage.xaml", UriKind.Relative);
                    break;
                case "Caja":
                    pageUri = new Uri("Views/CajaPage.xaml", UriKind.Relative);
                    break;
                case "Cotizaciones":
                    pageUri = new Uri("Views/CotizacionesPage.xaml", UriKind.Relative);
                    break;
                case "Ventas realizadas":
                    pageUri = new Uri("Views/VentasRealizadasPage.xaml", UriKind.Relative);
                    break;
                case "Inventario":
                    pageUri = new Uri("Views/InventarioPage.xaml", UriKind.Relative);
                    break;
                case "Movimientos":
                    // Asegúrate de haber creado el archivo MovimientosPage.xaml en la carpeta Views
                    pageUri = new Uri("Views/MovimientosPage.xaml", UriKind.Relative);
                    break;
                case "Clientes":
                    pageUri = new Uri("Views/ClientesPage.xaml", UriKind.Relative);
                    break;
                case "Departamentos":
                    pageUri = new Uri("Views/DepartamentosPage.xaml", UriKind.Relative);
                    break;
                case "Facturación":
                    pageUri = new Uri("Views/FacturacionPage.xaml", UriKind.Relative);
                    break;
                case "Ingresos":
                    pageUri = new Uri("Views/IngresosPage.xaml", UriKind.Relative);
                    break;
                case "Gastos":
                    pageUri = new Uri("Views/GastosPage.xaml", UriKind.Relative);
                    break;
                case "Cuentas por cobrar":
                    pageUri = new Uri("Views/CuentasCobrarPage.xaml", UriKind.Relative);
                    break;
                case "Reportes":
                    pageUri = new Uri("Views/ReportesPage.xaml", UriKind.Relative);
                    break;
                case "Estadísticas":
                    pageUri = new Uri("Views/EstadisticasPage.xaml", UriKind.Relative);
                    break;
                case "Ajustes":
                    pageUri = new Uri("Views/AjustesPage.xaml", UriKind.Relative);
                    break;
                default:
                    pageUri = new Uri("Views/DashboardPage.xaml", UriKind.Relative);
                    break;
            }

            // 1. Navegamos el Frame a la página correspondiente
            if (pageUri != null)
            {
                MainFrame.Navigate(pageUri);
            }

            // 2. ¡EL GRAN BONUS!
            //    Actualizamos la propiedad del sidebar para que se pinte el botón correcto.
            //    Ahora, no importa QUIÉN llame a este método (el sidebar o el dashboard),
            //    ¡el sidebar siempre se actualizará!
            Sidebar.ActivePage = pageIdentifier;
        }

        // Este es tu método original, ahora es SÚPER SIMPLE.
        // Solo extrae el nombre y se lo pasa al método 'NavigateToPage'.
        private void Sidebar_NavigationRequested(object sender, RoutedEventArgs e)
        {
            // El parámetro que pasamos es el Content del botón, ej: "Ventas realizadas"
            string pageName = e.OriginalSource.ToString();

            // ¡Llamamos al método centralizado!
            NavigateToPage(pageName);
        }
    }
}