// GastosPage.xaml.cs

using System.Windows;
using System.Windows.Controls;
using WpfApp1.Dialogs;
using WpfApp1.Models;
using WpfApp1.ViewModels;

namespace WpfApp1.Views
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
    }
}