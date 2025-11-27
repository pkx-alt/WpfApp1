using System.Collections.Generic;
using System.Windows;
using System.Windows.Input; // <--- NECESARIO

namespace OrySiPOS.Views.Dialogs
{
    // 1. CAMBIAMOS EL NOMBRE AQUÍ DE "DetalleItem" A "ReporteItem"
    public class ReporteItem
    {
        public string Propiedad { get; set; }
        public string Valor { get; set; }
    }
    // ------------------------------------------------
    public partial class VisorReporteWindow : Window
    {
        // 2. ACTUALIZAMOS EL CONSTRUCTOR PARA RECIBIR "ReporteItem"
        public VisorReporteWindow(string titulo, IEnumerable<ReporteItem> datos)
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