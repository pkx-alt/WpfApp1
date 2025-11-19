// --- Archivo: DisminuirStockModal.xaml.cs ---

using System.Windows;
using WpfApp1.Models; // ¡Importante!
using System.Windows.Input; // Necesario para PreviewTextInput
using System.Windows.Controls; // Necesario para TextChangedEventArgs
using System.Text.RegularExpressions; // Para validar números
using WpfApp1.Data; // <--- ¡AÑADE ESTA LÍNEA!

namespace WpfApp1.Views.Dialogs
{
    public partial class DisminuirStockModal : Window
    {
        private Producto _productoActual;

        // Constructor por defecto (lo dejamos)
        public DisminuirStockModal()
        {
            InitializeComponent();
        }

        // ¡EL CONSTRUCTOR QUE VAMOS A USAR!
        public DisminuirStockModal(Producto productoParaEditar)
        {
            InitializeComponent();
            _productoActual = productoParaEditar;
            this.DataContext = _productoActual;

            // --- ¡DIFERENCIA CLAVE! ---
            // Marcamos "Disminuir" como la opción por defecto
            RadioDisminuir.IsChecked = true;
            // (Asumo que tu XAML tiene un RadioButton con x:Name="RadioDisminuir")

            // Calculamos el total inicial
            ActualizarNuevaExistencia();
        }

        // --- MÉTODOS DE LOS BOTONES PRINCIPALES ---

        private void FinalizarButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(CantidadTextBox.Text, out int cantidad) || cantidad <= 0)
            {
                MessageBox.Show("Introduce una cantidad válida.", "Error");
                return;
            }

            try
            {
                using (var db = new InventarioDbContext())
                {
                    var productoEnDb = db.Productos.Find(_productoActual.ID);

                    if (productoEnDb != null)
                    {
                        int stockAntes = productoEnDb.Stock;
                        string tipoMovimiento = "";

                        // Tu lógica de radios
                        if (RadioAgregar.IsChecked == true)
                        {
                            productoEnDb.Stock += cantidad;
                            tipoMovimiento = "Entrada (Corrección)";
                        }
                        else // Disminuir
                        {
                            if (productoEnDb.Stock < cantidad)
                            {
                                MessageBox.Show("No hay suficiente stock para disminuir esa cantidad.");
                                return;
                            }
                            productoEnDb.Stock -= cantidad;
                            tipoMovimiento = "Salida (Ajuste Manual)";

                            // Leemos el motivo del ComboBox si lo tienes
                            // tipoMovimiento += $" - {MotivoComboBox.Text}";
                        }

                        // Creamos la bitácora
                        var historial = new MovimientoInventario
                        {
                            Fecha = DateTime.Now,
                            ProductoId = productoEnDb.ID,
                            TipoMovimiento = tipoMovimiento,
                            Cantidad = cantidad,
                            StockAnterior = stockAntes,
                            StockNuevo = productoEnDb.Stock,
                            Motivo = NotasTextBox.Text,
                            Usuario = "Admin"
                        };

                        db.Movimientos.Add(historial);
                        db.SaveChanges();

                        // Refrescamos el objeto visual
                        _productoActual.Stock = productoEnDb.Stock;
                    }
                }
                this.DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void CerrarButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }


        // --- LOS MÉTODOS QUE TE FALTABAN (STUBS) ---
        // ¡Estos son los que causaban los errores!

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CantidadTextBox.Focus();
            CantidadTextBox.SelectAll();

            // ¡AQUÍ ESTÁ EL ARREGLO!
            ActualizarNuevaExistencia();
        }

        private void CantidadTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Validamos que SÓLO sean números
            var regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
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

            // (Asumo que tienes un RadioButton x:Name="RadioAgregar")
            if (RadioAgregar.IsChecked == true)
            {
                nuevoTotal = stockActual + cantidad;
            }
            else // Si es RadioDisminuir
            {
                nuevoTotal = stockActual - cantidad;
            }

            // Actualizamos la etiqueta de "Nueva existencia total"
            // (Asumo que tienes un TextBlock x:Name="NuevaExistenciaTextBlock")
            NuevaExistenciaTextBlock.Text = $"{nuevoTotal} unidades";
        }
    }
}