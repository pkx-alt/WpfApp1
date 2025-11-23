using System.Collections.Generic;
using System.Windows;
using System.Windows.Input; // <--- NECESARIO

namespace WpfApp1.Views.Dialogs
{
    public partial class VisorReporteWindow : Window
    {
        public VisorReporteWindow(string titulo, IEnumerable<dynamic> datos)
        {
            InitializeComponent();
            TxtTitulo.Text = titulo;
            GridDatos.ItemsSource = datos;
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