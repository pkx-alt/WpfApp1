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
    /// Lógica de interacción para NuevoClienteWindow.xaml
    /// </summary>
    public partial class NuevoClienteWindow : Window
    {
        public NuevoClienteWindow()
        {
            InitializeComponent();
        }
        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // 1. Validar (básico)
            if (string.IsNullOrWhiteSpace(txtRfc.Text) ||
                string.IsNullOrWhiteSpace(txtRazonSocial.Text))
            {
                MessageBox.Show("El RFC y la Razón Social son obligatorios.", "Error");
                return;
            }

            // 2. Crear el objeto Cliente
            var nuevoCliente = new Cliente
            {
                RFC = txtRfc.Text,
                RazonSocial = txtRazonSocial.Text,
                Telefono = txtTelefono.Text
                // La fecha 'Creado' se pone sola por el valor por defecto
            };

            try
            {
                // 3. Guardar en la BD
                // El 'using' se encarga de abrir y cerrar la conexión
                using (var db = new InventarioDbContext())
                {
                    db.Clientes.Add(nuevoCliente);
                    db.SaveChanges();
                }

                // 4. Avisar y cerrar
                MessageBox.Show("¡Cliente guardado con éxito!", "Éxito");
                this.DialogResult = true; // Esto le dice a la página anterior que "todo salió bien"
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar el cliente: " + ex.Message, "Error");
            }
        }
    }
}
