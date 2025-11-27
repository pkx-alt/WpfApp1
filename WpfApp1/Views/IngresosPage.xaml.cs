using OrySiPOS.Models;
using OrySiPOS.ViewModels;
using OrySiPOS.Views.Dialogs;
using System.Linq; // Necesario para la consulta
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OrySiPOS.Views
{
    public partial class IngresosPage : Page
    {
        // Usaremos este ViewModel para cargar los datos de la tabla
        public IngresosViewModel ViewModel { get; set; }

        public IngresosPage()
        {
            InitializeComponent();

            // Instanciamos el ViewModel de Ingresos (Lo crearemos en el siguiente paso)
            // Asume que necesitas un IngresosViewModel para manejar la lista.
            ViewModel = new IngresosViewModel();
            this.DataContext = ViewModel;
        }

        private void BtnRegistrarIngreso_Click(object sender, RoutedEventArgs e)
        {
            // 1. Preparamos el Modal (reutilizando la lógica de cierre segura)
            var modalVM = new RegistroIngresoViewModel();
            var modalWindow = new RegistroIngresoWindow(); // Usaremos la ventana verde creada antes

            // 2. Conectamos el delegado de cierre (El botón Guardar en el modal llama a este código)
            modalVM.CerrarVentana = () =>
            {
                modalWindow.DialogResult = true;
                modalWindow.Close();
            };

            // 3. Asignamos Contexto y mostramos
            modalWindow.DataContext = modalVM;
            modalWindow.Owner = Window.GetWindow(this);
            bool? result = modalWindow.ShowDialog();

            // 4. Si el resultado es OK, guardamos y actualizamos la vista
            if (result == true)
            {
                Ingreso nuevoIngreso = modalVM.NuevoIngreso;

                // Llamamos al método que guarda y actualiza la lista
                // (Este método debe estar en IngresosViewModel)
                ViewModel.AgregarIngreso(nuevoIngreso);

                MessageBox.Show("Ingreso registrado con éxito.", "Guardado", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void GridIngresos_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var grid = sender as DataGrid;
            if (grid?.SelectedItem is OrySiPOS.Models.Ingreso ingreso)
            {
                var listaDetalles = new List<ReporteItem>
                {
                    new ReporteItem { Propiedad = "Folio Ingreso", Valor = ingreso.Id.ToString() },
                    new ReporteItem { Propiedad = "Fecha", Valor = ingreso.Fecha.ToString("F") }, // Fecha completa
                    new ReporteItem { Propiedad = "Categoría", Valor = ingreso.Categoria },
                    new ReporteItem { Propiedad = "Concepto", Valor = ingreso.Concepto },
                    new ReporteItem { Propiedad = "Usuario", Valor = ingreso.Usuario },
                    new ReporteItem { Propiedad = "Monto", Valor = ingreso.Monto.ToString("C") }
                };

                var visor = new VisorReporteWindow("Detalle de Ingreso", listaDetalles);
                visor.Owner = Window.GetWindow(this);
                visor.ShowDialog();
            }
        }
    }
}