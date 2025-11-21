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
using WpfApp1.Data;
using WpfApp1.Models;
using WpfApp1.ViewModels;
using WpfApp1.Views.Dialogs;

public class ProductoDetalleItem
{
    public string Propiedad { get; set; }
    public string Valor { get; set; }
}

namespace WpfApp1.Views
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
        private void BtnImportar_Click(object sender, RoutedEventArgs e)
        {
            // 1. Abrir diálogo para buscar el archivo
            OpenFileDialog openDialog = new OpenFileDialog
            {
                Filter = "Archivo CSV (*.csv)|*.csv",
                Title = "Importar productos masivamente"
            };

            if (openDialog.ShowDialog() == true)
            {
                try
                {
                    using (var db = new InventarioDbContext())
                    {
                        // 2. ESTRATEGIA DE SUBCATEGORÍA
                        // Buscamos una subcategoría por defecto para asignarle a los nuevos productos.
                        // Intentamos buscar una que se llame "General", si no, agarramos la primera que haya.
                        var subcatPorDefecto = db.Subcategorias.FirstOrDefault(s => s.Nombre == "General")
                                               ?? db.Subcategorias.FirstOrDefault();

                        if (subcatPorDefecto == null)
                        {
                            MessageBox.Show("Error: No existen subcategorías en la base de datos. Crea al menos una (ej. 'General') antes de importar.", "Falta Configuración");
                            return;
                        }

                        // 3. LEER EL ARCHIVO
                        string[] lineas = File.ReadAllLines(openDialog.FileName);
                        int contados = 0;
                        int errores = 0;

                        // Empezamos en i = 1 para saltarnos los encabezados
                        for (int i = 1; i < lineas.Length; i++)
                        {
                            string linea = lineas[i];
                            if (string.IsNullOrWhiteSpace(linea)) continue;

                            // Separamos por comas
                            string[] partes = linea.Split(',');

                            // VALIDACIÓN BÁSICA: Necesitamos al menos Descripción, Precio, Costo, Stock (4 columnas)
                            if (partes.Length < 4)
                            {
                                errores++;
                                continue;
                            }

                            try
                            {
                                // 4. CREAR EL PRODUCTO
                                var nuevoProd = new Producto
                                {
                                    Descripcion = partes[0].Trim(), // Columna A: Descripción
                                    Precio = decimal.Parse(partes[1]), // Columna B: Precio
                                    Costo = decimal.Parse(partes[2]),  // Columna C: Costo
                                    Stock = int.Parse(partes[3]),      // Columna D: Stock

                                    // Datos automáticos
                                    Activo = true,
                                    ImagenUrl = "https://via.placeholder.com/150", // Imagen genérica
                                    SubcategoriaId = subcatPorDefecto.Id // Asignamos la categoría "General"
                                };

                                db.Productos.Add(nuevoProd);
                                contados++;
                            }
                            catch
                            {
                                // Si falla una línea (ej: precio con letras), la saltamos y contamos el error
                                errores++;
                            }
                        }

                        // 5. GUARDAR EN BD
                        db.SaveChanges();

                        string mensaje = $"Se importaron {contados} productos exitosamente.";
                        if (errores > 0) mensaje += $"\nHubo {errores} líneas con errores que se omitieron.";

                        MessageBox.Show(mensaje, "Importación Finalizada", MessageBoxButton.OK, MessageBoxImage.Information);

                        // 6. REFRESCAR LA PANTALLA
                        var vm = this.DataContext as InventarioViewModel;
                        vm?.CargarProductos();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error crítico al importar: {ex.Message}", "Error");
                }
            }
        }

        // En Views/InventarioPage.xaml.cs

        private void BtnImportarXML_Click(object sender, RoutedEventArgs e)
        {
            // 1. Buscar el archivo
            OpenFileDialog openDialog = new OpenFileDialog
            {
                Filter = "Factura XML (*.xml)|*.xml",
                Title = "Selecciona la factura de compra (CFDI)"
            };

            if (openDialog.ShowDialog() == true)
            {
                try
                {
                    using (var db = new InventarioDbContext())
                    {
                        // A. Buscamos la categoría por defecto (igual que en CSV)
                        var subcatPorDefecto = db.Subcategorias.FirstOrDefault(s => s.Nombre == "General")
                                               ?? db.Subcategorias.FirstOrDefault();

                        if (subcatPorDefecto == null)
                        {
                            MessageBox.Show("Necesitas tener al menos una subcategoría (ej. 'General') para importar.", "Error");
                            return;
                        }

                        // B. Cargamos el XML en memoria
                        XDocument doc = XDocument.Load(openDialog.FileName);

                        // C. Detectar el "Namespace" (El idioma del SAT)
                        // Las facturas tienen una url rara al principio (http://www.sat.gob.mx/cfd/4).
                        // Necesitamos esa URL para encontrar las etiquetas hijas.
                        XNamespace cfdi = doc.Root.Name.Namespace;

                        // D. Buscar todos los "Conceptos" (Los productos de la factura)
                        var conceptos = doc.Descendants(cfdi + "Concepto").ToList();

                        int nuevos = 0;
                        int actualizados = 0;

                        foreach (var concepto in conceptos)
                        {
                            // 1. Extraer datos del XML (Atributos)
                            string descripcion = concepto.Attribute("Descripcion")?.Value;
                            string noIdentificacion = concepto.Attribute("NoIdentificacion")?.Value ?? ""; // Código de barras o SKU del proveedor

                            // Cantidad (El XML puede traer decimales, nosotros usamos enteros en stock, redondeamos)
                            decimal cantDec = decimal.Parse(concepto.Attribute("Cantidad")?.Value ?? "0");
                            int cantidad = (int)Math.Round(cantDec);

                            // Costo Unitario (ValorUnitario en el XML)
                            decimal costo = decimal.Parse(concepto.Attribute("ValorUnitario")?.Value ?? "0");

                            // Precio Sugerido (Calculamos una ganancia del 30% automática, luego tú la cambias)
                            decimal precioSugerido = costo * 1.30m;

                            // 2. LÓGICA DE MATCH (Emparejamiento)
                            // Primero buscamos si ya tenemos ese producto por su nombre exacto
                            var productoExistente = db.Productos
                                .FirstOrDefault(p => p.Descripcion.ToLower() == descripcion.ToLower());

                            if (productoExistente != null)
                            {
                                // --- ESCENARIO: YA EXISTE ---
                                // Solo sumamos al stock y actualizamos el costo (último costo)
                                productoExistente.Stock += cantidad;
                                productoExistente.Costo = costo;
                                // Opcional: ¿Actualizar precio? Mejor no, para no afectar tus márgenes sin que te des cuenta.

                                actualizados++;
                            }
                            else
                            {
                                // --- ESCENARIO: NUEVO PRODUCTO ---
                                var nuevoProd = new Producto
                                {
                                    Descripcion = descripcion,
                                    Costo = costo,
                                    Precio = precioSugerido,
                                    Stock = cantidad,
                                    Activo = true,
                                    SubcategoriaId = subcatPorDefecto.Id,
                                    ImagenUrl = "https://via.placeholder.com/150" // Imagen temporal
                                };

                                db.Productos.Add(nuevoProd);
                                nuevos++;
                            }
                        }

                        // E. Guardar Cambios
                        db.SaveChanges();

                        // F. Refrescar la pantalla
                        var vm = this.DataContext as InventarioViewModel;
                        vm?.CargarProductos();

                        MessageBox.Show($"Proceso terminado.\n\nNuevos productos creados: {nuevos}\nProductos actualizados (stock): {actualizados}",
                                        "Importación XML Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al leer el XML: {ex.Message}\n\nAsegúrate de que sea una factura válida (CFDI 3.3 o 4.0).", "Error");
                }
            }
        }

        // En Views/InventarioPage.xaml.cs

        // Método público que recibe la ruta de un archivo XML y lo procesa
        // En Views/InventarioPage.xaml.cs

        public int ProcesarArchivoXML(string rutaArchivo)
        {
            int nuevos = 0;
            int actualizados = 0;

            try
            {
                // 1. DATABASE CONTEXT
                using (var db = new WpfApp1.Data.InventarioDbContext())
                {
                    // Buscamos la subcategoría por defecto
                    var subcatPorDefecto = db.Subcategorias.FirstOrDefault(s => s.Nombre == "General")
                                           ?? db.Subcategorias.FirstOrDefault();

                    if (subcatPorDefecto == null) return 0;

                    // 2. CARGAR Y ANALIZAR XML (CFDI)
                    var doc = System.Xml.Linq.XDocument.Load(rutaArchivo);
                    // Detectamos el Namespace del SAT
                    System.Xml.Linq.XNamespace cfdi = doc.Root.Name.Namespace;

                    // Obtenemos todos los <cfdi:Concepto>
                    var conceptos = doc.Descendants(cfdi + "Concepto").ToList();

                    foreach (var concepto in conceptos)
                    {
                        // 3. EXTRACCIÓN Y LIMPIEZA DE DATOS
                        string descripcion = concepto.Attribute("Descripcion")?.Value?.Trim();

                        if (string.IsNullOrWhiteSpace(descripcion)) continue;

                        // --- ¡NUEVAS LÍNEAS CLAVE! ---
                        // Leemos las claves fiscales del XML
                        string claveSat = concepto.Attribute("ClaveProdServ")?.Value;
                        string claveUnidad = concepto.Attribute("ClaveUnidad")?.Value;
                        // -----------------------------

                        decimal cantDec = decimal.Parse(concepto.Attribute("Cantidad")?.Value ?? "0");
                        int cantidad = (int)Math.Round(cantDec);
                        decimal costo = decimal.Parse(concepto.Attribute("ValorUnitario")?.Value ?? "0");
                        decimal precioSugerido = costo * 1.30m; // Sugerimos un 30% de ganancia

                        // 4. LÓGICA DE MATCH (ANTI-DUPLICADOS)
                        var productoEnMemoria = db.Productos.Local
                            .FirstOrDefault(p => p.Descripcion.ToLower() == descripcion.ToLower());

                        var productoExistente = productoEnMemoria ?? db.Productos
                            .FirstOrDefault(p => p.Descripcion.ToLower() == descripcion.ToLower());

                        if (productoExistente != null)
                        {
                            // 5. ACTUALIZAR STOCK, COSTO Y CLAVES FISCALES
                            productoExistente.Stock += cantidad;
                            if (costo > 0) productoExistente.Costo = costo;

                            // --- ¡AÑADIDO PARA ACTUALIZAR DATOS FISCALES! ---
                            if (!string.IsNullOrEmpty(claveSat)) productoExistente.ClaveSat = claveSat;
                            if (!string.IsNullOrEmpty(claveUnidad)) productoExistente.ClaveUnidad = claveUnidad;
                            // ------------------------------------------------

                            actualizados++;
                        }
                        else
                        {
                            // 6. CREAR NUEVO PRODUCTO
                            var nuevoProd = new WpfApp1.Models.Producto
                            {
                                Descripcion = descripcion,
                                Costo = costo,
                                Precio = precioSugerido,
                                Stock = cantidad,
                                Activo = true,
                                SubcategoriaId = subcatPorDefecto.Id,
                                ImagenUrl = "https://via.placeholder.com/150",

                                // --- ¡AÑADIDO PARA ASIGNAR DATOS FISCALES! ---
                                // Usamos el operador ?? para asignar un valor por defecto si la lectura del XML falló.
                                ClaveSat = claveSat ?? "01010101",
                                ClaveUnidad = claveUnidad ?? "H87"
                                // --------------------------------------------
                            };

                            db.Productos.Add(nuevoProd);
                            nuevos++;
                        }
                    }

                    // 7. GUARDAR Y REFRESCAR
                    db.SaveChanges();

                    // La recarga de la vista se hace en el código que llama a esta función (BtnEscanearCorreo_Click)
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error procesando archivo: " + ex.Message);
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

            using (var db = new WpfApp1.Data.InventarioDbContext())
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

            // 4. Transformar el objeto en una lista de Propiedad: Valor
            var detalles = new List<ProductoDetalleItem>
            {
                new ProductoDetalleItem { Propiedad = "ID (SKU)", Valor = productoCompleto.ID.ToString() },
                new ProductoDetalleItem { Propiedad = "Descripción", Valor = productoCompleto.Descripcion },
                new ProductoDetalleItem { Propiedad = "Categoría", Valor = productoCompleto.Subcategoria?.Categoria?.Nombre ?? "N/A" },
                new ProductoDetalleItem { Propiedad = "Subcategoría", Valor = productoCompleto.Subcategoria?.Nombre ?? "N/A" },
                new ProductoDetalleItem { Propiedad = "Precio Venta", Valor = productoCompleto.Precio.ToString("C") },
                new ProductoDetalleItem { Propiedad = "Costo", Valor = productoCompleto.Costo.ToString("C") },
                new ProductoDetalleItem { Propiedad = "Ganancia (Margen)", Valor = productoCompleto.Ganancia.ToString("C") },
                new ProductoDetalleItem { Propiedad = "Stock Actual", Valor = productoCompleto.Stock.ToString() + " uds." },
                new ProductoDetalleItem { Propiedad = "Activo para Venta", Valor = productoCompleto.Activo ? "Sí" : "No" },
                new ProductoDetalleItem { Propiedad = "Clave SAT", Valor = productoCompleto.ClaveSat },
                new ProductoDetalleItem { Propiedad = "Clave Unidad", Valor = productoCompleto.ClaveUnidad },
                new ProductoDetalleItem { Propiedad = "URL Imagen", Valor = productoCompleto.ImagenUrl },
            };

            // 5. Mostrar el diálogo (VisorReporteWindow)
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

        

        private void Duplicar_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var producto = menuItem?.DataContext as Producto;
            if (producto == null) return;

            MessageBox.Show($"Duplicar: {producto.Descripcion} (Próximamente)");
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
                producto.Activo = !producto.Activo;
                try
                {
                    using (var db = new InventarioDbContext())
                    {
                        db.Productos.Update(producto);
                        db.SaveChanges();
                    }
                    VM.CargarProductos();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}");
                }
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
                var vm = this.DataContext as WpfApp1.ViewModels.InventarioViewModel;
                vm?.CargarProductos();

            }
            catch (Exception ex)
            {
                Mouse.OverrideCursor = null;
                MessageBox.Show("Error: " + ex.Message, "Error al importar");
            }
        }
    }
}