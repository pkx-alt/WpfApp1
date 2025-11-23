using System.Collections.Generic;
using System.Windows;
using System.Windows.Input; // <--- NECESARIO
using WpfApp1.Models;

namespace WpfApp1.Views.Dialogs
{
    public partial class MoverSubcategoriasDialog : Window
    {
        public Categoria CategoriaDestino { get; private set; }

        public MoverSubcategoriasDialog(int conteo, List<Categoria> categoriasDestino)
        {
            InitializeComponent();

            // Actualizamos el mensaje informativo
            ConteoTextBlock.Text = $"Se moverán {conteo} subcategoría(s) seleccionada(s).";

            CategoriasComboBox.ItemsSource = categoriasDestino;

            if (categoriasDestino.Count > 0)
            {
                CategoriasComboBox.SelectedIndex = 0;
            }
        }

        // --- AGREGA ESTO ---
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
        // -------------------

        private void MoverButton_Click(object sender, RoutedEventArgs e)
        {
            if (CategoriasComboBox.SelectedItem is Categoria seleccionada)
            {
                CategoriaDestino = seleccionada;
                this.DialogResult = true;
            }
            else
            {
                MessageBox.Show("Debes seleccionar una categoría de destino.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}