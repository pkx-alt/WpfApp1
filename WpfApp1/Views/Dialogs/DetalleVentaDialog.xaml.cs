using System.Windows;
using System.Windows.Input; // <--- NECESARIO
using OrySiPOS.ViewModels;

namespace OrySiPOS.Views.Dialogs
{
    public partial class DetalleVentaDialog : Window
    {
        public DetalleVentaDialog(int ventaId)
        {
            InitializeComponent();

            var vm = new DetalleVentaViewModel(ventaId);

            vm.CloseAction = (bool result) => {
                this.DialogResult = result;
                this.Close();
            };

            this.DataContext = vm;
        }

        // --- AGREGA ESTO ---
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
        // -------------------
    }
}