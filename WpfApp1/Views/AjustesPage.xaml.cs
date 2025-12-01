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
            btn.Content = "Sincronizando todo el sistema...";

            try
            {
                var servicioNube = new OrySiPOS.Services.SupabaseService();

                // Contadores para el reporte final
                int cats = 0, subcats = 0, prods = 0, clientes = 0;

                await Task.Run(async () =>
                {
                    using (var db = new OrySiPOS.Data.InventarioDbContext())
                    {
                        // -------------------------------------------------------
                        // FASE 1: CATEGORÍAS (Para que se arme el menú web)
                        // -------------------------------------------------------
                        var listaCategorias = db.Categorias.ToList();
                        foreach (var cat in listaCategorias)
                        {
                            Dispatcher.Invoke(() => btn.Content = $"Subiendo Categ: {cat.Nombre}...");
                            await servicioNube.SincronizarCategoria(cat);
                            cats++;
                        }

                        // -------------------------------------------------------
                        // FASE 2: SUBCATEGORÍAS
                        // -------------------------------------------------------
                        var listaSubcategorias = db.Subcategorias.ToList();
                        foreach (var sub in listaSubcategorias)
                        {
                            Dispatcher.Invoke(() => btn.Content = $"Subiendo Subcat: {sub.Nombre}...");
                            await servicioNube.SincronizarSubcategoria(sub);
                            subcats++;
                        }

                        // -------------------------------------------------------
                        // FASE 3: PRODUCTOS (Ahora sí, con sus categorías listas)
                        // -------------------------------------------------------
                        // Usamos Include para asegurarnos de que el producto sepa cuál es su categoría
                        var listaProductos = db.Productos
                                               .Include(p => p.Subcategoria)
                                               .ThenInclude(s => s.Categoria)
                                               .ToList();

                        foreach (var prod in listaProductos)
                        {
                            Dispatcher.Invoke(() => btn.Content = $"Subiendo Prod: {prod.Descripcion}...");
                            // Filtro opcional: subir solo activos o excluir servicios
                            await servicioNube.SincronizarProducto(prod);
                            prods++;
                        }

                        // -------------------------------------------------------
                        // FASE 4: CLIENTES (Lo que ya tenías)
                        // -------------------------------------------------------
                        var listaClientes = db.Clientes.ToList();
                        foreach (var cliente in listaClientes)
                        {
                            Dispatcher.Invoke(() => btn.Content = $"Subiendo Cliente: {cliente.RazonSocial}...");
                            await servicioNube.SincronizarCliente(cliente);
                            clientes++;
                        }
                    }
                });

                MessageBox.Show(
                    $"✅ ¡SINCRONIZACIÓN TOTAL COMPLETADA!\n\n" +
                    $"📂 Categorías: {cats}\n" +
                    $"file_folder Subcategorías: {subcats}\n" +
                    $"📦 Productos: {prods}\n" +
                    $"👥 Clientes: {clientes}\n\n" +
                    "Tu base de datos local y la nube ahora están comunicadas.",
                    "Operación Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ Ocurrió un error:\n" + ex.Message);
            }
            finally
            {
                btn.IsEnabled = true;
                btn.Content = "⚠️ FORZAR SUBIDA DE TODO (MASTER SYNC)";
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