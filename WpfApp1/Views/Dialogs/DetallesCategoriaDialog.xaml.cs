using System.Windows;
using WpfApp1.Models; // Necesitamos esto

namespace WpfApp1.Views.Dialogs
{
    public partial class DetallesCategoriaDialog : Window
    {
        // Un constructor que "acepta" los datos que le manda el ViewModel
        public DetallesCategoriaDialog(string nombre, string descripcion, int subCount, int prodCount)
        {
            InitializeComponent();

            // Llenamos los campos del XAML
            NombreTextBlock.Text = nombre;
            DescripcionTextBlock.Text = descripcion; // (Lo dejaremos vacío por ahora)
            SubCountTextBlock.Text = subCount.ToString();
            ProdCountTextBlock.Text = prodCount.ToString();

            // Lógica de la descripción (por si no tenemos una)
            if (string.IsNullOrWhiteSpace(descripcion))
            {
                DescripcionTextBlock.Text = "(Sin descripción)";
                DescripcionTextBlock.FontStyle = FontStyles.Italic;
            }
        }

        // Este botón le "avisará" al ViewModel que queremos editar
        private void EditarButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true; // Cierra la ventana y dice "¡Quiero Editar!"
        }
    }
}