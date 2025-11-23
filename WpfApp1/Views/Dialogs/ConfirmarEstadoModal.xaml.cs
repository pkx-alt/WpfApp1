using System.Windows;
using System.Windows.Input; // <--- IMPORTANTE
using System.Windows.Media;
using WpfApp1.Models;

namespace WpfApp1.Views.Dialogs
{
    public partial class ConfirmarEstadoModal : Window
    {
        private Producto _producto;
        private bool _deshabilitando;

        public ConfirmarEstadoModal(Producto producto, bool esParaDeshabilitar)
        {
            InitializeComponent();
            _producto = producto;
            _deshabilitando = esParaDeshabilitar;
            this.DataContext = _producto;
            ConfigurarVentana();
        }

        // --- NUEVO: ARRASTRAR ---
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
        // ------------------------

        private void ConfigurarVentana()
        {
            if (_deshabilitando)
            {
                TituloTextBlock.Text = "Deshabilitar Producto";
                TituloTextBlock.Foreground = (Brush)Application.Current.Resources["DangerColor"];

                BotonConfirmar.Content = "Deshabilitar";
                BotonConfirmar.Style = (Style)Application.Current.Resources["BtnDanger"]; // Usamos estilo Danger

                PanelInfo.Visibility = Visibility.Visible; // Mostramos las advertencias
            }
            else
            {
                TituloTextBlock.Text = "Reactivar Producto";
                TituloTextBlock.Foreground = (Brush)Application.Current.Resources["SuccessColor"];

                BotonConfirmar.Content = "Reactivar";
                BotonConfirmar.Style = (Style)Application.Current.Resources["BtnSuccess"]; // Usamos estilo Success

                PanelInfo.Visibility = Visibility.Collapsed; // Ocultamos advertencias para reactivar
            }
        }

        private void Confirmar_Click(object sender, RoutedEventArgs e) => this.DialogResult = true;
        private void Cancelar_Click(object sender, RoutedEventArgs e) => this.DialogResult = false;
    }
}