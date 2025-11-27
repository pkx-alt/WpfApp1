using System.Windows;
using System.Windows.Input; // <--- NECESARIO
using OrySiPOS.Models;

namespace OrySiPOS.Views.Dialogs
{
    public partial class DetallesCategoriaDialog : Window
    {
        public DetallesCategoriaDialog(string nombre, string descripcion, int subCount, int prodCount)
        {
            InitializeComponent();

            NombreTextBlock.Text = nombre;
            DescripcionTextBlock.Text = descripcion;
            SubCountTextBlock.Text = subCount.ToString();
            ProdCountTextBlock.Text = prodCount.ToString();

            if (string.IsNullOrWhiteSpace(descripcion))
            {
                DescripcionTextBlock.Text = "(Sin descripción)";
                DescripcionTextBlock.FontStyle = FontStyles.Italic;
                DescripcionTextBlock.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        // --- AGREGA ESTO ---
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
        // -------------------

        private void EditarButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void CerrarButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}