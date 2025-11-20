// --- Pega ESTO en FormaPagoModal.xaml.cs ---

using System.Windows;
using WpfApp1.Models;
using WpfApp1.ViewModels;

namespace WpfApp1.Views.Dialogs
{
    public partial class FormaPagoModal : Window
    {
        public decimal TotalAPagar { get; set; }
        public Cliente ClienteSeleccionadoEnModal { get; private set; }
        public decimal PagoRecibidoEnModal { get; private set; }

        // --- ¡NUEVAS PROPIEDADES PARA DEVOLVER DATOS CFDI! ---
        public string FormaPagoSATEnModal { get; private set; }
        public string MetodoPagoSATEnModal { get; private set; }
        // ----------------------------------------------------

        public FormaPagoModal()
        {
            InitializeComponent();
            this.Loaded += FormaPagoModal_Loaded;
        }

        private void FormaPagoModal_Loaded(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as FormaPagoViewModel;

            if (vm != null)
            {
                vm.TotalAPagar = this.TotalAPagar;

                vm.CloseAction = (bool result) => {

                    // --- ¡AQUÍ ES DONDE RECUPERAMOS LOS DATOS! ---
                    this.ClienteSeleccionadoEnModal = vm.ClienteSeleccionado;
                    this.PagoRecibidoEnModal = vm.PagoRecibido;
                    this.FormaPagoSATEnModal = vm.FormaPagoSAT;      // <-- NUEVO
                    this.MetodoPagoSATEnModal = vm.MetodoPagoSAT;    // <-- NUEVO
                                                                     // ---------------------------------------------

                    this.DialogResult = result;
                    this.Close();
                };
            }

            TxtCantidad.SelectAll();
            TxtCantidad.Focus();
        }
    }
}