using System.Windows;
using System.Windows.Input; // <--- Necesario para arrastrar

namespace OrySiPOS.Views.Dialogs
{
    public partial class CrearCategoriaDialog : Window
    {
        public string NombreCategoria { get; private set; }

        public CrearCategoriaDialog()
        {
            InitializeComponent();
        }

        // --- 1. ARRASTRAR VENTANA ---
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        // --- 2. SELECCIONAR TEXTO AL INICIAR ---
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Ponemos el foco en la cajita
            NombreTextBox.Focus();
            // Seleccionamos todo el texto (útil para editar rápido)
            NombreTextBox.SelectAll();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(NombreTextBox.Text))
            {
                NombreCategoria = NombreTextBox.Text;
                this.DialogResult = true;
            }
            else
            {
                MessageBox.Show("El nombre no puede estar vacío.", "Dato requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) // <--- Cambié el nombre para que coincida con el XAML nuevo
        {
            this.DialogResult = false;
        }
    }
}