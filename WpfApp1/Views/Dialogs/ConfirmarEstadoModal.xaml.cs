// --- Archivo: ConfirmarEstadoModal.xaml.cs ---

using System.Windows;
using System.Windows.Media; // Para los colores
using WpfApp1.Models;     // ¡Importante!

namespace WpfApp1.Views.Dialogs
{
    public partial class ConfirmarEstadoModal : Window
    {
        private Producto _producto;
        private bool _deshabilitando; // Guardamos la acción

        public ConfirmarEstadoModal(Producto producto, bool esParaDeshabilitar)
        {
            InitializeComponent();
            _producto = producto;
            _deshabilitando = esParaDeshabilitar;

            // Conectamos el producto al DataContext (para la imagen y descripción)
            this.DataContext = _producto;

            // ¡Aquí ocurre la magia!
            ConfigurarVentana();
        }

        private void ConfigurarVentana()
        {
            if (_deshabilitando)
            {
                // --- Configuración para DESHABILITAR ---
                TituloTextBlock.Text = "Deshabilitar producto";
                PreguntaTextBlock.Text = "¿Estás seguro de que quieres deshabilitar el producto?";

                PuntosDeshabilitar.Visibility = Visibility.Visible;
                PuntosHabilitar.Visibility = Visibility.Collapsed;

                BotonConfirmar.Content = "Sí, deshabilitar";
                BotonConfirmar.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#D32F2F")); // Rojo
            }
            else
            {
                // --- Configuración para HABILITAR ---
                TituloTextBlock.Text = "Habilitar producto";
                PreguntaTextBlock.Text = "¿Estás seguro de que quieres volver a habilitar el producto?";

                PuntosDeshabilitar.Visibility = Visibility.Collapsed;
                NotaDeshabilitar.Visibility = Visibility.Collapsed; // Ocultamos la nota roja
                PuntosHabilitar.Visibility = Visibility.Visible;

                BotonConfirmar.Content = "Sí, habilitar";
                BotonConfirmar.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#4CAF50")); // Verde
            }
        }

        private void Confirmar_Click(object sender, RoutedEventArgs e)
        {
            // El usuario dijo "Sí". Cerramos con resultado "OK".
            this.DialogResult = true;
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            // El usuario dijo "No". Cerramos con resultado "Cancel".
            this.DialogResult = false;
        }
    }
}