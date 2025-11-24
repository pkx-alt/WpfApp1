using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input; // <--- NECESARIO
using WpfApp1.Data;
using WpfApp1.Models;
using Microsoft.EntityFrameworkCore;

namespace WpfApp1.Views.Dialogs
{
    public partial class NuevoProductoModal : Window
    {
        public Producto ProductoRegistrado { get; private set; }

        public NuevoProductoModal()
        {
            InitializeComponent();
            CargarCombos();
        }

        // --- AGREGA ESTO ---
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
        // -------------------

        private void CargarCombos()
        {
            using (var db = new InventarioDbContext())
            {
                var categorias = db.Categorias.Include(c => c.Subcategorias).ToList();
                CmbCategoria.ItemsSource = categorias;

                // Seleccionar Genérica si existe
                var generica = categorias.FirstOrDefault(c => c.Nombre == "Genérica");
                if (generica != null)
                {
                    CmbCategoria.SelectedItem = generica;
                    CmbSubcategoria.SelectedItem = generica.Subcategorias.FirstOrDefault();
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
            // --- TUS VALIDACIONES ORIGINALES ---
            if (string.IsNullOrWhiteSpace(txtDescripcion.Text)) { MessageBox.Show("Falta descripción"); return; }
            if (!decimal.TryParse(txtPrecio.Text, out decimal precio)) { MessageBox.Show("Precio inválido"); return; }

            decimal.TryParse(txtCosto.Text, out decimal costo);
            int.TryParse(txtStock.Text, out int stock);
            string img = string.IsNullOrWhiteSpace(txtImagenUrl.Text) ? "https://via.placeholder.com/150" : txtImagenUrl.Text;
            string claveSat = string.IsNullOrWhiteSpace(txtClaveSat.Text) ? "01010101" : txtClaveSat.Text;
            string claveUnidad = string.IsNullOrWhiteSpace(txtClaveUnidad.Text) ? "H87" : txtClaveUnidad.Text;

            var subcat = CmbSubcategoria.SelectedItem as Subcategoria;
            if (subcat == null) { MessageBox.Show("Selecciona subcategoría"); return; }

            ProductoRegistrado = new Producto
            {
                Descripcion = txtDescripcion.Text,
                Precio = precio,
                Costo = costo,
                Stock = stock,
                ImagenUrl = img,
                SubcategoriaId = subcat.Id,
                Activo = true,
                ClaveSat = claveSat,
                ClaveUnidad = claveUnidad
            };

            try
            {
                int nuevoId = 0;

                // 1. GUARDADO LOCAL (Rápido)
                using (var db = new InventarioDbContext())
                {
                    db.Productos.Add(ProductoRegistrado);
                    db.SaveChanges();
                    nuevoId = ProductoRegistrado.ID; // Obtenemos el ID generado
                }

                // 2. SINCRONIZACIÓN EN SEGUNDO PLANO (Fire and Forget)
                if (nuevoId > 0)
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            using (var dbSync = new InventarioDbContext())
                            {
                                // Volvemos a cargar el producto con sus relaciones (Categoría) para enviarlo completo
                                var prodParaNube = await dbSync.Productos
                                    .Include(p => p.Subcategoria)
                                    .ThenInclude(s => s.Categoria)
                                    .FirstOrDefaultAsync(p => p.ID == nuevoId);

                                if (prodParaNube != null)
                                {
                                    var srv = new WpfApp1.Services.SupabaseService();
                                    await srv.SincronizarProducto(prodParaNube);
                                    System.Diagnostics.Debug.WriteLine($"Producto nuevo sincronizado: {prodParaNube.Descripcion}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine("Error sync nuevo producto: " + ex.Message);
                        }
                    });
                }

                // 3. CERRAR DE INMEDIATO
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