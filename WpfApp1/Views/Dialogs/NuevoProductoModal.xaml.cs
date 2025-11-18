using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfApp1.Data;
using WpfApp1.Models;
using Microsoft.EntityFrameworkCore; // ¡¡AÑADE ESTE USING!!

namespace WpfApp1.Views.Dialogs
{
    public partial class NuevoProductoModal : Window
    {
        // Guardamos las categorías aquí para la lógica en cascada
        private List<Categoria> _categoriasCargadas;
        public Producto ProductoRegistrado { get; private set; }

        public NuevoProductoModal()
        {
            InitializeComponent();

            // ¡NUEVO! Cargamos los combos al abrir la ventana
            CargarCombos();
        }

        // --- ¡NUEVO MÉTODO! ---
        // Carga todas las categorías (y sus hijas) de la BD

        private void CargarCombos()
        {
            using (var db = new InventarioDbContext())
            {
                _categoriasCargadas = db.Categorias
                                        .Include(c => c.Subcategorias)
                                        .ToList();

                CmbCategoria.ItemsSource = _categoriasCargadas;

                // --- ¡AQUÍ ESTÁ LA MAGIA! ---

                // 1. Buscamos la categoría "Genérica" que acabamos de sembrar
                var catGenerica = _categoriasCargadas.FirstOrDefault(c => c.Nombre == "Genérica");

                if (catGenerica != null)
                {
                    // 2. La ponemos como seleccionada POR DEFECTO
                    CmbCategoria.SelectedItem = catGenerica;

                    // 3. (Esto disparará el evento 'CmbCategoria_SelectionChanged'...)
                    //    (...que cargará "General" en el combo de subcategorías)

                    // 4. Seleccionamos "General" por defecto
                    CmbSubcategoria.SelectedItem = catGenerica.Subcategorias.FirstOrDefault();
                }
            }
        }

        // --- ¡NUEVO EVENTO! ---
        // Se dispara cuando el usuario cambia la Categoría
        private void CmbCategoria_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbCategoria.SelectedItem is Categoria categoriaSeleccionada)
            {
                // Llenamos el segundo combo CON LAS HIJAS de la categoría
                CmbSubcategoria.ItemsSource = categoriaSeleccionada.Subcategorias;
                CmbSubcategoria.IsEnabled = true; // Lo habilitamos
            }
            else
            {
                // Si no hay nada seleccionado, limpiamos y deshabilitamos
                CmbSubcategoria.ItemsSource = null;
                CmbSubcategoria.IsEnabled = false;
            }
        }

        private void Guardar_Click(object sender, RoutedEventArgs e)
        {
            // --- 1. Validaciones OBLIGATORIAS (Solo lo indispensable) ---

            if (CmbSubcategoria.SelectedItem == null)
            {
                MessageBox.Show("Selecciona una categoría.", "Falta dato", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtDescripcion.Text))
            {
                MessageBox.Show("Escribe una descripción.", "Falta dato", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtDescripcion.Focus();
                return;
            }

            // El Precio SÍ lo exigimos, porque si no, ¿cuánto vas a cobrar?
            if (!decimal.TryParse(txtPrecio.Text, out decimal precio))
            {
                MessageBox.Show("El precio debe ser un número válido.", "Dato incorrecto", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPrecio.SelectAll();
                txtPrecio.Focus();
                return;
            }

            // --- 2. Manejo de Campos OPCIONALES (Lógica "Fail-Safe") ---

            // A. COSTO: Si está vacío o mal escrito, asumimos 0.
            decimal costo = 0;
            if (!string.IsNullOrWhiteSpace(txtCosto.Text))
            {
                // Solo si escribió algo, intentamos validarlo. Si falla, avisamos.
                if (!decimal.TryParse(txtCosto.Text, out costo))
                {
                    MessageBox.Show("El costo ingresado no es válido (déjalo vacío si no lo sabes).");
                    return;
                }
            }

            // B. STOCK: Si está vacío, asumimos 1.
            // ¿Por qué 1? Porque si lo pones en 0, tu VentaViewModel lanzará error de "Stock Insuficiente"
            // al intentar agregarlo al carrito. Como lo tienes en la mano, asumes que existe 1.
            int stock = 1;
            if (!string.IsNullOrWhiteSpace(txtStock.Text))
            {
                if (!int.TryParse(txtStock.Text, out stock))
                {
                    MessageBox.Show("El stock debe ser un número entero.");
                    return;
                }
            }

            // C. IMAGEN: Si está vacía, usamos un placeholder genérico.
            string urlImagen = string.IsNullOrWhiteSpace(txtImagenUrl.Text)
                               ? "https://via.placeholder.com/150?text=Sin+Imagen" // Imagen por defecto
                               : txtImagenUrl.Text;


            // --- 3. Crear el Objeto ---

            var subcategoriaSeleccionada = (Subcategoria)CmbSubcategoria.SelectedItem;

            ProductoRegistrado = new Producto
            {
                SubcategoriaId = subcategoriaSeleccionada.Id,
                Descripcion = txtDescripcion.Text,
                Precio = precio,

                // Usamos nuestras variables "seguras"
                Costo = costo,
                Stock = stock,
                ImagenUrl = urlImagen,

                Activo = true
            };

            // --- 4. Guardar en BD ---
            try
            {
                using (var db = new InventarioDbContext())
                {
                    db.Productos.Add(ProductoRegistrado);
                    db.SaveChanges();
                }

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                var msj = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                MessageBox.Show("Error al guardar: " + msj, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}