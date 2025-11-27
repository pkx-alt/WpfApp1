using System.Linq;
using System.Windows;
using System.Windows.Input; // <--- NECESARIO PARA ARRASTRAR
using OrySiPOS.Data;

namespace OrySiPOS.Views.Dialogs
{
    public partial class HistorialCajaWindow : Window
    {
        public HistorialCajaWindow()
        {
            InitializeComponent();
            CargarDatos();
        }

        // --- AGREGA ESTO ---
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
        // -------------------

        private void CargarDatos()
        {
            using (var db = new InventarioDbContext())
            {
                // Cargamos todos los cortes, ordenados del más reciente al más antiguo
                var lista = db.CortesCaja.OrderByDescending(c => c.Id).ToList();
                GridHistorial.ItemsSource = lista;

                TxtTotalRegistros.Text = lista.Count.ToString();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}