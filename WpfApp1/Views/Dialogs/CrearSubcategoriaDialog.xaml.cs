using System.Windows;
using System.Windows.Input;

namespace OrySiPOS.Views.Dialogs
{
    public partial class CrearSubcategoriaDialog : Window
    {
        public string NombreSubcategoria { get; private set; }

        public CrearSubcategoriaDialog()
        {
            InitializeComponent();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            NombreTextBox.Focus();
            NombreTextBox.SelectAll();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(NombreTextBox.Text))
            {
                NombreSubcategoria = NombreTextBox.Text;
                this.DialogResult = true;
            }
            else
            {
                MessageBox.Show("El nombre no puede estar vacío.", "Dato requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Agregué este manejador para el botón Cancelar
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}