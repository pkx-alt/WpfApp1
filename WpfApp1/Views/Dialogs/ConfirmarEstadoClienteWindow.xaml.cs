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

            // Personalizamos el mensaje según el estado ACTUAL del cliente
            if (_cliente.Activo)
            {
                lblMensaje.Text = $"¿Estás seguro de que deseas DESACTIVAR al cliente '{_cliente.RazonSocial}'?";
            }
            else
            {
                lblMensaje.Text = $"¿Estás seguro de que deseas ACTIVAR al cliente '{_cliente.RazonSocial}'?";
            }
        }

        private void btnConfirmar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var db = new InventarioDbContext())
                {
                    // 1. Buscamos al cliente en la BD
                    var clienteEnDb = db.Clientes.Find(_cliente.ID);
                    if (clienteEnDb != null)
                    {
                        // 2. ¡Invertimos el estado!
                        clienteEnDb.Activo = !clienteEnDb.Activo;

                        // 3. Guardamos los cambios
                        db.SaveChanges();
                    }
                }

                // 4. Avisamos a la página anterior que SÍ se hizo el cambio
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al actualizar el estado: " + ex.Message);
                this.DialogResult = false;
                this.Close();
            }
        }
    }
}
