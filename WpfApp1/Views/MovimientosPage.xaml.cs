using OrySiPOS.ViewModels;
using OrySiPOS.Views.Dialogs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OrySiPOS.Views
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

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var grid = sender as DataGrid;
            if (grid?.SelectedItem is OrySiPOS.Models.MovimientoInventario mov)
            {
                // 1. Preparamos los datos
                var listaDetalles = new List<ReporteItem>
                {
                    new ReporteItem { Propiedad = "Folio Movimiento", Valor = mov.Id.ToString() },
                    new ReporteItem { Propiedad = "Fecha y Hora", Valor = mov.Fecha.ToString("F") }, // Formato completo
                    new ReporteItem { Propiedad = "Producto", Valor = mov.Producto?.Descripcion ?? "N/A" },
                    new ReporteItem { Propiedad = "SKU Producto", Valor = mov.ProductoId.ToString() },
                    new ReporteItem { Propiedad = "Tipo Acción", Valor = mov.TipoMovimiento },
                    new ReporteItem { Propiedad = "Cantidad Afectada", Valor = mov.Cantidad.ToString() },
                    new ReporteItem { Propiedad = "Stock Antes", Valor = mov.StockAnterior.ToString() },
                    new ReporteItem { Propiedad = "Stock Después", Valor = mov.StockNuevo.ToString() },
                    new ReporteItem { Propiedad = "Motivo / Nota", Valor = mov.Motivo },
                    new ReporteItem { Propiedad = "Usuario", Valor = mov.Usuario }
                };

                // 2. Abrimos el visor
                var visor = new VisorReporteWindow("Detalle del Movimiento", listaDetalles);
                visor.Owner = Window.GetWindow(this);
                visor.ShowDialog();
            }
        }
    }
}