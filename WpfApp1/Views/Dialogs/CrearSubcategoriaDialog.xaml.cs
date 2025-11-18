using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WpfApp1.Views.Dialogs
{
    /// <summary>
    /// Lógica de interacción para CrearSubcategoriaDialog.xaml
    /// </summary>
    public partial class CrearSubcategoriaDialog : Window
    {
        // Esta propiedad pública la usará nuestro ViewModel
        // para "leer" el nombre que escribió el usuario.
        public string NombreSubcategoria { get; private set; }

        public CrearSubcategoriaDialog()
        {
            InitializeComponent();
            NombreTextBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(NombreTextBox.Text))
            {
                NombreSubcategoria = NombreTextBox.Text;
                this.DialogResult = true; // Cierra la ventana y dice "OK"
            }
            else
            {
                MessageBox.Show("El nombre no puede estar vacío.", "Error");
            }
        }
    }
}
