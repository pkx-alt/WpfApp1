using System;
using System.Globalization;
using System.Text.RegularExpressions; // Necesario para Regex
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media; // Necesario para los colores

namespace OrySiPOS.Views.Dialogs
{
    public partial class CierreCajaWindow : Window
    {
        public decimal TotalContado { get; private set; }
        public string Notas { get; private set; }

        private decimal _efectivoEsperado;
        private CultureInfo _culturaMoneda = new CultureInfo("es-MX");
        private bool _isWindowLoaded = false;

        // Bandera para evitar conflictos entre lo manual y lo automático
        private bool _ignorarCambiosManuales = false;

        public CierreCajaWindow(decimal efectivoEsperado)
        {
            InitializeComponent();
            _isWindowLoaded = true;
            _efectivoEsperado = efectivoEsperado;

            EfectivoEsperadoTextBlock.Text = _efectivoEsperado.ToString("C", _culturaMoneda);

            // Iniciamos en cero
            ActualizarTotalesDesdeBilletes();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        #region Lógica de Billetes (Automática)

        private void Denominacion_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isWindowLoaded) return;
            ActualizarTotalesDesdeBilletes();
        }

        private void ActualizarTotalesDesdeBilletes()
        {
            decimal total = 0;
            total += LeerValor(Txt20) * 20;
            total += LeerValor(Txt50) * 50;
            total += LeerValor(Txt100) * 100;
            total += LeerValor(Txt200) * 200;
            total += LeerValor(Txt500) * 500;
            total += LeerValor(Txt1000) * 1000;
            total += LeerValor(TxtMonedas);

            // ACTIVAMOS LA BANDERA: "Oye, estoy actualizando el texto por código, no es el usuario escribiendo"
            _ignorarCambiosManuales = true;

            TotalContadoTextBlock.Text = total.ToString("N2", _culturaMoneda); // Usamos N2 para que sea número limpio, o C si prefieres con signo

            // DESACTIVAMOS LA BANDERA
            _ignorarCambiosManuales = false;

            // Calculamos la diferencia
            RecalcularDiferencia(total);
        }

        private int LeerValor(TextBox txt)
        {
            if (int.TryParse(txt.Text, out int valor) && valor >= 0) return valor;
            return 0;
        }

        private void PlusButton_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            TextBox txt = (TextBox)this.FindName(btn.Tag.ToString());
            txt.Text = (LeerValor(txt) + 1).ToString();
        }

        private void MinusButton_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            TextBox txt = (TextBox)this.FindName(btn.Tag.ToString());
            int valor = LeerValor(txt);
            if (valor > 0) txt.Text = (valor - 1).ToString();
        }

        #endregion

        #region Lógica Manual (Cuando escribes el total directo)

        private void TotalContado_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Si la bandera está activa, significa que el cambio vino de los billetes, así que no hacemos nada aquí
            if (_ignorarCambiosManuales || !_isWindowLoaded) return;

            // Si llegamos aquí, es porque el usuario está escribiendo manualmente
            // Limpiamos el texto de signos de pesos o letras para obtener el número
            string textoLimpio = TotalContadoTextBlock.Text.Replace("$", "").Replace(",", "").Trim();

            if (decimal.TryParse(textoLimpio, out decimal totalManual))
            {
                RecalcularDiferencia(totalManual);
            }
            else
            {
                // Si borra todo o escribe letras, asumimos 0
                RecalcularDiferencia(0);
            }
        }

        // Validación para que solo deje escribir números y puntos en el Total
        private void TotalContado_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9.]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        #endregion

        #region Lógica Común

        private void RecalcularDiferencia(decimal totalFinal)
        {
            this.TotalContado = totalFinal;
            decimal diferencia = totalFinal - _efectivoEsperado;

            DiferenciaTextBlock.Text = diferencia.ToString("C", _culturaMoneda);

            if (diferencia < 0)
                DiferenciaTextBlock.Foreground = (Brush)Application.Current.Resources["DangerColor"];
            else if (diferencia > 0)
                DiferenciaTextBlock.Foreground = (Brush)Application.Current.Resources["SuccessColor"];
            else
                DiferenciaTextBlock.Foreground = (Brush)Application.Current.Resources["TextSecondary"];
        }

        private void FinalizarButton_Click(object sender, RoutedEventArgs e)
        {
            this.Notas = NotasTextBox.Text;
            this.DialogResult = true;
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void ImprimirButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Imprimiendo pre-corte...");
        }

        #endregion
    }
}