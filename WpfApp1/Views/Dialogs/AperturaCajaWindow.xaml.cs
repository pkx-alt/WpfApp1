using System;
using System.Windows;
using System.Windows.Input; // <--- Verifica este using

namespace OrySiPOS.Views.Dialogs
{
    public partial class AperturaCajaWindow : Window
    {
        public decimal MontoInicial { get; private set; }

        public AperturaCajaWindow()
        {
            InitializeComponent();

            // Formato amigable
            FechaHoraTextBlock.Text = DateTime.Now.ToString("dddd, dd MMMM yyyy - hh:mm tt");

            MontoInicialTextBox.Focus();
        }

        // --- AGREGA ESTO PARA MOVER LA VENTANA ---
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
        // -----------------------------------------

        private void FinalizarButton_Click(object sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(MontoInicialTextBox.Text, out var monto) && monto >= 0)
            {
                this.MontoInicial = monto;
                this.DialogResult = true;
            }
            else
            {
                MessageBox.Show("Ingresa un monto válido.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}