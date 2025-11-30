using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OrySiPOS.Data;
using OrySiPOS.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks; // Para el Sync
using System.ComponentModel;   // Para ICollectionView
using System.Windows.Data;     // Para CollectionViewSource
using System.Collections.Generic;
using System.Windows.Threading; // <--- Necesario para el Timer
using System.Threading.Tasks;

namespace OrySiPOS.Views.Dialogs
{
    public partial class NuevoProductoModal : Window
    {
        public Producto ProductoRegistrado { get; private set; }
        private Producto _productoEdicion; // Guardamos el producto si es edición
                                           // Declaramos el temporizador
        // Listas en memoria para el filtrado rápido (Modelo de Base de Datos)
        private List<SatProducto> _listaSatProductos;
        private List<SatUnidad> _listaSatUnidades;

        // Vistas (Lentes) para filtrar
        private ICollectionView _vistaClavesSat;
        private ICollectionView _vistaUnidades;

        // Constructor modificado: acepta producto opcional
        public NuevoProductoModal(Producto productoParaEditar = null)
        {
            InitializeComponent();

            // 1. Cargar Datos
            CargarCombos();       // Categorías y Subcategorías
            CargarCatalogosSat(); // Catálogos del SAT desde BD

            if (productoParaEditar != null)
            {
                // --- MODO EDICIÓN ---
                _productoEdicion = productoParaEditar;
                Title = "Editar Producto";
                btnGuardar.Content = "Actualizar";

                // Bloqueamos el stock visualmente
                txtStock.IsEnabled = false;
                txtStock.ToolTip = "Usa el módulo de 'Movimientos' para ajustar existencias.";

                CargarDatosEnFormulario();
            }
            else
            {
                // --- MODO CREACIÓN ---
                _productoEdicion = null;
                Title = "Nuevo Producto";
                btnGuardar.Content = "Guardar Producto";
                txtStock.IsEnabled = true; // En creación sí permitimos stock inicial

                // Defaults SAT (Genéricos)
                CmbClaveSat.SelectedValue = "01010101";
                CmbClaveUnidad.SelectedValue = "H87";
            }
        }

        // Método para arrastrar la ventana
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
        }

        private void CargarCombos()
        {
            using (var db = new InventarioDbContext())
            {
                var categorias = db.Categorias.Include(c => c.Subcategorias).ToList();
                CmbCategoria.ItemsSource = categorias;
            }
        }

        private void TxtBusquedaSat_TextChanged(object sender, TextChangedEventArgs e)
        {
            
        }

