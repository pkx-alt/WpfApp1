using System;
using System.Windows;
using System.Windows.Input;
using OrySiPOS.Data;
using OrySiPOS.Models;
using OrySiPOS.Helpers;
using System.Threading.Tasks;

namespace OrySiPOS.Views.Dialogs
{
    public partial class NuevoClienteWindow : Window
    {
        private Cliente _clienteEdicion;

        public NuevoClienteWindow(Cliente clienteAEditar = null)
        {
            InitializeComponent();

            // Cargar Catálogos
            CmbRegimen.ItemsSource = CatalogosSAT.Regimenes;
            CmbUsoCfdi.ItemsSource = CatalogosSAT.UsosCFDI;

            if (clienteAEditar != null)
            {
                _clienteEdicion = clienteAEditar;
                this.Title = "Editar Cliente";
                btnGuardar.Content = "Actualizar Cliente";
                CargarDatosEnFormulario();
            }
            else
            {
                _clienteEdicion = null;
                // Defaults para nuevo
                chkEsFactura.IsChecked = false;
                txtRfc.Text = "XAXX010101000";
                txtCodigoPostal.Text = "00000";
                CmbRegimen.SelectedValue = "616";
                CmbUsoCfdi.SelectedValue = "S01";

                // Aplicar bloqueo inicial
                ActualizarEstadoVisual();
            }
        }

        private void CargarDatosEnFormulario()
        {
            txtRazonSocial.Text = _clienteEdicion.RazonSocial;
            txtTelefono.Text = _clienteEdicion.Telefono;
            chkEsFactura.IsChecked = _clienteEdicion.EsFactura;
            txtRfc.Text = _clienteEdicion.RFC;
            txtCodigoPostal.Text = _clienteEdicion.CodigoPostal;
            CmbRegimen.SelectedValue = _clienteEdicion.RegimenFiscal;
            CmbUsoCfdi.SelectedValue = _clienteEdicion.UsoCFDI;

            // Aplicar bloqueo según lo cargado
            ActualizarEstadoVisual();
        }

        private void chkEsFactura_Click(object sender, RoutedEventArgs e)
        {
            // Cuando hacen clic, aplicamos la lógica Y sugerimos cambios
            bool quiereFactura = chkEsFactura.IsChecked == true;

            if (quiereFactura)
            {
                // Limpiar genéricos para obligar a capturar
                if (txtRfc.Text == "XAXX010101000") txtRfc.Text = "";
                if (txtCodigoPostal.Text == "00000") txtCodigoPostal.Text = "";

                // Sugerencia inteligente: Si es factura, NO debe ser 616. Sugerimos 605.
                if ((string)CmbRegimen.SelectedValue == "616") CmbRegimen.SelectedValue = "605";
                if ((string)CmbUsoCfdi.SelectedValue == "S01") CmbUsoCfdi.SelectedValue = "G03";

                txtRfc.Focus();
            }
            else
            {
                // Restaurar genéricos
                txtRfc.Text = "XAXX010101000";
                txtCodigoPostal.Text = "00000";
                CmbRegimen.SelectedValue = "616";
                CmbUsoCfdi.SelectedValue = "S01";
            }

            ActualizarEstadoVisual();
        }

