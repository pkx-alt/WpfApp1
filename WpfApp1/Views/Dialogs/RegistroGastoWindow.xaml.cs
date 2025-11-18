// RegistroGastoWindow.xaml.cs (CÓDIGO LIMPIO)
using System.Windows;

namespace WpfApp1.Dialogs
{
    public partial class RegistroGastoWindow : Window
    {
        public RegistroGastoWindow()
        {
            InitializeComponent();
            // BORRAMOS TODO EL IF DEL DATACONTEXT AQUÍ
            // Dejamos que GastosPage se encargue de eso.
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}