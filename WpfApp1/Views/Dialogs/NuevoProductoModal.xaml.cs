// WpfApp1/Views/Dialogs/NuevoProductoModal.xaml.cs - MODIFICADO
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfApp1.Data;
using WpfApp1.Models;
using Microsoft.EntityFrameworkCore;

namespace WpfApp1.Views.Dialogs
{
    public partial class NuevoProductoModal : Window
    {
        private List<Categoria> _categoriasCargadas;
        public Producto ProductoRegistrado { get; private set; }

        public NuevoProductoModal()
        {
            InitializeComponent();
            CargarCombos();
        }

        private void CargarCombos()
        {
            using (var db = new InventarioDbContext())
            {
                _categoriasCargadas = db.Categorias
                                        .Include(c => c.Subcategorias)
                                        .ToList();

                CmbCategoria.ItemsSource = _categoriasCargadas;

                var catGenerica = _categoriasCargadas.FirstOrDefault(c => c.Nombre == "Genérica");

                if (catGenerica != null)
                {
                    CmbCategoria.SelectedItem = catGenerica;
                    CmbSubcategoria.SelectedItem = catGenerica.Subcategorias.FirstOrDefault();
                }
            }
        }

        private void CmbCategoria_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbCategoria.SelectedItem is Categoria categoriaSeleccionada)
            {
                CmbSubcategoria.ItemsSource = categoriaSeleccionada.Subcategorias;
                CmbSubcategoria.IsEnabled = true;
            }
            else
            {
                CmbSubcategoria.ItemsSource = null;
                CmbSubcategoria.IsEnabled = false;
            }
        }

        private void Guardar_Click(object sender, RoutedEventArgs e)
        {
            // --- 1. Validaciones OBLIGATORIAS ---
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

            if (!decimal.TryParse(txtPrecio.Text, out decimal precio))
            {
                MessageBox.Show("El precio debe ser un número válido.", "Dato incorrecto", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPrecio.SelectAll();
                txtPrecio.Focus();
                return;
            }

            // --- 2. Manejo de Campos OPCIONALES/DEFAULT ---
            decimal costo = 0;
            if (!string.IsNullOrWhiteSpace(txtCosto.Text))
            {
                if (!decimal.TryParse(txtCosto.Text, out costo))
                {
                    MessageBox.Show("El costo ingresado no es válido (déjalo vacío si no lo sabes).");
                    return;
                }
            }

            int stock = 1;
            if (!string.IsNullOrWhiteSpace(txtStock.Text))
            {
                if (!int.TryParse(txtStock.Text, out stock))
                {
                    MessageBox.Show("El stock debe ser un número entero.");
                    return;
                }
            }

            string urlImagen = string.IsNullOrWhiteSpace(txtImagenUrl.Text)
                               ? "https://via.placeholder.com/150?text=Sin+Imagen"
                               : txtImagenUrl.Text;

            // ¡NUEVOS CAMPOS CFDI! Asignamos valores por defecto si están vacíos
            string claveSat = string.IsNullOrWhiteSpace(txtClaveSat.Text) ? "01010101" : txtClaveSat.Text.Trim();
            string claveUnidad = string.IsNullOrWhiteSpace(txtClaveUnidad.Text) ? "H87" : txtClaveUnidad.Text.Trim();
            // FIN NUEVOS CAMPOS


            // --- 3. Crear el Objeto ---
            var subcategoriaSeleccionada = (Subcategoria)CmbSubcategoria.SelectedItem;

            ProductoRegistrado = new Producto
            {
                SubcategoriaId = subcategoriaSeleccionada.Id,
                Descripcion = txtDescripcion.Text,
                Precio = precio,

                Costo = costo,
                Stock = stock,
                ImagenUrl = urlImagen,
                Activo = true,

                // ¡Asignación de nuevos campos!
                ClaveSat = claveSat,
                ClaveUnidad = claveUnidad
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