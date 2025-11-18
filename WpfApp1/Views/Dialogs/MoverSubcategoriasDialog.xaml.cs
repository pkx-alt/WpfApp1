using System.Collections.Generic;
using System.Windows;
using WpfApp1.Models; // ¡Necesitamos Categoria!

namespace WpfApp1.Views.Dialogs
{
    public partial class MoverSubcategoriasDialog : Window
    {
        // Propiedad pública para que el ViewModel "lea" la respuesta
        public Categoria CategoriaDestino { get; private set; }

        public MoverSubcategoriasDialog(int conteo, List<Categoria> categoriasDestino)
        {
            InitializeComponent();

            // Llenamos el texto y el ComboBox
            ConteoTextBlock.Text = $"Vas a mover {conteo} subcategoría(s)";
            CategoriasComboBox.ItemsSource = categoriasDestino;

            // Seleccionamos la primera por defecto
            if (categoriasDestino.Count > 0)
            {
                CategoriasComboBox.SelectedIndex = 0;
            }
        }

        private void MoverButton_Click(object sender, RoutedEventArgs e)
        {
            if (CategoriasComboBox.SelectedItem is Categoria seleccionada)
            {
                CategoriaDestino = seleccionada;
                this.DialogResult = true; // Cierra y dice "OK"
            }
            else
            {
                MessageBox.Show("Debes seleccionar una categoría de destino.");
            }
        }
    }
}