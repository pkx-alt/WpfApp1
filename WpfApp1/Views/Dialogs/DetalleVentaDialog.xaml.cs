// --- Views/Dialogs/DetalleVentaDialog.xaml.cs ---
using System.Windows;
using WpfApp1.ViewModels; // ¡Importante! Traer el ViewModel

namespace WpfApp1.Views.Dialogs
{
    public partial class DetalleVentaDialog : Window
    {
        // ¡Este es el constructor que USARÁ nuestra página de Historial!
        public DetalleVentaDialog(int ventaId)
        {
            InitializeComponent();

            // 1. Creamos el "cerebro" (ViewModel) y le pasamos el Folio
            var vm = new DetalleVentaViewModel(ventaId);

            // 2. Le damos al VM la "llave" para cerrar esta ventana
            vm.CloseAction = (bool result) => {
                this.DialogResult = result;
                this.Close();
            };

            // 3. ¡Conectamos la Vista al ViewModel!
            this.DataContext = vm;
        }
    }
}