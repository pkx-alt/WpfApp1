using System.Collections.Generic;
using System.Windows;
using System.Windows.Input; // <--- NECESARIO

namespace WpfApp1.Views.Dialogs
{
    public partial class ResultadosImportacionWindow : Window
    {
        public ResultadosImportacionWindow(string textoResumen, IEnumerable<dynamic> listaDetalles)
        {
            InitializeComponent();

            TxtResumen.Text = textoResumen;
            GridDetalles.ItemsSource = listaDetalles;
        }

        // --- AGREGA ESTO ---
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
        // -------------------

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}