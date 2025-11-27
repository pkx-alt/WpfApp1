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

namespace OrySiPOS.Views.Dialogs
{
    public partial class NuevoProductoModal : Window
    {
        public Producto ProductoRegistrado { get; private set; }
        private Producto _productoEdicion; // Guardamos el producto si es edición

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

        private void CargarCatalogosSat()
        {
            using (var db = new InventarioDbContext())
            {
                // Traemos los catálogos de la BD a la memoria
                // (Como son pocos registros, es rápido)
                _listaSatProductos = db.SatProductos.OrderBy(x => x.Descripcion).ToList();
                _listaSatUnidades = db.SatUnidades.OrderBy(x => x.Descripcion).ToList();
            }

            // Creamos las vistas filtrables
            _vistaClavesSat = CollectionViewSource.GetDefaultView(_listaSatProductos);
            _vistaUnidades = CollectionViewSource.GetDefaultView(_listaSatUnidades);

            // Asignamos al XAML
            CmbClaveSat.ItemsSource = _vistaClavesSat;
            CmbClaveUnidad.ItemsSource = _vistaUnidades;
        }

        private void CargarDatosEnFormulario()
        {
            // 1. Datos simples
            txtDescripcion.Text = _productoEdicion.Descripcion;
            txtPrecio.Text = _productoEdicion.Precio.ToString();
            txtCosto.Text = _productoEdicion.Costo.ToString();
            txtStock.Text = _productoEdicion.Stock.ToString();
            txtImagenUrl.Text = _productoEdicion.ImagenUrl;

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
            FiltarCombos(CmbClaveSat, _vistaClavesSat);
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
                            Precio = precio,
                            Costo = costo,
                            Stock = stock,
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