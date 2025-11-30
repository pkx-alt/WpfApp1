using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

// ¡Ya no necesitas casi nada de esto!
// using System.Collections.Generic;
// using System.ComponentModel;
// ...etc.

namespace OrySiPOS.Views
{
    public partial class VentaPage : Page
    {
        // Solo dejamos el placeholder, porque es 100% lógica de VISTA
        private string placeholderText = "Buscar productos por nombre o ID...";

        public VentaPage()
        {
            InitializeComponent();

            // ¡Ya no necesitas NADA de lo que estaba aquí!
            // El XAML se encarga de crear el ViewModel
            // y el ViewModel se encarga de TODO lo demás.

            SetupSearchBoxPlaceholder();
        }

        // --- LÓGICA DE UI (Placeholder) ---
        // (Esto está bien dejarlo aquí)
        private void SetupSearchBoxPlaceholder()
        {
            // OJO: Como ya no tienes "Name", debes buscarlo de otra forma
            // ¡Ah! Pero los dejé en el XAML. Si los borraste,
            // tendrías que cambiar este código.
            // Por ahora, asumiré que SearchTextBox sigue teniendo el GotFocus/LostFocus
            SearchTextBox.Text = placeholderText;
            SearchTextBox.Foreground = Brushes.Gray;
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = (sender as TextBox);
            if (textBox.Text == placeholderText)
            {
                textBox.Text = "";
                textBox.Foreground = Brushes.Black;
            }
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = (sender as TextBox);
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = placeholderText;
                textBox.Foreground = Brushes.Gray;
            }
        }

        // ¡BORRA TODO LO DEMÁS!
        // Borra:
        // - CarritoItems, ResultadosBusqueda (Propiedades)
        // - ActualizarTotales()
        // - CarritoItems_CollectionChanged()
        // - CartItem_PropertyChanged()
        // - SearchTextBox_TextChanged()
        // - SearchTextBox_KeyDown()
        // - AgregarProductoAlCarrito()
        // - ResultadosBusqueda_SelectionChanged()
        // - DecreaseQuantity_Click()
        // - IncreaseQuantity_Click()
        // - FinVentaBtn_Click()
        // - CancelarVentaBtn_Click()
        // - Button_Click()
        // - HistorialVentas_Click()
        // - GuardarVentaEnBD()
        // - RefrescarSesionEnUI()
        // - CambiarClienteButton_Click()
        // - ResumirVentaButton_Click()

        // El único que podríamos dejar es el de HistorialVentas,
        // ya que la navegación es un tema más avanzado en MVVM.
        private void HistorialVentas_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new VentasRealizadasPage());
        }

        // 1. Validar que solo entren números (Evita letras y símbolos)
        private void ValidarSoloNumeros(object sender, TextCompositionEventArgs e)
        {
            // Regex que dice: "Si NO es número, es verdadero (bloquéalo)"
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        // 2. Seleccionar todo el texto al hacer clic (Comodidad)
        private void SeleccionarTextoAlEnfocar(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.Dispatcher.BeginInvoke(new Action(() => textBox.SelectAll()));
            }
        }
        // (Y en el XAML, mantén el Click para ese botón en específico)
    }

    // ¡La clase CartItem ya NO debe vivir aquí!
    // Muévela a su propio archivo en la carpeta "Models" (Models/CartItem.cs)
}