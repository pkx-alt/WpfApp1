using System;
using System.Collections.Generic;
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
    /// <summary>
    /// Lógica de interacción para AperturaCajaWindow.xaml
    /// </summary>
    public partial class AperturaCajaWindow : Window
    {
        // 1. Creamos una propiedad pública para que la otra ventana
        //    pueda LEER el monto que escribimos aquí.
        public decimal MontoInicial { get; private set; }

        public AperturaCajaWindow()
        {
            InitializeComponent();

            // 2. Ponemos la fecha y hora actuales en el TextBlock
            string formato = "dd/MM/yyyy, h:mm tt";
            FechaHoraTextBlock.Text = "Fecha y hora: " + DateTime.Now.ToString(formato);

            // 3. (Opcional) Pone el cursor listo para escribir en el TextBox.
            MontoInicialTextBox.Focus();
        }

        private void FinalizarButton_Click(object sender, RoutedEventArgs e)
        {
            // 4. Validación de datos
            string montoTexto = MontoInicialTextBox.Text;

            // Usamos decimal.TryParse, es la forma más segura de convertir.
            // "out var" es un atajo para crear la variable 'monto' ahí mismo.
            if (decimal.TryParse(montoTexto, out var monto) && monto >= 0)
            {
                // ¡Éxito! El texto era un número válido y no es negativo.

                // 5. Guardamos el monto en nuestra propiedad pública
                this.MontoInicial = monto;

                // 6. ¡LA CLAVE! Esto le dice a la ventana que nos llamó
                //    que el usuario pulsó "OK" (o "Finalizar").
                this.DialogResult = true;

                // (La ventana se cierra sola después de esto)
            }
            else
            {
                // ¡Error! El texto no es un número o está vacío.
                MessageBox.Show(
                    "Por favor, ingrese un monto inicial válido (solo números).",
                    "Monto no válido",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            // 7. Si cancela, simplemente le decimos a la ventana que
            //    nos llamó que el usuario canceló.
            this.DialogResult = false;

            // (La ventana se cierra sola)
        }
    }
}
