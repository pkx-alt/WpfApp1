using System.Linq;
using System.Windows;
using System.Windows.Input; // <--- NECESARIO PARA ARRASTRAR
using WpfApp1.Data;

namespace WpfApp1.Views.Dialogs
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
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}