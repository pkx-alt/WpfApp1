using System.Windows;
using System.Windows.Input; // <--- NECESARIO
using OrySiPOS.ViewModels;

namespace OrySiPOS.Views.Dialogs
{
    public partial class DetalleCotizacionDialog : Window
    {
        public bool DeseaConvertir { get; private set; } = false;

        public DetalleCotizacionDialog(int cotizacionId)
        {
            InitializeComponent();

            var vm = new DetalleCotizacionViewModel(cotizacionId);

            vm.CloseAction = (bool result) => {
                this.DialogResult = result;
                this.Close();
            };

            vm.ConvertirAction = () => {
                this.DeseaConvertir = true;
                this.DialogResult = true;
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