using System.Windows;
using System.Windows.Input; // <--- NECESARIO

namespace OrySiPOS.Dialogs // Asegúrate de que el namespace coincida con donde está tu archivo
{
    public partial class RegistroGastoWindow : Window
    {
        public RegistroGastoWindow()
        {
            InitializeComponent();
        }

        // --- AGREGA ESTO ---
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
        // -------------------

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}