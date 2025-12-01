using Microsoft.EntityFrameworkCore;
using OrySiPOS.ViewModels;
using System.Text.RegularExpressions; // Para Regex
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OrySiPOS.Views
{
    public partial class AjustesPage : Page
    {
        // Bandera para evitar bucles infinitos al sincronizar las cajas de contraseña
        private bool _isSyncing = false;

        public AjustesPage()
        {
            InitializeComponent();
            this.DataContext = new AjustesViewModel();

            // --- CARGAR CONTRASEÑA AL INICIO ---
            // Como el PasswordBox no tiene Binding directo, le metemos el valor manualmente al arrancar
            if (this.DataContext is AjustesViewModel vm)
            {
                PassBoxSecret.Password = vm.PassEmailInventario;
                TxtPassVisible.Text = vm.PassEmailInventario;
            }
        }

        // --- 1. VALIDACIÓN DE SOLO NÚMEROS (Para el Stock) ---
        private void SoloNumeros_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        // --- 2. LÓGICA DE SEGURIDAD DE CONTRASEÑA ---

        // A. Cuando escriben en los asteriscos, actualizamos el ViewModel
        private void PassBoxSecret_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isSyncing) return;

            if (this.DataContext is AjustesViewModel vm)
            {
                vm.PassEmailInventario = PassBoxSecret.Password;

                // Sincronizamos la caja visible por si acaso cambian de modo
                _isSyncing = true;
                TxtPassVisible.Text = PassBoxSecret.Password;
                _isSyncing = false;
            }
        }

        // B. Cuando escriben en texto plano (si está visible), actualizamos el ViewModel
        private void TxtPassVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSyncing) return;

            if (this.DataContext is AjustesViewModel vm)
            {
                vm.PassEmailInventario = TxtPassVisible.Text;

                _isSyncing = true;
                PassBoxSecret.Password = TxtPassVisible.Text;
                _isSyncing = false;
            }
        }

        // C. BOTÓN MOSTRAR/OCULTAR (CON PERMISO)
        private void BtnTogglePass_Click(object sender, RoutedEventArgs e)
        {
            // CASO A: Si está visible, lo ocultamos (no pide permiso para ocultar)
            if (TxtPassVisible.Visibility == Visibility.Visible)
            {
                TxtPassVisible.Visibility = Visibility.Collapsed;
                PassBoxSecret.Visibility = Visibility.Visible;

                // Devolvemos el foco a la caja secreta
                PassBoxSecret.Focus();
            }
            // CASO B: Quiere ver la contraseña (¡PEDIR PERMISO!)
            else
            {
                if (SolicitarPermisoAdministrador())
                {
                    PassBoxSecret.Visibility = Visibility.Collapsed;
                    TxtPassVisible.Visibility = Visibility.Visible;
                    TxtPassVisible.Focus();
                }
            }
        }

        // D. SIMULACIÓN DE SEGURIDAD
        private bool SolicitarPermisoAdministrador()
        {
            var resultado = MessageBox.Show(
                "Esta información es sensible.\n¿Eres el administrador del sistema?",
                "Confirmación de Seguridad",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            return resultado == MessageBoxResult.Yes;
        }

        // (Opcional) Si todavía tienes el botón de guardar con evento Click en el XAML
        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // Si el botón usa Command="{Binding...}" esto no se ejecuta, pero lo dejamos por si acaso.
        }

        // En WpfApp1/Views/AjustesPage.xaml.cs

        private async void BtnForzarSubida_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            btn.IsEnabled = false;
            string textoOriginal = btn.Content.ToString();
            btn.Content = "⚡ Preparando envío masivo...";

            try
            {
                var servicioNube = new OrySiPOS.Services.SupabaseService();

                await Task.Run(async () =>
                {
                    using (var db = new OrySiPOS.Data.InventarioDbContext())
                    {
                        // =======================================================
                        // 1. PREPARAR CATEGORÍAS (Memoria)
                        // =======================================================
                        Dispatcher.Invoke(() => btn.Content = "📦 Empaquetando Categorías...");

                        var dbCategorias = db.Categorias.ToList();
                        var loteCategorias = new List<OrySiPOS.Models.Supabase.CategoriaWeb>();

                        foreach (var c in dbCategorias)
                        {
                            loteCategorias.Add(new OrySiPOS.Models.Supabase.CategoriaWeb
                            {
                                Id = c.Id,
                                Nombre = c.Nombre,
                                UltimaActualizacion = DateTime.Now
                            });
                        }
                        // ¡ENVÍO MASIVO 1!
                        await servicioNube.SincronizarCategoriasMasivo(loteCategorias);


                        // =======================================================
                        // 2. PREPARAR SUBCATEGORÍAS
                        // =======================================================
                        Dispatcher.Invoke(() => btn.Content = "📦 Empaquetando Subcategorías...");

                        var dbSubcategorias = db.Subcategorias.ToList();
                        var loteSub = new List<OrySiPOS.Models.Supabase.SubcategoriaWeb>();

                        foreach (var s in dbSubcategorias)
                        {
                            loteSub.Add(new OrySiPOS.Models.Supabase.SubcategoriaWeb
                            {
                                Id = s.Id,
                                Nombre = s.Nombre,
                                CategoriaId = s.CategoriaId,
                                UltimaActualizacion = DateTime.Now
                            });
                        }
                        // ¡ENVÍO MASIVO 2!
                        await servicioNube.SincronizarSubcategoriasMasivo(loteSub);


                        // =======================================================
                        // 3. PREPARAR PRODUCTOS (Aquí estaba el mayor cuello de botella)
                        // =======================================================
                        Dispatcher.Invoke(() => btn.Content = "📦 Empaquetando Productos...");

                        // A. Traemos los productos con sus relaciones
                        var dbProductos = db.Productos
                                            .Include(p => p.Subcategoria)
                                            .ThenInclude(s => s.Categoria)
                                            .ToList();

                        // B. OPTIMIZACIÓN DE VENTAS: Traemos todos los conteos en UNA sola consulta
                        // (Antes hacías 1 consulta por cada producto = lentísimo)
                        var ventasDict = db.VentasDetalle
                                           .GroupBy(v => v.ProductoId)
                                           .Select(g => new { Id = g.Key, Total = g.Sum(x => x.Cantidad) })
                                           .ToDictionary(x => x.Id, x => x.Total);

                        var loteProductos = new List<OrySiPOS.Models.Supabase.ProductoWeb>();

                        foreach (var p in dbProductos)
                        {
                            if (p.EsServicio) continue; // Filtramos servicios

                            // Buscamos sus ventas en el diccionario (instantáneo)
                            int vendidos = ventasDict.ContainsKey(p.ID) ? ventasDict[p.ID] : 0;

                            string nombreCat = "General";
                            if (p.Subcategoria?.Categoria != null) nombreCat = p.Subcategoria.Categoria.Nombre;

                            loteProductos.Add(new OrySiPOS.Models.Supabase.ProductoWeb
                            {
                                Sku = p.ID,
                                Descripcion = p.Descripcion,
                                Precio = p.Precio,
                                Stock = p.Stock,
                                Activo = p.Activo,
                                ImagenUrl = p.ImagenUrl,
                                Costo = p.Costo,
                                ClaveSat = p.ClaveSat ?? "01010101",
                                ClaveUnidad = p.ClaveUnidad ?? "H87",
                                EsServicio = p.EsServicio,
                                PorcentajeIVA = p.PorcentajeIVA,
                                Categoria = nombreCat,
                                VentasTotales = vendidos, // Dato calculado rápido
                                UltimaActualizacion = DateTime.Now
                            });
                        }
                        // ¡ENVÍO MASIVO 3! (El camión grande)
                        await servicioNube.SincronizarProductosMasivo(loteProductos);


                        // =======================================================
                        // 4. PREPARAR CLIENTES
                        // =======================================================
                        Dispatcher.Invoke(() => btn.Content = "📦 Empaquetando Clientes...");

                        var dbClientes = db.Clientes.ToList();
                        var loteClientes = new List<OrySiPOS.Models.Supabase.ClienteWeb>();

                        foreach (var c in dbClientes)
                        {
                            loteClientes.Add(new OrySiPOS.Models.Supabase.ClienteWeb
                            {
                                Id = c.ID,
                                Rfc = c.RFC,
                                RazonSocial = c.RazonSocial,
                                Telefono = c.Telefono,
                                Correo = c.Correo,
                                Activo = c.Activo,
                                EsFactura = c.EsFactura,
                                CodigoPostal = c.CodigoPostal,
                                RegimenFiscal = c.RegimenFiscal,
                                UsoCfdi = c.UsoCFDI,
                                Creado = c.Creado,
                                UltimaActualizacion = DateTime.Now
                            });
                        }
                        // ¡ENVÍO MASIVO 4!
                        await servicioNube.SincronizarClientesMasivo(loteClientes);
                    }
                });

                MessageBox.Show("✅ ¡Sincronización completada!", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error: {ex.Message}");
            }
            finally
            {
                btn.IsEnabled = true;
                btn.Content = textoOriginal;
            }
        }

        private async void BtnForzarBajada_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            btn.IsEnabled = false;
            string textoOriginal = btn.Content.ToString();
            btn.Content = "⏳ Descargando...";

            try
            {
                var servicioNube = new OrySiPOS.Services.SupabaseService();

                int nuevosClientes = 0;
                int nuevosProductos = 0;
                int nuevasCats = 0;
                int nuevasSubCats = 0;

                await Task.Run(async () =>
                {
                    using (var db = new OrySiPOS.Data.InventarioDbContext())
                    {
                        // ---------------------------------------------------------
                        // PASO 1: CATEGORÍAS (Padres)
                        // ---------------------------------------------------------
                        Dispatcher.Invoke(() => btn.Content = "Bajando Categorías...");
                        var listaCategorias = await servicioNube.ObtenerCategoriasDeNube();

                        foreach (var catEntrante in listaCategorias)
                        {
                            // ESTRATEGIA ANTI-DUPLICADOS:
                            // 1. Buscamos por ID (Coincidencia exacta)
                            var catExistente = db.Categorias.FirstOrDefault(c => c.Id == catEntrante.Id);

                            // 2. Si no está por ID, buscamos por NOMBRE (para atrapar al "General" local)
                            if (catExistente == null)
                            {
                                catExistente = db.Categorias.FirstOrDefault(c => c.Nombre.ToLower() == catEntrante.Nombre.ToLower());
                            }

                            if (catExistente == null)
                            {
                                // CASO A: No existe ni por ID ni por Nombre -> LA CREAMOS
                                db.Categorias.Add(catEntrante);
                                nuevasCats++;
                            }
                            else
                            {
                                // CASO B: Ya existe (ej: "General" local).
                                // OPCIONAL: Podríamos actualizar el nombre si cambió en la nube, 
                                // pero lo importante es NO duplicar.

                                // ¡TRUCO PRO! Si la local tiene un ID diferente al de la nube (ej: Local 99, Nube 1),
                                // esto es complicado de arreglar al vuelo porque rompería las subcategorías locales.
                                // Lo mejor aquí es NO hacer nada y confiar en que la coincidencia de nombre es suficiente
                                // para que el usuario no vea dobles.
                            }
                        }
                        db.SaveChanges();

                        // ---------------------------------------------------------
                        // PASO 2: SUBCATEGORÍAS (Hijos)
                        // ---------------------------------------------------------
                        Dispatcher.Invoke(() => btn.Content = "Bajando Subcategorías...");
                        var listaSubcategorias = await servicioNube.ObtenerSubcategoriasDeNube();

                        foreach (var subEntrante in listaSubcategorias)
                        {
                            if (subEntrante.CategoriaId == null) continue;

                            // Buscamos si ya existe por ID o por NOMBRE dentro de la misma categoría
                            var subExistente = db.Subcategorias.FirstOrDefault(s => s.Id == subEntrante.Id);

                            if (subExistente == null)
                            {
                                subExistente = db.Subcategorias.FirstOrDefault(s =>
                                    s.Nombre.ToLower() == subEntrante.Nombre.ToLower() &&
                                    s.CategoriaId == subEntrante.CategoriaId);
                            }

                            if (subExistente == null)
                            {
                                // Verificamos que el papá exista (por ID de nube)
                                if (db.Categorias.Any(c => c.Id == subEntrante.CategoriaId))
                                {
                                    db.Subcategorias.Add(subEntrante);
                                    nuevasSubCats++;
                                }
                            }
                        }
                        db.SaveChanges();

                        // ---------------------------------------------------------
                        // PASO 3: PRODUCTOS (¡AQUÍ ESTÁ LA MEJORA!)
                        // ---------------------------------------------------------
                        Dispatcher.Invoke(() => btn.Content = "Bajando Productos...");
                        var productosWeb = await servicioNube.ObtenerProductosDeNube();

                        foreach (var prodWeb in productosWeb)
                        {
                            // Si ya existe el SKU (ID), lo ignoramos para no duplicar
                            if (db.Productos.Any(p => p.ID == prodWeb.Sku)) continue;

                            // --- BÚSQUEDA INTELIGENTE DE UBICACIÓN ---
                            int idSubcategoriaFinal = 1; // Un ID default por seguridad (asegura tener General)

                            // INTENTO A: Buscar directo en las SUBCATEGORÍAS (Lo que tú mandaste en SQL)
                            var subDirecta = db.Subcategorias.FirstOrDefault(s => s.Nombre == prodWeb.Categoria);

                            if (subDirecta != null)
                            {
                                // ¡Bingo! Encontramos que "Papel" es una subcategoría válida
                                idSubcategoriaFinal = subDirecta.Id;
                            }
                            else
                            {
                                // INTENTO B: Buscar en CATEGORÍAS PADRE (Por si acaso mandaste "Zapatería")
                                var catPadre = db.Categorias
                                                 .Include(c => c.Subcategorias)
                                                 .FirstOrDefault(c => c.Nombre == prodWeb.Categoria);

                                if (catPadre != null && catPadre.Subcategorias.Any())
                                {
                                    // Si mandaste el nombre del padre, lo metemos en su primer hijo
                                    idSubcategoriaFinal = catPadre.Subcategorias.First().Id;
                                }
                            }

                            // Creamos el producto
                            var nuevoProducto = new OrySiPOS.Models.Producto
                            {
                                ID = (int)prodWeb.Sku,
                                Descripcion = prodWeb.Descripcion,
                                Precio = prodWeb.Precio,
                                Stock = prodWeb.Stock,
                                Activo = prodWeb.Activo,
                                ImagenUrl = prodWeb.ImagenUrl,

                                SubcategoriaId = idSubcategoriaFinal,

                                // --- ¡AHORA SÍ TRAEMOS DATOS REALES! ---
                                Costo = prodWeb.Costo, // Ya no es 0 fijo
                                EsServicio = prodWeb.EsServicio,
                                PorcentajeIVA = prodWeb.PorcentajeIVA,
                                ClaveSat = string.IsNullOrEmpty(prodWeb.ClaveSat) ? "01010101" : prodWeb.ClaveSat,
                                ClaveUnidad = string.IsNullOrEmpty(prodWeb.ClaveUnidad) ? "H87" : prodWeb.ClaveUnidad
                            };

                            db.Productos.Add(nuevoProducto);
                            nuevosProductos++;
                        }
                        db.SaveChanges();

                        // ---------------------------------------------------------
                        // PASO 4: CLIENTES
                        // ---------------------------------------------------------
                        Dispatcher.Invoke(() => btn.Content = "Bajando Clientes...");
                        var clientesWeb = await servicioNube.ObtenerClientesDeNube();
                        foreach (var cliWeb in clientesWeb)
                        {
                            if (!db.Clientes.Any(c => c.RFC == cliWeb.RFC))
                            {
                                db.Clientes.Add(cliWeb);
                                nuevosClientes++;
                            }
                        }
                        db.SaveChanges();
                    }
                });

                MessageBox.Show(
                    $"✅ ¡BAJADA COMPLETADA!\n\n" +
                    $"📂 Categorías: {nuevasCats}\n" +
                    $"file_folder Subcategorías: {nuevasSubCats}\n" +
                    $"📦 Productos: {nuevosProductos}\n" +
                    $"👥 Clientes: {nuevosClientes}\n\n" +
                    "Todo está en su lugar correcto.",
                    "Sincronización Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                var mensajeError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                MessageBox.Show("❌ Error al bajar datos:\n" + mensajeError);
            }
            finally
            {
                btn.IsEnabled = true;
                btn.Content = textoOriginal;
            }
        }
    }
}