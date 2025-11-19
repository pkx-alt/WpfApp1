using System.Collections.Generic;
using System.Windows;

namespace WpfApp1.Views.Dialogs
{
    public partial class VisorReporteWindow : Window
    {
        public VisorReporteWindow(string titulo, IEnumerable<dynamic> datos)
        {
            InitializeComponent();
            TxtTitulo.Text = titulo;
            GridDatos.ItemsSource = datos; // ¡Aquí ocurre la magia automática!
        }
    }
}