        // --- MÉTODO MAESTRO DE ESTADO VISUAL ---
        private void ActualizarEstadoVisual()
        {
            bool requiereFactura = chkEsFactura.IsChecked == true;

            if (requiereFactura)
            {
                // CLIENTE REAL: Todo habilitado
                CmbRegimen.IsEnabled = true;
                CmbUsoCfdi.IsEnabled = true;
                txtRfc.IsEnabled = true;
                txtCodigoPostal.IsEnabled = true;
            }
            else
            {
                // PÚBLICO GENERAL: Todo bloqueado
                CmbRegimen.IsEnabled = false;
                CmbUsoCfdi.IsEnabled = false;
                txtRfc.IsEnabled = false;
                txtCodigoPostal.IsEnabled = false;
            }
        }

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // 1. Validaciones Básicas
            if (string.IsNullOrWhiteSpace(txtRazonSocial.Text))
            {
                MessageBox.Show("El nombre es obligatorio.", "Falta dato", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (CmbRegimen.SelectedItem == null || CmbUsoCfdi.SelectedItem == null)
            {
                MessageBox.Show("Selecciona Régimen y Uso válidos.", "Falta dato", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string regimen = CmbRegimen.SelectedValue.ToString();
            string uso = CmbUsoCfdi.SelectedValue.ToString();

            // 2. VALIDACIONES FISCALES (TU PETICIÓN)

            // REGLA A: Si pide factura (Cliente Real), NO puede ser 616
            if (chkEsFactura.IsChecked == true && regimen == "616")
            {
                MessageBox.Show(
                    "Un cliente que requiere factura NO puede tener el régimen '616 - Sin obligaciones fiscales'.\n\n" +
                    "Ese régimen es exclusivo para el público en general.\n" +
                    "Por favor selecciona el régimen real del cliente (ej. 605, 601, 626).",
                    "Error Fiscal", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // REGLA B: Público en General (Si por error lograran moverle) debe ser 616/S01
            if (chkEsFactura.IsChecked == false)
            {
                if (regimen != "616" || uso != "S01")
                {
                    // Autocorrección silenciosa por seguridad
                    regimen = "616";
                    uso = "S01";
                }
            }

            // REGLA C: Sueldos y Salarios (605) no deduce G01/G03
            if (regimen == "605" && (uso == "G01" || uso == "G03"))
            {
                MessageBox.Show(
                    "El régimen '605 - Sueldos y Salarios' NO puede usar G01 ni G03.\n" +
                    "Selecciona 'S01 - Sin efectos fiscales' para que el SAT acepte la factura.",
                    "Validación SAT", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 3. GUARDADO
            try
            {
                using (var db = new InventarioDbContext())
                {
                    if (_clienteEdicion == null)
                    {
                        var nuevoCliente = new Cliente
                        {
                            RazonSocial = txtRazonSocial.Text.Trim(),
                            Telefono = txtTelefono.Text.Trim(),
                            Activo = true,
                            EsFactura = chkEsFactura.IsChecked == true,
                            RFC = txtRfc.Text.Trim().ToUpper(),
                            CodigoPostal = txtCodigoPostal.Text.Trim(),
                            RegimenFiscal = regimen,
                            UsoCFDI = uso,
                            Creado = DateTime.Now
                        };
                        db.Clientes.Add(nuevoCliente);
                        db.SaveChanges();
                    }
                    else
                    {
                        var clienteEnDb = db.Clientes.Find(_clienteEdicion.ID);
                        if (clienteEnDb != null)
                        {
                            clienteEnDb.RazonSocial = txtRazonSocial.Text.Trim();
                            clienteEnDb.Telefono = txtTelefono.Text.Trim();
                            clienteEnDb.EsFactura = chkEsFactura.IsChecked == true;
                            clienteEnDb.RFC = txtRfc.Text.Trim().ToUpper();
                            clienteEnDb.CodigoPostal = txtCodigoPostal.Text.Trim();
                            clienteEnDb.RegimenFiscal = regimen;
                            clienteEnDb.UsoCFDI = uso;

                            db.Clientes.Update(clienteEnDb);
                            db.SaveChanges();

                            // Sync
                            int idSync = clienteEnDb.ID;
                            Task.Run(async () => {
                                try
                                {
                                    using (var dbSync = new InventarioDbContext())
                                    {
                                        var c = await dbSync.Clientes.FindAsync(idSync);
                                        if (c != null) await new OrySiPOS.Services.SupabaseService().SincronizarCliente(c);
                                    }
                                }
                                catch { }
                            });
                        }
                    }
                }

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar: " + ex.Message);
            }
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
        }
    }
}