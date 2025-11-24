using System.Windows;
using System.Windows.Input; // <--- IMPORTANTE
using System.Windows.Media;
using WpfApp1.Data;
using WpfApp1.Models;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace WpfApp1.Views.Dialogs
{
    public partial class ConfirmarEstadoModal : Window
    {
        private Producto _producto;
        private bool _deshabilitando;

        public ConfirmarEstadoModal(Producto producto, bool esParaDeshabilitar)
        {
            InitializeComponent();
            _producto = producto;
            _deshabilitando = esParaDeshabilitar;
            this.DataContext = _producto;
            ConfigurarVentana();
        }

        // --- NUEVO: ARRASTRAR ---
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
        // ------------------------

        private void ConfigurarVentana()
        {
            if (_deshabilitando)
            {
                TituloTextBlock.Text = "Deshabilitar Producto";
                TituloTextBlock.Foreground = (Brush)Application.Current.Resources["DangerColor"];

                BotonConfirmar.Content = "Deshabilitar";
                BotonConfirmar.Style = (Style)Application.Current.Resources["BtnDanger"]; // Usamos estilo Danger

                PanelInfo.Visibility = Visibility.Visible; // Mostramos las advertencias
            }
            else
            {
                TituloTextBlock.Text = "Reactivar Producto";
                TituloTextBlock.Foreground = (Brush)Application.Current.Resources["SuccessColor"];

                BotonConfirmar.Content = "Reactivar";
                BotonConfirmar.Style = (Style)Application.Current.Resources["BtnSuccess"]; // Usamos estilo Success

                PanelInfo.Visibility = Visibility.Collapsed; // Ocultamos advertencias para reactivar
            }
        }

        private void Confirmar_Click(object sender, RoutedEventArgs e)
        {
            // El producto viene "enlazado" en memoria, pero para estar seguros y no tener problemas de hilos,
            // solo usamos su ID. La actualización real se suele hacer en la vista padre (InventarioPage),
            // pero aquí podemos forzar la sincronización.

            // Como este modal devuelve "true", la vista padre (InventarioPage) es quien hace el cambio en DB local.
            // Así que haremos un truco: Haremos el cambio AQUÍ mismo para garantizar la sincronización.

            // NOTA: Tu código original en InventarioPage hacía la lógica.
            // Vamos a cambiarlo para que ESTA ventana haga el trabajo sucio y la sincronización.

            // 1. PROCESO LOCAL
            try
            {
                int idProducto = _producto.ID;

                using (var db = new InventarioDbContext())
                {
                    var prodDb = db.Productos.Find(idProducto);
                    if (prodDb != null)
                    {
                        // Invertimos el estado (si estaba activo, se desactiva y viceversa)
                        prodDb.Activo = !prodDb.Activo;
                        db.SaveChanges();

                        // Actualizamos el objeto visual para que la tabla se refresque al volver
                        _producto.Activo = prodDb.Activo;
                    }
                }

                // 2. PROCESO NUBE (Background)
                Task.Run(async () =>
                {
                    try
                    {
                        using (var dbSync = new InventarioDbContext())
                        {
                            var prod = await dbSync.Productos
                                .Include(p => p.Subcategoria).ThenInclude(s => s.Categoria)
                                .FirstOrDefaultAsync(p => p.ID == idProducto);

                            if (prod != null)
                            {
                                var srv = new WpfApp1.Services.SupabaseService();
                                await srv.SincronizarProducto(prod);
                            }
                        }
                    }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Error sync estado: " + ex.Message); }
                });

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cambiar estado: " + ex.Message);
            }
        }
        private void Cancelar_Click(object sender, RoutedEventArgs e) => this.DialogResult = false;
    }
}