using System.Collections.Generic;
using System.Windows;

namespace WpfApp1.Views.Dialogs
{
    public partial class ResultadosImportacionWindow : Window
    {
        // Constructor que recibe el resumen y la lista de datos
        public ResultadosImportacionWindow(string textoResumen, IEnumerable<dynamic> listaDetalles)
        {
            InitializeComponent();

            // Asignamos los datos
            TxtResumen.Text = textoResumen;
            GridDetalles.ItemsSource = listaDetalles;
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}