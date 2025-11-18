using System.Text.RegularExpressions; // Para validar números
using System.Windows;
using System.Windows.Input;

namespace WpfApp1.Views.Dialogs
{
    public partial class DescuentoDialog : Window
    {
        // --- Propiedades Públicas para leer el resultado ---
        public decimal Valor { get; private set; }
        public bool EsPorcentaje { get; private set; }

        public DescuentoDialog()
        {
            InitializeComponent();

            // Enfocar el TextBox al abrir
            Loaded += (s, e) => ValorTextBox.Focus();
        }

        private void AceptarButton_Click(object sender, RoutedEventArgs e)
        {
            // Validar que se escribió un número válido
            if (decimal.TryParse(ValorTextBox.Text, out decimal valor))
            {
                if (valor <= 0)
                {
                    MessageBox.Show("El valor debe ser mayor a cero.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Guardar los resultados en las propiedades públicas
                this.Valor = valor;
                this.EsPorcentaje = PorcentajeRadioButton.IsChecked == true;

                // Cerrar el diálogo y devolver "OK"
                this.DialogResult = true;
            }
            else
            {
                MessageBox.Show("Por favor, ingresa un valor numérico válido.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            // Cerrar el diálogo y devolver "Cancel"
            this.DialogResult = false;
        }

        // --- Eventos de UI ---

        // Validar que solo se escriban números y un punto decimal
        private void ValorTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Expresión Regular: solo números, y un solo punto decimal
            var regex = new Regex(@"^[0-9]*(\.[0-9]*)?$");
            string fullText = ValorTextBox.Text.Insert(ValorTextBox.CaretIndex, e.Text);

            e.Handled = !regex.IsMatch(fullText);
        }

        // Permitir Enter/Esc
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AceptarButton_Click(null, null);
            }
            else if (e.Key == Key.Escape)
            {
                CancelarButton_Click(null, null);
            }
        }
    }
}