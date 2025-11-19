using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using WpfApp1.Data;
using WpfApp1.Models;
using WpfApp1.Views.Dialogs;
using System.Windows.Media;
using System.Windows;
using WpfApp1.ViewModels; // ¡IMPORTANTE!

namespace WpfApp1.Views
{
    public partial class InventarioPage : Page
    {
        // ¡Ya no necesitamos la propiedad Productos!
        // ¡Ya no necesitamos la constante NivelBajoStock!

        // Solo dejamos el placeholder si quieres manejarlo desde aquí
        private const string PlaceholderText = "Buscar por nombre o descripción...";

        public InventarioPage()
        {
            InitializeComponent();

            // --- ¡EL GRAN CAMBIO! ---
            // Ya no es DataContext = this
            this.DataContext = new InventarioViewModel();

            SetupPlaceholder(); // Mantenemos la lógica del placeholder
        }

        // --- ¡TODA LA LÓGICA DE CARGA Y FILTROS SE FUE AL VIEWMODEL! ---
        // private void CargarProductos() { ... } // ¡ELIMINADO!
        // private void Filtro_Actualizado(object sender, ...) { ... } // ¡ELIMINADO!
        // private void LimpiarFiltrosButton_Click(object sender, ...) { ... } // ¡ELIMINADO!
        // private void NuevoProducto_Click(object sender, ...) { ... } // ¡ELIMINADO!

        // --- LÓGICA DEL PLACEHOLDER (SE QUEDA) ---
        private void SetupPlaceholder()
        {
            // (Tu código de SetupPlaceholder... se queda igual)
            SearchTextBox.Text = PlaceholderText;
            SearchTextBox.Foreground = Brushes.Gray;
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            // (Tu código de GotFocus... se queda igual)
            if (SearchTextBox.Text == PlaceholderText)
            {
                SearchTextBox.Text = "";
                SearchTextBox.Foreground = Brushes.Black;
            }
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // (Tu código de LostFocus... se queda igual)
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                SearchTextBox.Text = PlaceholderText;
                SearchTextBox.Foreground = Brushes.Gray;
            }
        }


        // --- LÓGICA DE CLICS DEL DATAGRID (SE QUEDA... POR AHORA) ---
        // La única diferencia es que ahora llamamos al CargarProductos()
        // PÚBLICO de nuestro ViewModel.

        // Obtenemos el ViewModel
        private InventarioViewModel VM => (InventarioViewModel)DataContext;

        private void AddStock_Click(object sender, RoutedEventArgs e)
        {
            var boton = sender as FrameworkElement;
            if (boton == null) return;
            var productoSeleccionado = boton.DataContext as Producto;
            if (productoSeleccionado == null) return;

            var modal = new AgregarStockModal(productoSeleccionado);
            bool? resultado = modal.ShowDialog();
            if (resultado == true)
            {
                VM.CargarProductos(); // ¡Llama al método del VM!
            }
        }

        private void RemoveStock_Click(object sender, RoutedEventArgs e)
        {
            var boton = sender as FrameworkElement;
            if (boton == null) return;

            // 1. Identificamos qué producto se seleccionó
            var productoSeleccionado = boton.DataContext as Producto;
            if (productoSeleccionado == null) return;

            // 2. Creamos la ventana de DISMINUIR (ya la tienes lista)
            var modal = new DisminuirStockModal(productoSeleccionado);

            // Opcional: Que aparezca centrada sobre la ventana principal
            modal.Owner = Window.GetWindow(this);

            // 3. La mostramos y esperamos
            bool? resultado = modal.ShowDialog();

            // 4. Si guardó cambios (resultado == true), refrescamos la tabla
            if (resultado == true)
            {
                VM.CargarProductos();
            }
        }

        private void OpcionesButton_Click(object sender, RoutedEventArgs e)
        {
            // (Tu lógica de OpcionesButton_Click... se queda igual)
            var boton = sender as Button;
            if (boton == null || boton.ContextMenu == null) return;
            boton.ContextMenu.DataContext = boton.DataContext;
            boton.ContextMenu.PlacementTarget = boton;
            boton.ContextMenu.IsOpen = true;
        }

        private void EditarProducto_Click(object sender, RoutedEventArgs e)
        {
            // (Tu lógica de EditarProducto_Click... se queda igual)
            var menuItem = sender as MenuItem;
            if (menuItem == null) return;
            var producto = menuItem.DataContext as Producto;
            if (producto == null) return;
            MessageBox.Show($"Vas a EDITAR: {producto.Descripcion}");
            // Si editas, al final llama a VM.CargarProductos();
        }

        private void VerDetalles_Click(object sender, RoutedEventArgs e)
        {
            // (Tu lógica... se queda igual)
            var menuItem = sender as MenuItem;
            if (menuItem == null) return;
            var producto = menuItem.DataContext as Producto;
            if (producto == null) return;
            MessageBox.Show($"Vas a VER DETALLES de: {producto.Descripcion}");
        }

        private void Duplicar_Click(object sender, RoutedEventArgs e)
        {
            // (Tu lógica... se queda igual)
            var menuItem = sender as MenuItem;
            if (menuItem == null) return;
            var producto = menuItem.DataContext as Producto;
            if (producto == null) return;
            MessageBox.Show($"Vas a DUPLICAR: {producto.Descripcion}");
        }

        private void Historial_Click(object sender, RoutedEventArgs e)
        {
            // (Tu lógica... se queda igual)
            var menuItem = sender as MenuItem;
            if (menuItem == null) return;
            var producto = menuItem.DataContext as Producto;
            if (producto == null) return;
            MessageBox.Show($"Vas a VER HISTORIAL de: {producto.Descripcion}");
        }

        private void Deshabilitar_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem == null) return;
            var producto = menuItem.DataContext as Producto;
            if (producto == null) return;

            bool esParaDeshabilitar = producto.Activo;
            var modalConfirmar = new ConfirmarEstadoModal(producto, esParaDeshabilitar);
            bool? resultado = modalConfirmar.ShowDialog();

            if (resultado == true)
            {
                producto.Activo = !producto.Activo;
                try
                {
                    using (var db = new InventarioDbContext())
                    {
                        db.Productos.Update(producto);
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al actualizar: {ex.Message}");
                    producto.Activo = !producto.Activo; // Revertimos
                    return;
                }

                VM.CargarProductos(); // ¡Llama al método del VM!
            }
        }
    }
}