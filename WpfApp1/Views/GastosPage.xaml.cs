// GastosPage.xaml.cs

using OrySiPOS.Dialogs;
using OrySiPOS.Models;
using OrySiPOS.ViewModels;
using OrySiPOS.Views.Dialogs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OrySiPOS.Views
{
    public partial class GastosPage : Page
    {
        public GastosViewModel ViewModel { get; set; }

        public GastosPage()
        {
            InitializeComponent();
            ViewModel = new GastosViewModel();
            this.DataContext = ViewModel;
        }

        private void btnNuevoGasto_Click(object sender, RoutedEventArgs e)
        {
            // 1. Creamos el ViewModel y la Ventana
            var modalVM = new ViewModels.RegistroGastoViewModel();
            var modalWindow = new RegistroGastoWindow();

            // 2. CONECTAMOS EL CABLE (Antes de mostrar nada)
            // Le decimos al ViewModel: "Cuando quieras cerrar, haz esto:"
            modalVM.CerrarVentana = () =>
            {
                modalWindow.DialogResult = true; // 1. Marca éxito
                modalWindow.Close();             // 2. Cierra la ventana visualmente
            };

            // 3. Asignamos el DataContext (Ahora sí)
            modalWindow.DataContext = modalVM;

            // 4. Abrimos la ventana y esperamos
            bool? result = modalWindow.ShowDialog();

            // 5. Si el resultado fue true (porque se ejecutó el código del paso 2)
            if (result == true)
            {
                // Obtenemos el gasto y lo guardamos
                var nuevoGasto = modalVM.NuevoGasto;
                ViewModel.AgregarGasto(nuevoGasto);

                MessageBox.Show("Gasto registrado con éxito.", "Guardado", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void GridGastos_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var grid = sender as DataGrid;
            if (grid?.SelectedItem is OrySiPOS.Models.Gasto gasto)
            {
                var listaDetalles = new List<ReporteItem>
                {
                    new ReporteItem { Propiedad = "Folio Gasto", Valor = gasto.Id.ToString() },
                    new ReporteItem { Propiedad = "Fecha", Valor = gasto.Fecha.ToString("F") },
                    new ReporteItem { Propiedad = "Categoría", Valor = gasto.Categoria },
                    new ReporteItem { Propiedad = "Concepto", Valor = gasto.Concepto },
                    new ReporteItem { Propiedad = "Método Pago", Valor = gasto.MetodoPago },
                    new ReporteItem { Propiedad = "Usuario", Valor = gasto.Usuario },
                    new ReporteItem { Propiedad = "Monto", Valor = gasto.Monto.ToString("C") }
                };

                var visor = new VisorReporteWindow("Detalle de Gasto", listaDetalles);
                visor.Owner = Window.GetWindow(this);
                visor.ShowDialog();
            }
        }
    }
}