        // Método exclusivo para buscar cuando tú lo ordenes
        private async void EjecutarBusquedaSat()
        {
            string textoABuscar = CmbClaveSat.Text.Trim();

            if (string.IsNullOrEmpty(textoABuscar) || textoABuscar.Length < 3) return;

            try
            {
                // Indicador visual opcional (cambiar el cursor a relojito)
                Mouse.OverrideCursor = Cursors.Wait;

                var resultados = await Task.Run(() =>
                {
                    return OrySiPOS.Helpers.CatalogosSAT.BuscarPorDescripcion(textoABuscar);
                });

                CmbClaveSat.ItemsSource = resultados;

                // Abrimos la lista automáticamente si encontramos algo
                if (resultados.Count > 0)
                {
                    CmbClaveSat.IsDropDownOpen = true;
                }
                else
                {
                    // Opcional: Avisar que no hubo suerte
                    CmbClaveSat.IsDropDownOpen = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error SAT: " + ex.Message);
            }
            finally
            {
                // Regresamos el cursor a la normalidad
                Mouse.OverrideCursor = null;
            }
        }
        private void CargarCatalogosSat()
        {
            // NO cargamos los 50,000 productos al inicio, eso congela la flechita.
            // Solo cargamos una lista vacía o los más comunes si quieres.
            CmbClaveSat.ItemsSource = new List<SatProducto>();

            using (var db = new InventarioDbContext())
            {
                // Para unidades son poquitas (unas 200), esas SÍ las podemos cargar todas
                _listaSatUnidades = db.SatUnidades.OrderBy(x => x.Descripcion).ToList();
                _vistaUnidades = CollectionViewSource.GetDefaultView(_listaSatUnidades);
                CmbClaveUnidad.ItemsSource = _vistaUnidades;
            }
        }

        private void CargarDatosEnFormulario()
        {
            // 1. Datos simples
            txtDescripcion.Text = _productoEdicion.Descripcion;
            txtPrecio.Text = _productoEdicion.Precio.ToString();
            txtCosto.Text = _productoEdicion.Costo.ToString();
            txtStock.Text = _productoEdicion.Stock.ToString();
            txtImagenUrl.Text = _productoEdicion.ImagenUrl;
            chkEsServicio.IsChecked = _productoEdicion.EsServicio;
            // 2. Datos SAT
            // Intentamos seleccionar de la lista
            CmbClaveSat.SelectedValue = _productoEdicion.ClaveSat;
            // Si la clave no está en la lista (ej. es antigua o rara), ponemos el texto directo
            if (CmbClaveSat.SelectedIndex == -1) CmbClaveSat.Text = _productoEdicion.ClaveSat;

            CmbClaveUnidad.SelectedValue = _productoEdicion.ClaveUnidad;
            if (CmbClaveUnidad.SelectedIndex == -1) CmbClaveUnidad.Text = _productoEdicion.ClaveUnidad;

            // 3. Seleccionar Categoría y Subcategoría
            if (_productoEdicion.Subcategoria != null)
            {
                foreach (Categoria cat in CmbCategoria.ItemsSource)
                {
                    if (cat.Id == _productoEdicion.Subcategoria.CategoriaId)
                    {
                        CmbCategoria.SelectedItem = cat;
                        CmbSubcategoria.ItemsSource = cat.Subcategorias;

                        foreach (Subcategoria sub in CmbSubcategoria.ItemsSource)
                        {
                            if (sub.Id == _productoEdicion.SubcategoriaId)
                            {
                                CmbSubcategoria.SelectedItem = sub;
                                break;
                            }
                        }
                        break;
                    }
                }
            }

            // 4. Selección de IVA (Blindada contra errores de decimales)
            bool ivaEncontrado = false;
            foreach (ComboBoxItem item in CmbIVA.Items)
            {
                if (item.Tag != null && decimal.TryParse(item.Tag.ToString(),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out decimal valorTag))
                {
                    if (Math.Abs(valorTag - _productoEdicion.PorcentajeIVA) < 0.0001m)
                    {
                        CmbIVA.SelectedItem = item;
                        ivaEncontrado = true;
                        break;
                    }
                }
            }
            if (!ivaEncontrado) CmbIVA.SelectedIndex = 0; // Default 16%
        }

        private void CmbCategoria_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbCategoria.SelectedItem is Categoria cat)
            {
                CmbSubcategoria.ItemsSource = cat.Subcategorias;
                CmbSubcategoria.IsEnabled = true;
                if (cat.Subcategorias.Any()) CmbSubcategoria.SelectedIndex = 0;
            }
            else
            {
                CmbSubcategoria.ItemsSource = null;
                CmbSubcategoria.IsEnabled = false;
            }
        }

        // --- MÉTODOS DE FILTRADO SAT ---

        private void CmbClaveSat_KeyUp(object sender, KeyEventArgs e)
        {
            // 1. Teclas de Navegación:
            // Si el usuario solo se está moviendo con flechas por la lista YA abierta, no hacemos nada.
            if (e.Key == Key.Down || e.Key == Key.Up || e.Key == Key.Left || e.Key == Key.Right)
                return;

            // 2. Tecla de Acción (ENTER):
            // Aquí es donde lanzamos la búsqueda nueva y abrimos la lista.
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                EjecutarBusquedaSat();
                return;
            }

            // 3. Cualquier otra tecla (Letras, Números, Borrar):
            // Significa que el usuario está editando el texto.
            // CERRAMOS la lista inmediatamente para no mostrar resultados "viejos" o falsos.
            CmbClaveSat.IsDropDownOpen = false;

            // Opcional: Si quieres limpiar la selección interna para evitar comportamientos raros:
            // CmbClaveSat.SelectedItem = null; 
        }

        private void CmbClaveUnidad_KeyUp(object sender, KeyEventArgs e)
        {
            FiltarCombos(CmbClaveUnidad, _vistaUnidades);
        }

        private void FiltarCombos(ComboBox cmb, ICollectionView vista)
        {
            if (cmb == null || vista == null) return;

            // Ignorar teclas de navegación para permitir moverse en la lista
            if (Keyboard.IsKeyDown(Key.Down) || Keyboard.IsKeyDown(Key.Up) || Keyboard.IsKeyDown(Key.Enter)) return;

            string texto = cmb.Text;

            if (string.IsNullOrEmpty(texto))
            {
                vista.Filter = null;
            }
            else
            {
                vista.Filter = (obj) =>
                {
                    string busqueda = texto.ToUpper();

                    if (obj is SatProducto prod)
                        return prod.Clave.Contains(busqueda) || prod.Descripcion.ToUpper().Contains(busqueda);

                    if (obj is SatUnidad uni)
                        return uni.Clave.Contains(busqueda) || uni.Descripcion.ToUpper().Contains(busqueda);

                    return false;
                };
            }

            cmb.IsDropDownOpen = true;
            // Mantiene el cursor en su lugar
            cmb.GetBindingExpression(ComboBox.TextProperty)?.UpdateSource();
        }

        // En Views/Dialogs/NuevoProductoModal.xaml.cs

        private void ChkEsServicio_Checked(object sender, RoutedEventArgs e)
        {
            // 1. Bloquear Stock visualmente
            txtStock.Text = "0"; // O puedes poner "∞" si cambias validaciones, pero 0 o 1 funciona internamente
            txtStock.IsEnabled = false;
            txtStock.ToolTip = "Los servicios tienen stock infinito.";

            // 2. Asignar Clave Unidad SAT para Servicios (E48 - Unidad de servicio)
            // Buscamos en la lista de unidades si existe E48 y la seleccionamos
            CmbClaveUnidad.Text = "E48"; // Valor visual rápido

            // Intentar seleccionarlo del combo si ya está cargado
            if (CmbClaveUnidad.ItemsSource is ICollectionView vista)
            {
                // Lógica simple para seleccionar el objeto si existe en tu lista cargada
                foreach (var item in _listaSatUnidades)
                {
                    if (item.Clave == "E48")
                    {
                        CmbClaveUnidad.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        private void ChkEsServicio_Unchecked(object sender, RoutedEventArgs e)
        {
            // Restaurar comportamiento normal
            txtStock.IsEnabled = true;
            txtStock.Text = "1";
            txtStock.ToolTip = null;

            // Regresar a Pieza (H87) por defecto
            CmbClaveUnidad.Text = "H87";
        }

        // --- GUARDADO ---

        private void Guardar_Click(object sender, RoutedEventArgs e)
        {
            // 1. Validaciones básicas
            if (string.IsNullOrWhiteSpace(txtDescripcion.Text)) { MessageBox.Show("Falta descripción"); return; }
            if (!decimal.TryParse(txtPrecio.Text, out decimal precio)) { MessageBox.Show("Precio inválido"); return; }

            decimal.TryParse(txtCosto.Text, out decimal costo);
            int.TryParse(txtStock.Text, out int stock);

            string img = string.IsNullOrWhiteSpace(txtImagenUrl.Text) ? "https://via.placeholder.com/150" : txtImagenUrl.Text;

            // 2. CAPTURAR CLAVES SAT (Inteligente)
            string claveSatFinal = CmbClaveSat.SelectedValue != null ? CmbClaveSat.SelectedValue.ToString() : CmbClaveSat.Text.Trim();
            string unidadFinal = CmbClaveUnidad.SelectedValue != null ? CmbClaveUnidad.SelectedValue.ToString() : CmbClaveUnidad.Text.Trim();

            if (string.IsNullOrEmpty(claveSatFinal)) claveSatFinal = "01010101";
            if (string.IsNullOrEmpty(unidadFinal)) unidadFinal = "H87";

            // 3. IVA
            decimal ivaSeleccionado = 0.16m;
            if (CmbIVA.SelectedItem is ComboBoxItem item)
            {
                decimal.TryParse(item.Tag.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out ivaSeleccionado);
            }

            var subcat = CmbSubcategoria.SelectedItem as Subcategoria;
            if (subcat == null) { MessageBox.Show("Selecciona subcategoría"); return; }

            try
            {
                int idParaSync = 0;

                using (var db = new InventarioDbContext())
                {
                    // A. AUTO-APRENDIZAJE: Guardar claves SAT nuevas
                    var existeProdSat = db.SatProductos.Find(claveSatFinal);
                    if (existeProdSat == null)
                    {
                        db.SatProductos.Add(new SatProducto { Clave = claveSatFinal, Descripcion = "(Nuevo - Guardado Automático)" });
                    }

                    var existeUnidadSat = db.SatUnidades.Find(unidadFinal);
                    if (existeUnidadSat == null)
                    {
                        db.SatUnidades.Add(new SatUnidad { Clave = unidadFinal, Descripcion = "(Nuevo - Guardado Automático)" });
                    }
                    // Guardamos los catálogos antes de seguir
                    db.SaveChanges();


                    // B. GUARDAR PRODUCTO
                    if (_productoEdicion == null)
                    {
                        // --- CREAR ---
                        ProductoRegistrado = new Producto
                        {
                            Descripcion = txtDescripcion.Text,
                            EsServicio = chkEsServicio.IsChecked == true,
                            // Si es servicio, guardamos stock 0 o 1, da igual, la lógica de venta lo ignorará.
                            Stock = (chkEsServicio.IsChecked == true) ? 0 : stock,
                            Precio = precio,
                            Costo = costo,
                            ImagenUrl = img,
                            SubcategoriaId = subcat.Id,
                            PorcentajeIVA = ivaSeleccionado,
                            Activo = true,
                            ClaveSat = claveSatFinal,
                            ClaveUnidad = unidadFinal
                        };
                        db.Productos.Add(ProductoRegistrado);
                        db.SaveChanges();
                        idParaSync = ProductoRegistrado.ID;
                    }
                    else
                    {
                        // MODO EDITAR: AQUÍ HACEMOS EL CAMBIO
                        var prodDb = db.Productos.Find(_productoEdicion.ID);
                        if (prodDb != null)
                        {
                            // 1. Actualizamos la BD (Tu código original)
                            prodDb.Descripcion = txtDescripcion.Text;
                            prodDb.Precio = precio;
                            prodDb.Costo = costo;
                            prodDb.PorcentajeIVA = ivaSeleccionado;
                            prodDb.ImagenUrl = img;
                            prodDb.SubcategoriaId = subcat.Id;
                            prodDb.ClaveSat = claveSatFinal;
                            prodDb.ClaveUnidad = unidadFinal;
                            prodDb.EsServicio = chkEsServicio.IsChecked == true;
                            if (prodDb.EsServicio) prodDb.Stock = 0; // Opcional: resetear stock

                            db.Productos.Update(prodDb);
                            db.SaveChanges();
                            idParaSync = prodDb.ID;

                            // 2. ¡NUEVO! ACTUALIZAMOS EL OBJETO VISUAL EN MEMORIA
                            // Esto dispara el OnPropertyChanged y actualiza el icono al instante
                            _productoEdicion.Descripcion = txtDescripcion.Text;
                            _productoEdicion.Precio = precio;
                            _productoEdicion.Costo = costo;
                            _productoEdicion.PorcentajeIVA = ivaSeleccionado;
                            // _productoEdicion.Stock = stock; // El stock no se edita aquí, recuerda
                            _productoEdicion.ClaveSat = claveSatFinal;     // <--- Dispara cambio de icono
                            _productoEdicion.ClaveUnidad = unidadFinal;    // <--- Dispara cambio de icono
                            _productoEdicion.Subcategoria = subcat; // Para que se vea el nombre de subcategoría
                                                                    // Actualizar objeto en memoria
                            _productoEdicion.EsServicio = chkEsServicio.IsChecked == true;
                        }
                    }
                }

                // C. SYNC NUBE (Segundo plano)
                if (idParaSync > 0)
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            using (var dbSync = new InventarioDbContext())
                            {
                                var prodSync = await dbSync.Productos
                                    .Include(p => p.Subcategoria).ThenInclude(s => s.Categoria)
                                    .FirstOrDefaultAsync(p => p.ID == idParaSync);

                                if (prodSync != null)
                                {
                                    var srv = new OrySiPOS.Services.SupabaseService();
                                    await srv.SincronizarProducto(prodSync);
                                }
                            }
                        }
                        catch { /* Log error silencioso */ }
                    });
                }

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }


    }
}