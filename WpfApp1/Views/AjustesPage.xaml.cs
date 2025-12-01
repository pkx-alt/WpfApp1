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
    }
}