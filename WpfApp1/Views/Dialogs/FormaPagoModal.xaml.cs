// --- Pega ESTO en FormaPagoModal.xaml.cs ---

using System.Windows;
using WpfApp1.Models;
using WpfApp1.ViewModels; // ¡IMPORTANTE! Traer nuestro VM

namespace WpfApp1.Views.Dialogs
{
    public partial class FormaPagoModal : Window
    {
        // Esta es la "puerta" por donde VentaPage nos da el total.
        public decimal TotalAPagar { get; set; }
        public Cliente ClienteSeleccionadoEnModal { get; private set; }
        public decimal PagoRecibidoEnModal { get; private set; }

        public FormaPagoModal()
        {
            InitializeComponent();

            // Nos enganchamos al evento Loaded AQUÍ en C#
            this.Loaded += FormaPagoModal_Loaded;
        }

        private void FormaPagoModal_Loaded(object sender, RoutedEventArgs e)
        {
            // 1. Obtenemos la instancia del ViewModel que creó el XAML
            var vm = this.DataContext as FormaPagoViewModel;

            if (vm != null)
            {
                // 2. Le "pasamos" el total de nuestra puerta a su propiedad
                vm.TotalAPagar = this.TotalAPagar;

                // 3. ¡EL TRUCO! Le damos al VM la "llave" para cerrarse
                //    Le decimos: "Cuando llames a CloseAction, yo ejecutaré esto"
                vm.CloseAction = (bool result) => {
                    // ¡Antes de cerrar, "robamos" el cliente del VM!
                    this.ClienteSeleccionadoEnModal = vm.ClienteSeleccionado;
                    // --- ¡AÑADE ESTA LÍNEA! ---
                    this.PagoRecibidoEnModal = vm.PagoRecibido;
                    // --- FIN DE LÍNEA NUEVA ---
                    this.DialogResult = result; // Ponemos el resultado (true/false)
                    this.Close();               // Cerramos la ventana
                };
            }

            // 4. Mantenemos esta lógica de UI (¡esto sí es trabajo del View!)
            //    (Asegúrate de que TxtCantidad SÍ tenga x:Name en el XAML)
            TxtCantidad.SelectAll();
            TxtCantidad.Focus();
        }

        // ¡¡TODO LO DEMÁS (ActualizarCambio, BtnClick, etc.) DEBE SER BORRADO!!
    }
}