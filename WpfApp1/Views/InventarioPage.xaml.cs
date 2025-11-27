using Microsoft.EntityFrameworkCore;
using Microsoft.Win32; // Para el diálogo de guardar/abrir
using System;
using System.IO;       // Para leer/escribir archivos
using System.Linq;      // Para consultas rápidas
using System.Text;     // Para el StringBuilder
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq; // <--- ¡VITAL! Para entender el lenguaje del SAT
using OrySiPOS.Data;
using OrySiPOS.Models;
using OrySiPOS.ViewModels;
using OrySiPOS.Views.Dialogs;



namespace OrySiPOS.Views
{
    public partial class InventarioPage : Page
    {
        // Texto para el buscador
        private const string PlaceholderText = "Buscar por nombre o descripción...";

        public InventarioPage()
        {
            InitializeComponent();

            // Conectamos el ViewModel
            this.DataContext = new InventarioViewModel();

            // Configuramos el texto gris del buscador
            SetupPlaceholder();
        }

        // Atajo para acceder al ViewModel desde el código
        private InventarioViewModel VM => (InventarioViewModel)DataContext;

        // --- Lógica del Buscador (Placeholder) ---
        private void SetupPlaceholder()
        {
            SearchTextBox.Text = PlaceholderText;
            SearchTextBox.Foreground = Brushes.Gray;
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchTextBox.Text == PlaceholderText)
            {
                SearchTextBox.Text = "";
                SearchTextBox.Foreground = Brushes.Black;
            }
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                SearchTextBox.Text = PlaceholderText;
                SearchTextBox.Foreground = Brushes.Gray;
            }
        }

        // En Views/InventarioPage.xaml.cs

        // En Views/InventarioPage.xaml.cs

        private void BtnExportar_Click(object sender, RoutedEventArgs e)
        {
            // 1. Obtenemos los datos visibles en la tabla
            var vm = this.DataContext as InventarioViewModel;
            if (vm == null || vm.Productos == null || vm.Productos.Count == 0)
            {
                MessageBox.Show("No hay productos para exportar.", "Aviso");
                return;
            }

            // 2. Diálogo para guardar
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "Archivo CSV (*.csv)|*.csv",
                FileName = $"Inventario_{DateTime.Now:yyyyMMdd}.csv",
                Title = "Exportar Inventario Completo"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var sb = new StringBuilder();

                    // 3. ENCABEZADOS (Incluyendo campos CFDI, Ganancia, Categoría/Subcategoría)
                    sb.AppendLine("ID,Descripcion,Precio,Costo,Ganancia,Stock,Activo,ClaveSat,ClaveUnidad,Subcategoria,Categoria");

                    // 4. FILAS
                    foreach (var p in vm.Productos)
                    {
                        // Limpiamos la descripción de comas para no romper el CSV y usamos comillas para proteger el texto
                        string desc = p.Descripcion.Replace("\"", "\"\""); // Duplicamos comillas para escapar
                        string subcatNombre = p.Subcategoria?.Nombre.Replace("\"", "\"\"") ?? "Sin Subcategoría";
                        string catNombre = p.Subcategoria?.Categoria?.Nombre.Replace("\"", "\"\"") ?? "Sin Categoría";
                        string estado = p.Activo ? "Si" : "No";

                        // Usamos comillas dobles al inicio y fin de los campos de texto
                        sb.AppendLine($"{p.ID},\"{desc}\",{p.Precio},{p.Costo},{p.Ganancia},{p.Stock},{estado},{p.ClaveSat},{p.ClaveUnidad},\"{subcatNombre}\",\"{catNombre}\"");
                    }

                    // 5. GUARDAR
                    File.WriteAllText(saveDialog.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show("Inventario completo exportado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al exportar: {ex.Message}", "Error");
                }
            }
        }
        // En Views/InventarioPage.xaml.cs

        private void BtnImportar_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog
            {
                Filter = "Archivo CSV (*.csv)|*.csv",
                Title = "Importar productos masivamente"
            };

            if (openDialog.ShowDialog() == true)
            {
                try
                {
                    // Lista para guardar los IDs que tendremos que subir a la nube
                    var productosParaSync = new System.Collections.Generic.List<Producto>();

                    using (var db = new InventarioDbContext())
                    {
                        var subcatPorDefecto = db.Subcategorias.FirstOrDefault(s => s.Nombre == "General")
                                               ?? db.Subcategorias.FirstOrDefault();

                        if (subcatPorDefecto == null)
                        {
                            MessageBox.Show("Error: No existen subcategorías...", "Falta Configuración");
                            return;
                        }

                        string[] lineas = File.ReadAllLines(openDialog.FileName);
                        int contados = 0;
                        int errores = 0;

                        for (int i = 1; i < lineas.Length; i++)
                        {
                            string linea = lineas[i];
                            if (string.IsNullOrWhiteSpace(linea)) continue;

                            string[] partes = linea.Split(',');

                            if (partes.Length < 4) { errores++; continue; }

                            try
                            {
                                var nuevoProd = new Producto
                                {
                                    Descripcion = partes[0].Trim(),
                                    Precio = decimal.Parse(partes[1]),
                                    Costo = decimal.Parse(partes[2]),
                                    Stock = int.Parse(partes[3]),
                                    Activo = true,
                                    ImagenUrl = "https://via.placeholder.com/150",
                                    SubcategoriaId = subcatPorDefecto.Id
                                };

                                db.Productos.Add(nuevoProd);

                                // Lo agregamos a nuestra lista temporal para no perderle la pista
                                productosParaSync.Add(nuevoProd);

                                contados++;
                            }
                            catch { errores++; }
                        }

                        db.SaveChanges(); // ¡Aquí SQLite les asigna los IDs a todos!

                        // --- SINCRONIZACIÓN MASIVA EN SEGUNDO PLANO ---
                        if (productosParaSync.Count > 0)
                        {
                            // Sacamos solo los IDs para pasárselos al hilo secundario
                            var idsParaNube = productosParaSync.Select(p => p.ID).ToList();

                            Task.Run(async () =>
                            {
                                try
                                {
                                    using (var dbSync = new InventarioDbContext())
                                    {
                                        // Cargamos los productos completos (con categoría) usando los IDs
                                        var listaParaSubir = await dbSync.Productos
                                            .Include(p => p.Subcategoria).ThenInclude(s => s.Categoria)
                                            .Where(p => idsParaNube.Contains(p.ID))
                                            .ToListAsync();

                                        var srv = new OrySiPOS.Services.SupabaseService();
                                        foreach (var prod in listaParaSubir)
                                        {
                                            await srv.SincronizarProducto(prod);
                                        }
                                        System.Diagnostics.Debug.WriteLine($"Importación CSV sincronizada: {listaParaSubir.Count} productos.");
                                    }
                                }
                                catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Error sync CSV: " + ex.Message); }
                            });
                        }
                        // -----------------------------------------------

                        string mensaje = $"Se importaron {contados} productos exitosamente.";
                        if (errores > 0) mensaje += $"\nHubo {errores} líneas con errores.";

                        MessageBox.Show(mensaje, "Importación Finalizada", MessageBoxButton.OK, MessageBoxImage.Information);

                        var vm = this.DataContext as InventarioViewModel;
                        vm?.CargarProductos();
                    }
                }
                catch (Exception ex) { MessageBox.Show($"Error crítico: {ex.Message}"); }
            }
        }

        // En Views/InventarioPage.xaml.cs

        private void BtnImportarXML_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog
            {
                Filter = "Factura XML (*.xml)|*.xml",
                Title = "Selecciona la factura de compra (CFDI)"
            };

            if (openDialog.ShowDialog() == true)
            {
                try
                {
                    // Lista maestra de IDs que cambiaron (nuevos o editados)
                    var idsAfectados = new System.Collections.Generic.List<int>();

                    using (var db = new InventarioDbContext())
                    {
                        var subcatPorDefecto = db.Subcategorias.FirstOrDefault(s => s.Nombre == "General")
                                               ?? db.Subcategorias.FirstOrDefault();

                        if (subcatPorDefecto == null) { MessageBox.Show("Necesitas subcategorías...", "Error"); return; }

                        XDocument doc = XDocument.Load(openDialog.FileName);
                        XNamespace cfdi = doc.Root.Name.Namespace;
                        var conceptos = doc.Descendants(cfdi + "Concepto").ToList();

                        int nuevos = 0;
                        int actualizados = 0;

                        foreach (var concepto in conceptos)
                        {
                            string descripcion = concepto.Attribute("Descripcion")?.Value;
                            decimal cantDec = decimal.Parse(concepto.Attribute("Cantidad")?.Value ?? "0");
                            int cantidad = (int)Math.Round(cantDec);
                            decimal costo = decimal.Parse(concepto.Attribute("ValorUnitario")?.Value ?? "0");
                            decimal precioSugerido = costo * 1.30m;

                            var productoExistente = db.Productos
                                .FirstOrDefault(p => p.Descripcion.ToLower() == descripcion.ToLower());

                            if (productoExistente != null)
                            {
                                // --- YA EXISTE ---
                                productoExistente.Stock += cantidad;
                                productoExistente.Costo = costo;

                                // Guardamos su ID en la lista para sincronizar
                                if (!idsAfectados.Contains(productoExistente.ID))
                                    idsAfectados.Add(productoExistente.ID);

                                actualizados++;
                            }
                            else
                            {
                                // --- NUEVO ---
                                var nuevoProd = new Producto
                                {
                                    Descripcion = descripcion,
                                    Costo = costo,
                                    Precio = precioSugerido,
                                    Stock = cantidad,
                                    Activo = true,
                                    SubcategoriaId = subcatPorDefecto.Id,
                                    ImagenUrl = "https://via.placeholder.com/150"
                                };

                                db.Productos.Add(nuevoProd);
                                // Nota: Aquí 'nuevoProd.ID' aún es 0, pero EF Core es listo.
                                // Lo rastrearemos después de SaveChanges si mantenemos la referencia,
                                // o más fácil: haremos el sync DESPUÉS de guardar.
                                nuevos++;
                            }
                        }

                        db.SaveChanges(); // Guardamos todo

                        // --- CAPTURAR IDs DE LOS NUEVOS ---
                        // Como acabamos de guardar, buscamos los productos NUEVOS que no teníamos ID antes.
                        // Un truco simple es volver a buscar por descripción los que acabamos de insertar,
                        // pero para no complicar el código, haremos esto:
                        // En este punto 'db.ChangeTracker' ya limpió el estado, así que lo mejor
                        // es usar la lógica que pusimos en 'ProcesarArchivoXML' abajo, que es más robusta.
                        // Pero para este botón simple, vamos a re-escanear los productos recién tocados.

                        // (Para simplificar tu aprendizaje: El método 'ProcesarArchivoXML' de abajo es el "pro"
                        // que usaremos para el correo. Este botón manual lo dejaremos simple y delegaremos
                        // la lógica "pro" al método que ya tienes 'ProcesarArchivoXML' si quisieras unificarlo,
                        // pero aquí te dejo la versión corregida con sync).

                        // TRUCO RÁPIDO: Sincronizar TODO lo que coincida con los nombres del XML
                        var nombresEnXml = conceptos.Select(c => c.Attribute("Descripcion")?.Value.ToLower()).ToList();

                        Task.Run(async () =>
                        {
                            try
                            {
                                using (var dbSync = new InventarioDbContext())
                                {
                                    // Buscamos todos los productos que coincidan con las descripciones del XML
                                    var productosASincronizar = await dbSync.Productos
                                        .Include(p => p.Subcategoria).ThenInclude(s => s.Categoria)
                                        .Where(p => nombresEnXml.Contains(p.Descripcion.ToLower()))
                                        .ToListAsync();

                                    var srv = new OrySiPOS.Services.SupabaseService();
                                    foreach (var prod in productosASincronizar)
                                    {
                                        await srv.SincronizarProducto(prod);
                                    }
                                }
                            }
                            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Error sync XML: " + ex.Message); }
                        });

                        var vm = this.DataContext as InventarioViewModel;
                        vm?.CargarProductos();

                        MessageBox.Show($"Proceso terminado.\nNuevos: {nuevos}\nActualizados: {actualizados}");
                    }
                }
                catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
            }
        }

        // En Views/InventarioPage.xaml.cs

        // Método público que recibe la ruta de un archivo XML y lo procesa
        // En Views/InventarioPage.xaml.cs

        // En Views/InventarioPage.xaml.cs

        public int ProcesarArchivoXML(string rutaArchivo)
        {
            int nuevos = 0;
            int actualizados = 0;
            var nombresAfectados = new System.Collections.Generic.List<string>();

            try
            {
                using (var db = new OrySiPOS.Data.InventarioDbContext())
                {
                    // 1. CARGAR EL XML Y BUSCAR EL UUID
                    var doc = XDocument.Load(rutaArchivo);

                    XNamespace tfd = "http://www.sat.gob.mx/TimbreFiscalDigital";
                    var timbre = doc.Descendants(tfd + "TimbreFiscalDigital").FirstOrDefault();
                    string uuid = timbre?.Attribute("UUID")?.Value?.ToUpper();

                    // 2. VALIDACIÓN DE DUPLICADOS
                    if (string.IsNullOrEmpty(uuid)) return 0;

                    bool yaExiste = db.HistorialImportaciones.Any(h => h.UUID == uuid);
                    if (yaExiste)
                    {
                        System.Diagnostics.Debug.WriteLine($"FACTURA DUPLICADA: {uuid}");
                        return 0;
                    }

                    // --- PROCESAMIENTO DE CONCEPTOS ---

                    var subcatPorDefecto = db.Subcategorias.FirstOrDefault(s => s.Nombre == "General")
                                           ?? db.Subcategorias.FirstOrDefault();

                    if (subcatPorDefecto == null) return 0;

                    XNamespace cfdi = doc.Root.Name.Namespace;
                    var conceptos = doc.Descendants(cfdi + "Concepto").ToList();

                    foreach (var concepto in conceptos)
                    {
                        string descripcion = concepto.Attribute("Descripcion")?.Value?.Trim();
                        if (string.IsNullOrWhiteSpace(descripcion)) continue;

                        nombresAfectados.Add(descripcion.ToLower());

                        // --- ¡AQUÍ ESTÁ LA CORRECCIÓN! LEEMOS LOS DATOS SAT ---
                        string claveSatXml = concepto.Attribute("ClaveProdServ")?.Value;
                        string unidadXml = concepto.Attribute("ClaveUnidad")?.Value;

                        // Valores monetarios
                        decimal cantDec = decimal.Parse(concepto.Attribute("Cantidad")?.Value ?? "0");
                        int cantidad = (int)Math.Round(cantDec);
                        decimal costo = decimal.Parse(concepto.Attribute("ValorUnitario")?.Value ?? "0");
                        decimal precioSugerido = costo * 1.30m;

                        // Buscamos producto existente
                        var productoExistente = db.Productos.Local
                            .FirstOrDefault(p => p.Descripcion.ToLower() == descripcion.ToLower())
                            ?? db.Productos.FirstOrDefault(p => p.Descripcion.ToLower() == descripcion.ToLower());

                        if (productoExistente != null)
                        {
                            // --- ACTUALIZAR EXISTENTE ---
                            productoExistente.Stock += cantidad;

                            // Actualizamos costo solo si el nuevo es mayor (opcional, o siempre)
                            if (costo > 0) productoExistente.Costo = costo;

                            // ¡CORRECCIÓN! Si el XML trae clave SAT, actualizamos la nuestra
                            if (!string.IsNullOrEmpty(claveSatXml) && claveSatXml.Length == 8)
                                productoExistente.ClaveSat = claveSatXml;

                            if (!string.IsNullOrEmpty(unidadXml))
                                productoExistente.ClaveUnidad = unidadXml;

                            actualizados++;
                        }
                        else
                        {
                            // --- CREAR NUEVO ---
                            var nuevoProd = new OrySiPOS.Models.Producto
                            {
                                Descripcion = descripcion,
                                Costo = costo,
                                Precio = precioSugerido,
                                Stock = cantidad,
                                Activo = true,
                                SubcategoriaId = subcatPorDefecto.Id,
                                ImagenUrl = "https://via.placeholder.com/150",

                                // ¡CORRECCIÓN! Asignamos lo que viene del XML. 
                                // Si viene vacío (raro en CFDI), usamos el genérico.
                                ClaveSat = !string.IsNullOrEmpty(claveSatXml) ? claveSatXml : "01010101",
                                ClaveUnidad = !string.IsNullOrEmpty(unidadXml) ? unidadXml : "H87"
                            };

                            // Importante: Calcular ganancia (precio) base
                            // (Ya lo asignamos arriba en precioSugerido)

                            db.Productos.Add(nuevoProd);
                            nuevos++;

                            // OJO: También deberíamos guardar la clave en el catálogo de aprendizaje 
                            // para que aparezca en el buscador futuro (Lógica del Paso 2 de NuevoProductoModal)
                            // Pero eso lo podemos dejar para la sincronización o agregarlo aquí si quieres.
                        }
                    }

                    // 3. REGISTRAR HISTORIAL
                    db.HistorialImportaciones.Add(new OrySiPOS.Models.HistorialImportacion
                    {
                        UUID = uuid,
                        FechaProcesado = DateTime.Now,
                        Archivo = System.IO.Path.GetFileName(rutaArchivo)
                    });

                    db.SaveChanges();

                    // --- SINCRONIZACIÓN BACKGROUND ---
                    if (nombresAfectados.Count > 0)
                    {
                        System.Threading.Tasks.Task.Run(async () =>
                        {
                            try
                            {
                                using (var dbSync = new OrySiPOS.Data.InventarioDbContext())
                                {
                                    var productosParaSubir = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
                                        dbSync.Productos
                                        .Include(p => p.Subcategoria).ThenInclude(s => s.Categoria)
                                        .Where(p => nombresAfectados.Contains(p.Descripcion.ToLower()))
                                    );

                                    var srv = new OrySiPOS.Services.SupabaseService();
                                    foreach (var prod in productosParaSubir)
                                    {
                                        await srv.SincronizarProducto(prod);
                                    }
                                }
                            }
                            catch { /* Log silencioso */ }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error XML: " + ex.Message);
            }

            return nuevos + actualizados;
        }

        // 1. Maneja el clic en "Ver detalles" del menú contextual
        private void VerDetalles_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var producto = menuItem?.DataContext as Producto;
            if (producto != null)
            {
                MostrarDetalleProducto(producto.ID);
            }
        }

        // 2. Maneja el doble clic en la fila de la tabla
        private void GridProductos_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            // Aseguramos que el doble clic haya sido en una fila de datos
            if (dataGrid?.SelectedItem is Producto productoSeleccionado)
            {
                MostrarDetalleProducto(productoSeleccionado.ID);
            }
        }

        // 3. Lógica central para buscar y mostrar el diálogo
        private void MostrarDetalleProducto(int productoId)
        {
            Producto productoCompleto;

            using (var db = new OrySiPOS.Data.InventarioDbContext())
            {
                // Traemos el producto completo, incluyendo Subcategoría y Categoría
                productoCompleto = db.Productos
                    .Include(p => p.Subcategoria)
                        .ThenInclude(s => s.Categoria)
                    .FirstOrDefault(p => p.ID == productoId);
            }

            if (productoCompleto == null)
            {
                MessageBox.Show("No se pudo cargar el detalle del producto.", "Error");
                return;
            }

            // --- CAMBIO AQUÍ: Usamos ReporteItem ---
            var detalles = new List<ReporteItem>
    {
        new ReporteItem { Propiedad = "ID (SKU)", Valor = productoCompleto.ID.ToString() },
        new ReporteItem { Propiedad = "Descripción", Valor = productoCompleto.Descripcion },
        new ReporteItem { Propiedad = "Categoría", Valor = productoCompleto.Subcategoria?.Categoria?.Nombre ?? "N/A" },
        new ReporteItem { Propiedad = "Subcategoría", Valor = productoCompleto.Subcategoria?.Nombre ?? "N/A" },
        new ReporteItem { Propiedad = "Precio Venta", Valor = productoCompleto.Precio.ToString("C") },
        new ReporteItem { Propiedad = "Tasa IVA", Valor = (productoCompleto.PorcentajeIVA * 100).ToString("0") + "%" },
        new ReporteItem { Propiedad = "Costo", Valor = productoCompleto.Costo.ToString("C") },
        new ReporteItem { Propiedad = "Ganancia (Margen)", Valor = productoCompleto.Ganancia.ToString("C") },
        new ReporteItem { Propiedad = "Stock Actual", Valor = productoCompleto.Stock.ToString() + " uds." },
        new ReporteItem { Propiedad = "Activo para Venta", Valor = productoCompleto.Activo ? "Sí" : "No" },
        new ReporteItem { Propiedad = "Clave SAT", Valor = productoCompleto.ClaveSat },
        new ReporteItem { Propiedad = "Clave Unidad", Valor = productoCompleto.ClaveUnidad },
        new ReporteItem { Propiedad = "URL Imagen", Valor = productoCompleto.ImagenUrl },
    };

            // 5. Mostrar el diálogo
            var dialog = new VisorReporteWindow($"Detalle: {productoCompleto.Descripcion}", detalles);
            dialog.Owner = Window.GetWindow(this);

            // --- TRUCO VISUAL PARA EL VISOR DE REPORTE ---
            dialog.GridDatos.AutoGenerateColumns = false;
            dialog.GridDatos.Columns.Clear();

            // 1. Definir el estilo de la columna Propiedad (Negrita)
            var boldTextStyle = new Style(typeof(TextBlock));
            // Necesitas el using System.Windows.Controls y System.Windows.Media
            boldTextStyle.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.Bold));

            // Columna Propiedad (Negrita, ancho fijo)
            dialog.GridDatos.Columns.Add(new DataGridTextColumn
            {
                Header = "Propiedad",
                Binding = new System.Windows.Data.Binding("Propiedad"),
                // ASIGNAMOS EL ESTILO AQUÍ
                ElementStyle = boldTextStyle,
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });

            // Columna Valor (Ancho flexible)
            dialog.GridDatos.Columns.Add(new DataGridTextColumn
            {
                Header = "Valor",
                Binding = new System.Windows.Data.Binding("Valor"),
                Width = new DataGridLength(2, DataGridLengthUnitType.Star)
            });

            // Ocultamos encabezados de columna para que parezca una lista vertical
            dialog.GridDatos.HeadersVisibility = DataGridHeadersVisibility.Column;


            dialog.ShowDialog();
        }
        // --- Acciones de Stock (+ / -) ---
        private void AddStock_Click(object sender, RoutedEventArgs e)
        {
            var boton = sender as FrameworkElement;
            if (boton == null) return;
            var producto = boton.DataContext as Producto;
            if (producto == null) return;

            var modal = new AgregarStockModal(producto);
            if (modal.ShowDialog() == true)
            {
                VM.CargarProductos(); // Recargar lista
            }
        }

        private void RemoveStock_Click(object sender, RoutedEventArgs e)
        {
            var boton = sender as FrameworkElement;
            if (boton == null) return;
            var producto = boton.DataContext as Producto;
            if (producto == null) return;

            var modal = new DisminuirStockModal(producto);
            modal.Owner = Window.GetWindow(this);
            if (modal.ShowDialog() == true)
            {
                VM.CargarProductos(); // Recargar lista
            }
        }

        // --- Menú de Opciones y Clic Derecho ---

        // Este evento abre el menú del botón "..."
        private void OpcionesButton_Click(object sender, RoutedEventArgs e)
        {
            var boton = sender as Button;
            if (boton?.ContextMenu == null) return;

            // Asignar el producto al menú para que sepa qué editar
            boton.ContextMenu.DataContext = boton.DataContext;
            boton.ContextMenu.PlacementTarget = boton;
            boton.ContextMenu.IsOpen = true;
        }

        // --- Funciones del Menú (Compartidas por Botón y Clic Derecho) ---

        private void EditarProducto_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var producto = menuItem?.DataContext as Producto;
            if (producto == null) return;

            MessageBox.Show($"Editar: {producto.Descripcion} (Próximamente)");
            // Aquí abrirías tu ventana de edición real
        }


        private void Historial_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var producto = menuItem?.DataContext as Producto;
            if (producto == null) return;

            // Navegar a la página de movimientos con el filtro
            this.NavigationService.Navigate(new MovimientosPage(producto.ID));
        }

        private void Deshabilitar_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var producto = menuItem?.DataContext as Producto;
            if (producto == null) return;

            bool esParaDeshabilitar = producto.Activo;
            var modal = new ConfirmarEstadoModal(producto, esParaDeshabilitar);

            if (modal.ShowDialog() == true)
            {
                // YA NO HACEMOS NADA AQUÍ con la DB, porque el modal ya lo hizo.
                // Solo refrescamos la lista visual.
                VM.CargarProductos();
            }
        }

        // En Views/InventarioPage.xaml.cs

        private void BtnEscanearCorreo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var servicioEmail = new Services.EmailImportService();

                Mouse.OverrideCursor = Cursors.Wait;

                // 1. Descargar
                var correosProcesados = servicioEmail.DescargarCorreosConDetalles();

                Mouse.OverrideCursor = null;

                if (correosProcesados.Count == 0)
                {
                    MessageBox.Show("No se encontraron correos nuevos con facturas.", "Sin novedades");
                    return;
                }

                // 2. Procesar y Construir Lista para la Nueva Ventana
                var listaVisual = new List<dynamic>();
                int totalProductosGeneral = 0;
                int totalArchivos = 0;

                foreach (var correo in correosProcesados)
                {
                    // Limpiamos el nombre del remitente
                    string nombreLimpio = correo.Remitente.Split('<')[0].Trim().Replace("\"", "");

                    foreach (var archivo in correo.ArchivosAdjuntos)
                    {
                        totalArchivos++;

                        // Procesar lógica
                        int prods = ProcesarArchivoXML(archivo);
                        totalProductosGeneral += prods;

                        // --- AQUÍ DEFINIMOS LOS COLORES ---
                        string colorFondo = prods > 0 ? "#D4EDDA" : "#F8D7DA"; // Verde claro vs Rojo claro
                        string colorTexto = prods > 0 ? "#155724" : "#721C24"; // Verde oscuro vs Rojo oscuro
                        string textoEstado = prods > 0 ? $"✅ {prods} CARGADOS" : "⚠️ SIN CAMBIOS";

                        listaVisual.Add(new
                        {
                            Fecha = correo.Fecha.ToString("dd/MM HH:mm"),
                            Remitente = nombreLimpio,
                            Asunto = correo.Asunto,
                            Archivo = System.IO.Path.GetFileName(archivo),

                            // Propiedades visuales nuevas
                            ResultadoTexto = textoEstado,
                            ColorFondo = colorFondo,
                            ColorTexto = colorTexto
                        });
                    }
                }

                // 3. ¡Abrir la Ventana Bonita!
                string resumen = $"Se analizaron {totalArchivos} archivos adjuntos y se procesaron {totalProductosGeneral} productos en total.";

                var ventana = new Dialogs.ResultadosImportacionWindow(resumen, listaVisual);
                ventana.Owner = Window.GetWindow(this);
                ventana.ShowDialog();

                // 4. ¡AQUÍ ESTÁ LA NUEVA LÍNEA CLAVE! 
                // Solo recargamos la vista principal DESPUÉS de que el usuario cierre la ventana modal.
                var vm = this.DataContext as OrySiPOS.ViewModels.InventarioViewModel;
                vm?.CargarProductos();

            }
            catch (Exception ex)
            {
                Mouse.OverrideCursor = null;
                MessageBox.Show("Error: " + ex.Message, "Error al importar");
            }
        }

        private void MenuEditar_Click(object sender, RoutedEventArgs e)
        {
            // 1. Obtenemos el producto de la fila donde se hizo clic
            if (sender is MenuItem menuItem && menuItem.DataContext is Producto productoSeleccionado)
            {
                // 2. Obtenemos el "Cerebro" (ViewModel) de la página
                if (this.DataContext is InventarioViewModel vm)
                {
                    // 3. ¡Ejecutamos el comando manualmente!
                    if (vm.EditarProductoCommand.CanExecute(productoSeleccionado))
                    {
                        vm.EditarProductoCommand.Execute(productoSeleccionado);
                    }
                }
            }
        }
    }
}