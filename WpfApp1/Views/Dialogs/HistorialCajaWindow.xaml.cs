using System.Linq;
using System.Windows;
using WpfApp1.Data; // Para acceder a la DB

namespace WpfApp1.Views.Dialogs
{
    public partial class HistorialCajaWindow : Window
    {
        public HistorialCajaWindow()
        {
            InitializeComponent();
            CargarDatos();
        }

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