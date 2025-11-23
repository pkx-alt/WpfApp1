using System.Windows;
using System.Windows.Input; // <--- NECESARIO
using WpfApp1.Data;
using WpfApp1.Models;

namespace WpfApp1.Views.Dialogs
{
    public partial class NuevoClienteWindow : Window
    {
        public NuevoClienteWindow()
        {
            InitializeComponent();
        }

        // --- AGREGA ESTO ---
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
        // -------------------

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // 1. Validar
            if (string.IsNullOrWhiteSpace(txtRfc.Text) || string.IsNullOrWhiteSpace(txtRazonSocial.Text))
            {
                MessageBox.Show("RFC y Razón Social son obligatorios.", "Faltan datos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Crear objeto
            var nuevoCliente = new Cliente
            {
                RFC = txtRfc.Text.Trim(),
                RazonSocial = txtRazonSocial.Text.Trim(),
                Telefono = txtTelefono.Text.Trim(),
                CodigoPostal = txtCodigoPostal.Text.Trim(),
                RegimenFiscal = txtRegimenFiscal.Text.Trim(),
                UsoCFDI = txtUsoCFDI.Text.Trim(),
                Activo = true
            };

            try
            {
                // 3. Guardar
                using (var db = new InventarioDbContext())
                {
                    db.Clientes.Add(nuevoCliente);
                    db.SaveChanges();
                }

                MessageBox.Show("¡Cliente registrado!", "Éxito");
                this.DialogResult = true;
                this.Close();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Error al guardar: " + ex.Message, "Error");
            }
        }

        // Agregamos evento para el botón Cancelar
        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}