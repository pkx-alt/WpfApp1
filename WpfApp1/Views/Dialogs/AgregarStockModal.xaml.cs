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

            try
            {
                using (var db = new InventarioDbContext())
                {
                    // IMPORTANTE: Volvemos a buscar el producto en ESTE contexto
                    // para asegurarnos de tener la versión más reciente y evitar errores de tracking.
                    var productoEnDb = db.Productos.Find(_productoActual.ID);

                    if (productoEnDb != null)
                    {
                        // A. Guardamos el estado actual antes de tocar nada
                        int stockAntes = productoEnDb.Stock;

                        // B. Aplicamos el cambio (Tu lógica original de RadioButton)
                        string tipoMovimiento = "";

                        // Asumo que tienes RadioAgregar y RadioDisminuir en tu XAML
                        if (RadioAgregar.IsChecked == true)
                        {
                            productoEnDb.Stock += cantidad;
                            tipoMovimiento = "Entrada (Ajuste Manual)";
                        }
                        else
                        {
                            // Validación extra
                            if (productoEnDb.Stock < cantidad)
                            {
                                MessageBox.Show("No puedes quitar más stock del que tienes.");
                                return;
                            }
                            productoEnDb.Stock -= cantidad;
                            tipoMovimiento = "Salida (Corrección)";
                        }

                        // C. ¡LA NOVEDAD! Creamos el registro en la bitácora
                        var movimiento = new MovimientoInventario
                        {
                            Fecha = DateTime.Now,
                            ProductoId = productoEnDb.ID,
                            TipoMovimiento = tipoMovimiento,
                            Cantidad = cantidad,
                            StockAnterior = stockAntes,
                            StockNuevo = productoEnDb.Stock, // El stock ya modificado
                            Motivo = NotasTextBox.Text, // Asumiendo que tienes un TextBox para notas
                            Usuario = "Admin" // Aquí pondrías el usuario logueado
                        };

                        // D. Agregamos el movimiento a la tabla nueva
                        db.Movimientos.Add(movimiento);

                        // E. Guardamos TODO junto (Producto actualizado + Movimiento nuevo)
                        db.SaveChanges();

                        // Actualizamos el objeto visual de la ventana anterior (opcional pero visualmente útil)
                        _productoActual.Stock = productoEnDb.Stock;
                    }
                }

                this.DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar: {ex.Message}", "Error de BD");
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
            // 1. Ponemos el foco para escribir rápido
            CantidadTextBox.Focus();
            // 2. Seleccionamos todo el texto ("1") para que sea fácil borrarlo
            CantidadTextBox.SelectAll();

            // 3. ¡AQUÍ ESTÁ EL ARREGLO!
            // Forzamos el cálculo ahora que la ventana ya cargó (IsLoaded = true)
            ActualizarNuevaExistencia();
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

        // --- AGREGA ESTE MÉTODO PARA MOVER LA VENTANA ---
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
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