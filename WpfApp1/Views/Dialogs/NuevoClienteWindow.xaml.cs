// WpfApp1/Views/Dialogs/NuevoClienteWindow.xaml.cs - MODIFICADO
using System;
using System.Windows;
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
                RFC = txtRfc.Text.Trim(),
                RazonSocial = txtRazonSocial.Text.Trim(),
                Telefono = txtTelefono.Text.Trim(),

                // ¡AÑADIDOS NUEVOS CAMPOS CFDI!
                CodigoPostal = txtCodigoPostal.Text.Trim(),
                RegimenFiscal = txtRegimenFiscal.Text.Trim(),
                UsoCFDI = txtUsoCFDI.Text.Trim(),

                Activo = true // Por defecto
            };

            try
            {
                // 3. Guardar en la BD
                using (var db = new InventarioDbContext())
                {
                    db.Clientes.Add(nuevoCliente);
                    db.SaveChanges();
                }

                // 4. Avisar y cerrar
                MessageBox.Show("¡Cliente guardado con éxito!", "Éxito");
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar el cliente: " + ex.Message, "Error");
            }
        }
    }
}