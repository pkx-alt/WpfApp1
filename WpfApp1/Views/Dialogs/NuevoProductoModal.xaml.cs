using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OrySiPOS.Data;
using OrySiPOS.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks; // Para el Sync

namespace OrySiPOS.Views.Dialogs
{
    public partial class NuevoProductoModal : Window
    {
        public Producto ProductoRegistrado { get; private set; }
        private Producto _productoEdicion; // Guardamos el producto si es edición

        // Constructor modificado: acepta producto opcional
        public NuevoProductoModal(Producto productoParaEditar = null)
        {
            InitializeComponent();
            CargarCombos();

            if (productoParaEditar != null)
            {
                // --- MODO EDICIÓN ---
                _productoEdicion = productoParaEditar;
                Title = "Editar Producto";
                btnGuardar.Content = "Actualizar";

                // ¡TRUCO DE SÉNIOR! Bloqueamos el stock
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
            }
        }

        // Método para arrastrar la ventana (ya lo tienes, déjalo igual)
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
        }

        private void CargarCombos()
        {
            using (var db = new InventarioDbContext())
            {
                // Cargamos categorías para el combo
                var categorias = db.Categorias.Include(c => c.Subcategorias).ToList();
                CmbCategoria.ItemsSource = categorias;
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
            txtClaveSat.Text = _productoEdicion.ClaveSat;
            txtClaveUnidad.Text = _productoEdicion.ClaveUnidad;

            // 2. Seleccionar Categoría y Subcategoría (Un poco más complejo visualmente)
            // Necesitamos saber la Subcategoría actual para seleccionar los combos correctos
            if (_productoEdicion.Subcategoria != null)
            {
                // Buscamos en el Combo de Categorías aquella que coincida con la del producto
                foreach (Categoria cat in CmbCategoria.ItemsSource)
                {
                    if (cat.Id == _productoEdicion.Subcategoria.CategoriaId)
                    {
                        CmbCategoria.SelectedItem = cat;

                        // Forzamos la actualización de las subcategorías
                        CmbSubcategoria.ItemsSource = cat.Subcategorias;

                        // Ahora buscamos la subcategoría específica
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

            // Usamos CultureInfo.InvariantCulture para asegurar que el punto (.) se lea bien
            foreach (ComboBoxItem item in CmbIVA.Items)
            {
                if (decimal.TryParse(item.Tag.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal valorTag))
                {
                    // Usamos una tolerancia pequeña para comparar decimales
                    if (Math.Abs(valorTag - _productoEdicion.PorcentajeIVA) < 0.001m)
                    {
                        CmbIVA.SelectedItem = item;
                        break;
                    }
                }
            }
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

        private void Guardar_Click(object sender, RoutedEventArgs e)
        {
            // Validaciones básicas
            if (string.IsNullOrWhiteSpace(txtDescripcion.Text)) { MessageBox.Show("Falta descripción"); return; }
            if (!decimal.TryParse(txtPrecio.Text, out decimal precio)) { MessageBox.Show("Precio inválido"); return; }
            decimal.TryParse(txtCosto.Text, out decimal costo);
            // El stock lo leemos, pero en edición no cambiará porque está deshabilitado visualmente (o el usuario no debería poder tocarlo)
            int.TryParse(txtStock.Text, out int stock);

            string img = string.IsNullOrWhiteSpace(txtImagenUrl.Text) ? "https://via.placeholder.com/150" : txtImagenUrl.Text;
            string claveSat = string.IsNullOrWhiteSpace(txtClaveSat.Text) ? "01010101" : txtClaveSat.Text;
            string claveUnidad = string.IsNullOrWhiteSpace(txtClaveUnidad.Text) ? "H87" : txtClaveUnidad.Text;

            // 1. LEER EL IVA SELECCIONADO
            decimal ivaSeleccionado = 0.16m; // Valor por defecto si falla
            if (CmbIVA.SelectedItem is ComboBoxItem item)
            {
                // Forzamos la lectura con punto decimal (InvariantCulture)
                decimal.TryParse(item.Tag.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out ivaSeleccionado);
            }

            var subcat = CmbSubcategoria.SelectedItem as Subcategoria;
            if (subcat == null) { MessageBox.Show("Selecciona subcategoría"); return; }

            try
            {
                int idParaSync = 0;

                using (var db = new InventarioDbContext())
                {
                    if (_productoEdicion == null)
                    {
                        // --- CREAR NUEVO ---
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
                            ClaveSat = claveSat,
                            ClaveUnidad = claveUnidad
                        };
                        db.Productos.Add(ProductoRegistrado);
                        db.SaveChanges();
                        idParaSync = ProductoRegistrado.ID;
                    }
                    else
                    {
                        // --- ACTUALIZAR EXISTENTE ---
                        var prodDb = db.Productos.Find(_productoEdicion.ID);
                        if (prodDb != null)
                        {
                            prodDb.Descripcion = txtDescripcion.Text;
                            prodDb.Precio = precio;
                            prodDb.Costo = costo;
                            // ¡OJO! NO actualizamos prodDb.Stock aquí.
                            // Solo atributos descriptivos.
                            prodDb.PorcentajeIVA = ivaSeleccionado;
                            prodDb.ImagenUrl = img;
                            prodDb.SubcategoriaId = subcat.Id;
                            prodDb.ClaveSat = claveSat;
                            prodDb.ClaveUnidad = claveUnidad;

                            db.Productos.Update(prodDb);
                            db.SaveChanges();
                            idParaSync = prodDb.ID;
                        }
                    }
                }

                // Sincronización en segundo plano (Reutilizable para ambos casos)
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
                        catch { /* Log error */ }
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