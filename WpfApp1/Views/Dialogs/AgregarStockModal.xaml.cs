// --- Archivo: AgregarStockModal.xaml.cs ---

using System.Text.RegularExpressions; // Para validar números
using System.Windows;
using System.Windows.Controls; // Necesario para TextChangedEventArgs
using System.Windows.Input; // Necesario para PreviewTextInput
using OrySiPOS.Data; // <--- ¡AÑADE ESTA LÍNEA!
using OrySiPOS.Models; // ¡Importante!
using Microsoft.EntityFrameworkCore;

namespace OrySiPOS.Views.Dialogs
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
            // 1. Validaciones de UI (Esto sigue igual)
            if (!int.TryParse(CantidadTextBox.Text, out int cantidad) || cantidad <= 0)
            {
                MessageBox.Show("Por favor, introduce una cantidad válida.", "Error");
                return;
            }

            try
            {
                int idProductoParaSync = 0; // Aquí guardaremos el ID para el "ayudante"

                // 2. GUARDADO LOCAL (Rápido y Síncrono)
                using (var db = new InventarioDbContext())
                {
                    var productoEnDb = db.Productos.Find(_productoActual.ID);

                    if (productoEnDb != null)
                    {
                        // --- Tu lógica de inventario local ---
                        int stockAntes = productoEnDb.Stock;
                        string tipoMovimiento = "";

                        if (RadioAgregar.IsChecked == true)
                        {
                            productoEnDb.Stock += cantidad;
                            tipoMovimiento = "Entrada (Ajuste Manual)";
                        }
                        else
                        {
                            if (productoEnDb.Stock < cantidad)
                            {
                                MessageBox.Show("No puedes quitar más stock del que tienes.");
                                return;
                            }
                            productoEnDb.Stock -= cantidad;
                            tipoMovimiento = "Salida (Corrección)";
                        }

                        var movimiento = new MovimientoInventario
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

                        db.Movimientos.Add(movimiento);

                        // Guardamos en SQLite (esto tarda milisegundos)
                        db.SaveChanges();

                        // Actualizamos visualmente la ventana padre
                        _productoActual.Stock = productoEnDb.Stock;

                        // ¡IMPORTANTE! Guardamos el ID para el proceso de fondo
                        idProductoParaSync = productoEnDb.ID;
                    }
                }

                // 3. TAREA DE FONDO (Aquí está la magia)
                // Lanzamos un hilo aparte. No usamos 'await' aquí para no bloquear el cierre.
                if (idProductoParaSync > 0)
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            // Este código corre en paralelo, no importa si la ventana se cierra.
                            // Creamos un NUEVO contexto de base de datos exclusivo para este hilo.
                            using (var dbBackground = new InventarioDbContext())
                            {
                                // Volvemos a buscar el producto con sus relaciones limpias
                                var prodSync = await dbBackground.Productos
                                                    .Include(p => p.Subcategoria)
                                                    .ThenInclude(s => s.Categoria)
                                                    .FirstOrDefaultAsync(p => p.ID == idProductoParaSync);

                                if (prodSync != null)
                                {
                                    var srv = new OrySiPOS.Services.SupabaseService();
                                    await srv.SincronizarProducto(prodSync);
                                    System.Diagnostics.Debug.WriteLine($"Sincronización background exitosa: {prodSync.Descripcion}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // Si falla en el fondo, no podemos mostrar MessageBox porque no hay ventana.
                            // Lo escribimos en la consola de salida (Output) de Visual Studio.
                            System.Diagnostics.Debug.WriteLine("Error en sincronización background: " + ex.Message);
                        }
                    });
                }

                // 4. CERRAR INMEDIATAMENTE
                // Como la tarea anterior ya está corriendo en su propio mundo (Task.Run),
                // podemos cerrar la ventana sin miedo.
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar localmente: {ex.Message}", "Error de BD");
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