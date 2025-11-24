using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WpfApp1.Data;
using WpfApp1.Models;

namespace WpfApp1.Views.Dialogs
{
    /// <summary>
    /// Lógica de interacción para ConfirmarEstadoClienteWindow.xaml
    /// </summary>
    public partial class ConfirmarEstadoClienteWindow : Window
    {
        private Cliente _cliente; // Para guardar el cliente que recibamos

        // ¡Este es un constructor "personalizado"!
        public ConfirmarEstadoClienteWindow(Cliente cliente)
        {
            InitializeComponent();
            _cliente = cliente;
            ConfigurarMensaje();
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
        // --- NUEVO: PERMITIR ARRASTRAR VENTANA ---
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
        // -----------------------------------------

        private void btnConfirmar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int idCliente = _cliente.ID;
                bool nuevoEstado = !_cliente.Activo;

                // 1. CAMBIO LOCAL
                using (var db = new InventarioDbContext())
                {
                    var clienteEnDb = db.Clientes.Find(idCliente);
                    if (clienteEnDb != null)
                    {
                        clienteEnDb.Activo = nuevoEstado;
                        db.SaveChanges();

                        // Actualizamos el objeto en memoria para la UI
                        _cliente.Activo = nuevoEstado;
                    }
                }

                // 2. SYNC NUBE
                Task.Run(async () =>
                {
                    try
                    {
                        // Obtenemos una copia fresca para enviar
                        using (var dbSync = new InventarioDbContext())
                        {
                            var clienteParaNube = dbSync.Clientes.Find(idCliente);
                            if (clienteParaNube != null)
                            {
                                var srv = new WpfApp1.Services.SupabaseService();
                                await srv.SincronizarCliente(clienteParaNube);
                            }
                        }
                    }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Error sync estado cliente: " + ex.Message); }
                });

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
                this.DialogResult = false;
                this.Close();
            }
        }

        private void ConfigurarMensaje()
        {
            // Usamos Run para formatear partes del texto en negrita dentro del mismo TextBlock
            TxtMensaje.Inlines.Clear();
            TxtMensaje.Inlines.Add("¿Estás seguro de que deseas ");

            if (_cliente.Activo)
            {
                TxtTitulo.Text = "Desactivar Cliente";
                IconoEstado.Text = "⚠️"; // Icono de alerta

                var runAccion = new System.Windows.Documents.Run("DESACTIVAR") { FontWeight = FontWeights.Bold, Foreground = (System.Windows.Media.Brush)Application.Current.Resources["DangerColor"] };
                TxtMensaje.Inlines.Add(runAccion);
            }
            else
            {
                TxtTitulo.Text = "Reactivar Cliente";
                IconoEstado.Text = "✅"; // Icono de check

                var runAccion = new System.Windows.Documents.Run("ACTIVAR") { FontWeight = FontWeights.Bold, Foreground = (System.Windows.Media.Brush)Application.Current.Resources["SuccessColor"] };
                TxtMensaje.Inlines.Add(runAccion);
            }

            TxtMensaje.Inlines.Add($" al cliente ");
            TxtMensaje.Inlines.Add(new System.Windows.Documents.Run($"'{_cliente.RazonSocial}'") { FontWeight = FontWeights.Bold });
            TxtMensaje.Inlines.Add("?");
        }


    }
}
