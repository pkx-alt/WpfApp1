// Views/Dialogs/CrearCategoriaDialog.xaml.cs
using System.Windows;

namespace WpfApp1.Views.Dialogs
{
    public partial class CrearCategoriaDialog : Window
    {
        // Esta propiedad pública la usará nuestro ViewModel
        // para "leer" el nombre que escribió el usuario.
        public string NombreCategoria { get; private set; }

        public CrearCategoriaDialog()
        {
            InitializeComponent();
            NombreTextBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(NombreTextBox.Text))
            {
                NombreCategoria = NombreTextBox.Text;
                this.DialogResult = true; // Cierra la ventana y dice "OK"
            }
            else
            {
                MessageBox.Show("El nombre no puede estar vacío.", "Error");
            }
        }
    }
}