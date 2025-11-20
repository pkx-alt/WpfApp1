using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfApp1.Data;
using WpfApp1.Models;
using WpfApp1.ViewModels;
using WpfApp1.Views.Dialogs;

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

        private void VerDetalles_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var producto = menuItem?.DataContext as Producto;
            if (producto == null) return;

            MessageBox.Show($"Detalles: {producto.Descripcion} (Próximamente)");
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
    }
}