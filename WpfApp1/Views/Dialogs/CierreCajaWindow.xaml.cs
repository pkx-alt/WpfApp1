using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WpfApp1.Views.Dialogs
{
    public partial class CierreCajaWindow : Window
    {
        // Propiedades públicas para devolver los datos
        public decimal TotalContado { get; private set; }
        public string Notas { get; private set; }

        // Variables privadas para los cálculos
        private decimal _efectivoEsperado;
        private CultureInfo _culturaMoneda = new CultureInfo("es-MX"); // Para formato de moneda

        // ¡AQUÍ ESTÁ LA BANDERA!
        private bool _isWindowLoaded = false;

        public CierreCajaWindow(decimal efectivoEsperado)
        {
            InitializeComponent();

            // ¡SUBIMOS LA BANDERA!
            // Ahora que SÍ terminó InitializeComponent, es seguro trabajar.
            _isWindowLoaded = true;

            _efectivoEsperado = efectivoEsperado;

            // Asignamos los valores iniciales
            EfectivoEsperadoTextBlock.Text = _efectivoEsperado.ToString("C", _culturaMoneda);
            ActualizarTotales();
        }

        #region "Lógica de Conteo"

        /// <summary>
        /// Este método se dispara cada vez que un TextBox cambia.
        /// </summary>
        private void Denominacion_TextChanged(object sender, TextChangedEventArgs e)
        {
            ActualizarTotales();
        }

        /// <summary>
        /// El cerebro: Lee todos los TextBoxes, calcula el total y la diferencia.
        /// </summary>
        private void ActualizarTotales()
        {
            // ¡EL GUARDIÁN!
            // Si la ventana no está lista, no hagas absolutamente nada.
            if (!_isWindowLoaded)
            {
                return;
            }
            decimal total = 0;
            total += LeerValor(Txt20) * 20;
            total += LeerValor(Txt50) * 50;
            total += LeerValor(Txt100) * 100;
            total += LeerValor(Txt200) * 200;
            total += LeerValor(Txt500) * 500;
            total += LeerValor(Txt1000) * 1000;
            total += LeerValor(TxtMonedas); // Asumimos que "Monedas" es el monto total, no la cantidad.

            // Calculamos la diferencia
            decimal diferencia = total - _efectivoEsperado;

            // Actualizamos la UI
            TotalContadoTextBlock.Text = total.ToString("C", _culturaMoneda);
            DiferenciaTextBlock.Text = diferencia.ToString("C", _culturaMoneda);

            // Guardamos el total para devolverlo
            this.TotalContado = total;
        }

        /// <summary>
        /// Método ayudante para leer de forma segura un TextBox.
        /// </summary>
        private int LeerValor(TextBox txt)
        {
            // int.TryParse es la forma profesional de convertir texto a número
            if (int.TryParse(txt.Text, out int valor) && valor >= 0)
            {
                return valor;
            }
            return 0; // Si no es un número válido, devuelve 0
        }

        #endregion

        #region "Botones +/-"

        // Usamos el 'Tag' del botón para saber a qué TextBox afectar

        private void PlusButton_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            TextBox txt = (TextBox)this.FindName(btn.Tag.ToString());

            int valor = LeerValor(txt);
            txt.Text = (valor + 1).ToString();
            // El evento TextChanged se disparará y llamará a ActualizarTotales()
        }

        private void MinusButton_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            TextBox txt = (TextBox)this.FindName(btn.Tag.ToString());

            int valor = LeerValor(txt);
            if (valor > 0) // No permitimos negativos
            {
                txt.Text = (valor - 1).ToString();
            }
        }

        #endregion

        #region "Botones de Acción"

        private void FinalizarButton_Click(object sender, RoutedEventArgs e)
        {
            // Guardamos las notas
            this.Notas = NotasTextBox.Text;

            // Devolvemos "OK"
            this.DialogResult = true;
        }

        private void ImprimirButton_Click(object sender, RoutedEventArgs e)
        {
            // La lógica de impresión es un tema completo.
            // Por ahora, solo mostramos un aviso.
            MessageBox.Show("Lógica de impresión pendiente de implementar.", "Imprimir");
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            // Devolvemos "Cancelar"
            this.DialogResult = false;
        }

        #endregion
    }
}
