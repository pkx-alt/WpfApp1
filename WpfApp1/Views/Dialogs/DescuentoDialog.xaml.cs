using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input; // <--- NECESARIO

namespace WpfApp1.Views.Dialogs
{
    public partial class DescuentoDialog : Window
    {
        public decimal Valor { get; private set; }
        public bool EsPorcentaje { get; private set; }

        public DescuentoDialog()
        {
            InitializeComponent();
            Loaded += (s, e) => ValorTextBox.Focus();
        }

        // --- AGREGA ESTO ---
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
        // -------------------

        private void AceptarButton_Click(object sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(ValorTextBox.Text, out decimal valor))
            {
                if (valor <= 0)
                {
                    MessageBox.Show("El valor debe ser mayor a cero.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                this.Valor = valor;
                this.EsPorcentaje = PorcentajeRadioButton.IsChecked == true;
                this.DialogResult = true;
            }
            else
            {
                MessageBox.Show("Por favor, ingresa un valor numérico válido.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void ValorTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var regex = new Regex(@"^[0-9]*(\.[0-9]*)?$");
            string fullText = ValorTextBox.Text.Insert(ValorTextBox.CaretIndex, e.Text);
            e.Handled = !regex.IsMatch(fullText);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) AceptarButton_Click(null, null);
            else if (e.Key == Key.Escape) CancelarButton_Click(null, null);
        }
    }
}