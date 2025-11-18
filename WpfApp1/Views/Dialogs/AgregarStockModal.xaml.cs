// --- Archivo: AgregarStockModal.xaml.cs ---

using System.Text.RegularExpressions; // Para validar números
using System.Windows;
using System.Windows.Controls; // Necesario para TextChangedEventArgs
using System.Windows.Input; // Necesario para PreviewTextInput
using WpfApp1.Models; // ¡Importante!
using WpfApp1.Data; // <--- ¡AÑADE ESTA LÍNEA!

namespace WpfApp1.Views.Dialogs
{
    public partial class AgregarStockModal : Window
    {
        private Producto _productoActual;

        // Constructor que USABAS (lo dejamos)
        public AgregarStockModal()
        {
            InitializeComponent();
        }

        // ¡NUEVO CONSTRUCTOR!
        public AgregarStockModal(Producto productoParaEditar)
        {
            InitializeComponent();
            _productoActual = productoParaEditar;
            this.DataContext = _productoActual;

            // Cuando la ventana cargue, llamamos al cálculo inicial
            ActualizarNuevaExistencia();
        }

        // --- MÉTODOS DE LOS BOTONES PRINCIPALES ---

        private void FinalizarButton_Click(object sender, RoutedEventArgs e)
        {
            // 1. Validar la cantidad (esto ya lo tenías)
            if (!int.TryParse(CantidadTextBox.Text, out int cantidad) || cantidad <= 0)
            {
                MessageBox.Show("Por favor, introduce una cantidad válida.", "Error");
                return;
            }

            // 2. Determinar si es agregar o disminuir (esto ya lo tenías)
            bool esAgregar = RadioAgregar.IsChecked == true;

            // 3. Aplicar el cambio al objeto (esto ya lo tenías)
            if (esAgregar)
            {
                _productoActual.Stock += cantidad;
            }
            else
            {
                if (_productoActual.Stock < cantidad)
                {
                    MessageBox.Show("No puedes disminuir más stock del que tienes.", "Error");
                    return;
                }
                _productoActual.Stock -= cantidad;
            }

            // --- ¡ESTA ES LA PARTE NUEVA/MODIFICADA! ---

            // 4. Guardar en la Base de Datos
            try
            {
                // Creamos una NUEVA conexión a la BD
                using (var db = new InventarioDbContext())
                {
                    // Le decimos a EF Core: "Oye, este objeto (_productoActual)
                    // ha sido modificado. Por favor, actualízalo en la BD."
                    db.Productos.Update(_productoActual);

                    // ¡Ahora sí, guarda los cambios en el archivo .db!
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                // (Es buena práctica atrapar errores)
                MessageBox.Show($"Error al guardar en la base de datos: {ex.Message}", "Error de BD");
                return; // Si falló, no cerramos el modal
            }

            // 5. Si todo salió bien, cerramos
            this.DialogResult = true;
        }

        private void CerrarButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }


        // --- LOS MÉTODOS QUE TE FALTABAN (STUBS) ---
        // ¡Estos son los que causaban los errores!

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Este se llama cuando la ventana se carga.
            // Podemos usarlo para poner el foco en el TextBox.
            CantidadTextBox.Focus();
        }

        private void CantidadTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Esta función es para validar que el usuario SÓLO escriba números
            var regex = new Regex("[^0-9]+"); // Expresión regular que busca "cualquier cosa que NO sea un número"
            e.Handled = regex.IsMatch(e.Text); // Si encuentra algo que no es número, "maneja" el evento (lo bloquea)
        }

        private void CantidadTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Cada vez que el texto cambie, actualizamos el total
            ActualizarNuevaExistencia();
        }

        private void AumentarCantidad_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(CantidadTextBox.Text, out int cantidad))
            {
                CantidadTextBox.Text = (cantidad + 1).ToString();
            }
            else
            {
                CantidadTextBox.Text = "1";
            }
        }

        private void DisminuirCantidad_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(CantidadTextBox.Text, out int cantidad) && cantidad > 1)
            {
                CantidadTextBox.Text = (cantidad - 1).ToString();
            }
            else
            {
                CantidadTextBox.Text = "1";
            }
        }

        private void TipoAjuste_Checked(object sender, RoutedEventArgs e)
        {
            // Cada vez que cambie el RadioButton, actualizamos el total
            ActualizarNuevaExistencia();
        }

        // --- MÉTODO DE AYUDA (Helper) ---

        /// <summary>
        /// Lee el TextBox y los RadioButton para calcular el total
        /// </summary>
        private void ActualizarNuevaExistencia()
        {
            // 'IsLoaded' evita que esto se ejecute antes de que la ventana esté lista
            if (!this.IsLoaded || _productoActual == null) return;

            int cantidad;
            if (!int.TryParse(CantidadTextBox.Text, out cantidad))
            {
                cantidad = 0; // Si el texto no es un número, asumimos 0
            }

            int stockActual = _productoActual.Stock;
            int nuevoTotal;

            if (RadioAgregar.IsChecked == true)
            {
                nuevoTotal = stockActual + cantidad;
            }
            else // Si es RadioDisminuir
            {
                nuevoTotal = stockActual - cantidad;
            }

            // Actualizamos la etiqueta de "Nueva existencia total"
            NuevaExistenciaTextBlock.Text = $"{nuevoTotal} unidades";
        }
    }
}