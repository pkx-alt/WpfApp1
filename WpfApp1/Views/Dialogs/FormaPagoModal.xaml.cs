using System.Windows;
using System.Windows.Input; // <--- NECESARIO PARA ARRASTRAR
using OrySiPOS.Models;
using OrySiPOS.ViewModels;

namespace OrySiPOS.Views.Dialogs
{
    public partial class FormaPagoModal : Window
    {
        // Propiedades para devolver datos a la ventana padre
        public decimal TotalAPagar { get; set; }
        public Cliente ClienteSeleccionadoEnModal { get; private set; }
        public decimal PagoRecibidoEnModal { get; private set; }
        public string FormaPagoSATEnModal { get; private set; }
        public string MetodoPagoSATEnModal { get; private set; }

        public FormaPagoModal()
        {
            InitializeComponent();
            this.Loaded += FormaPagoModal_Loaded;
        }

        // --- AGREGA ESTO PARA MOVER LA VENTANA ---
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
        // -----------------------------------------

        private void FormaPagoModal_Loaded(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as FormaPagoViewModel;

            if (vm != null)
            {
                // Pasamos el total al ViewModel
                vm.TotalAPagar = this.TotalAPagar;

                // Configuramos la acción de cierre
                vm.CloseAction = (bool result) => {

                    // Capturamos los datos finales antes de morir
                    this.ClienteSeleccionadoEnModal = vm.ClienteSeleccionado;
                    this.PagoRecibidoEnModal = vm.PagoRecibido;
                    this.FormaPagoSATEnModal = vm.FormaPagoSAT;
                    this.MetodoPagoSATEnModal = vm.MetodoPagoSAT;

                    this.DialogResult = result;
                    this.Close();
                };
            }

            // Poner foco en el campo de dinero para escribir rápido
            TxtCantidad.Focus();
            TxtCantidad.SelectAll();
        }
    }
}