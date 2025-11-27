using System.Windows;
using System.Windows.Input;
using OrySiPOS.Data;
using OrySiPOS.Models;

namespace OrySiPOS.Views.Dialogs
{
    public partial class NuevoClienteWindow : Window
    {
        // Variable para guardar el cliente si estamos en modo edición
        private Cliente _clienteEdicion;

        public NuevoClienteWindow(Cliente clienteAEditar = null)
        {
            InitializeComponent();

            if (clienteAEditar != null)
            {
                // MODO EDICIÓN
                _clienteEdicion = clienteAEditar;
                CargarDatosEnFormulario();
                btnGuardar.Content = "Actualizar Cliente"; // Cambiamos el texto del botón
                this.Title = "Editar Cliente"; // Cambiamos el título de la ventana
            }
            else
            {
                // MODO CREACIÓN (Normal)
                _clienteEdicion = null;
            }
        }

        private void CargarDatosEnFormulario()
        {
            // Rellenamos las cajas de texto con los datos del cliente existente
            txtRazonSocial.Text = _clienteEdicion.RazonSocial;
            txtTelefono.Text = _clienteEdicion.Telefono;
            chkEsFactura.IsChecked = _clienteEdicion.EsFactura;

            // Datos fiscales
            txtRfc.Text = _clienteEdicion.RFC;
            txtCodigoPostal.Text = _clienteEdicion.CodigoPostal;
            txtRegimenFiscal.Text = _clienteEdicion.RegimenFiscal;
            txtUsoCFDI.Text = _clienteEdicion.UsoCFDI;
        }

        // --- 1. ARRASTRAR VENTANA ---
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        // --- 2. LÓGICA VISUAL INTELIGENTE ---
        // Aquí ocurre la magia de borrar/rellenar los campos
        private void chkEsFactura_Click(object sender, RoutedEventArgs e)
        {
            bool requiereFactura = chkEsFactura.IsChecked == true;

            if (requiereFactura)
            {
                // CASO: QUIERE FACTURA -> Borramos los genéricos para que escriba

                // Solo borramos si el texto es el genérico por defecto. 
                // (Así, si el usuario ya escribió algo y desmarca/marca por error, no le borramos su avance)
                if (txtRfc.Text == "XAXX010101000") txtRfc.Text = "";
                if (txtCodigoPostal.Text == "00000") txtCodigoPostal.Text = "";
                if (txtRegimenFiscal.Text == "616") txtRegimenFiscal.Text = "";
                if (txtUsoCFDI.Text == "S01") txtUsoCFDI.Text = "";

                // Ponemos el cursor en el RFC para empezar a escribir de inmediato
                txtRfc.Focus();
            }
            else
            {
                // CASO: NO QUIERE FACTURA -> Rellenamos con genéricos

                // Restauramos los valores "comodín" del SAT
                txtRfc.Text = "XAXX010101000";
                txtCodigoPostal.Text = "00000";
                txtRegimenFiscal.Text = "616";
                txtUsoCFDI.Text = "S01";
            }
        }

        // --- 3. GUARDAR CLIENTE ---
        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // --- TUS VALIDACIONES (NO CAMBIAN) ---
            if (string.IsNullOrWhiteSpace(txtRazonSocial.Text))
            {
                MessageBox.Show("El nombre o razón social es obligatorio.", "Falta nombre", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (chkEsFactura.IsChecked == true)
            {
                if (string.IsNullOrWhiteSpace(txtRfc.Text) || txtRfc.Text == "XAXX010101000")
                {
                    MessageBox.Show("Si requiere factura, debes ingresar un RFC real válido.", "RFC Inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (string.IsNullOrWhiteSpace(txtCodigoPostal.Text) || txtCodigoPostal.Text == "00000")
                {
                    MessageBox.Show("El código postal es obligatorio para facturar.", "CP Inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            //// Crear el objeto
            //var nuevoCliente = new Cliente
            //{
            //    RazonSocial = txtRazonSocial.Text.Trim(),
            //    Telefono = txtTelefono.Text.Trim(),
            //    Activo = true,
            //    EsFactura = chkEsFactura.IsChecked == true,
            //    RFC = txtRfc.Text.Trim().ToUpper(),
            //    CodigoPostal = txtCodigoPostal.Text.Trim(),
            //    RegimenFiscal = txtRegimenFiscal.Text.Trim(),
            //    UsoCFDI = txtUsoCFDI.Text.Trim()
            //};

            try
            {
                using (var db = new InventarioDbContext())
                {
                    if (_clienteEdicion == null)
                    {
                        // --- LÓGICA DE CREAR NUEVO (Tu código original) ---
                        var nuevoCliente = new Cliente
                        {
                            RazonSocial = txtRazonSocial.Text.Trim(),
                            Telefono = txtTelefono.Text.Trim(),
                            Activo = true,
                            EsFactura = chkEsFactura.IsChecked == true,
                            RFC = txtRfc.Text.Trim().ToUpper(),
                            CodigoPostal = txtCodigoPostal.Text.Trim(),
                            RegimenFiscal = txtRegimenFiscal.Text.Trim(),
                            UsoCFDI = txtUsoCFDI.Text.Trim()
                        };

                        db.Clientes.Add(nuevoCliente);
                        db.SaveChanges();

                        // Sync nube (copia tu lógica original de sync aquí para nuevo cliente)
                    }
                    else
                    {
                        // --- LÓGICA DE ACTUALIZAR (LO NUEVO) ---

                        // 1. Buscamos el cliente en la BD para asegurarnos de tener la última versión
                        var clienteEnDb = db.Clientes.Find(_clienteEdicion.ID);

                        if (clienteEnDb != null)
                        {
                            // 2. Actualizamos los campos
                            clienteEnDb.RazonSocial = txtRazonSocial.Text.Trim();
                            clienteEnDb.Telefono = txtTelefono.Text.Trim();
                            clienteEnDb.EsFactura = chkEsFactura.IsChecked == true;
                            clienteEnDb.RFC = txtRfc.Text.Trim().ToUpper();
                            clienteEnDb.CodigoPostal = txtCodigoPostal.Text.Trim();
                            clienteEnDb.RegimenFiscal = txtRegimenFiscal.Text.Trim();
                            clienteEnDb.UsoCFDI = txtUsoCFDI.Text.Trim();

                            // 3. Guardamos cambios
                            db.Clientes.Update(clienteEnDb);
                            db.SaveChanges();

                            // 4. Sync Nube (Tarea en segundo plano)
                            int idActualizado = clienteEnDb.ID;
                            Task.Run(async () =>
                            {
                                try
                                {
                                    // Obtenemos contexto fresco para el hilo secundario
                                    using (var dbSync = new InventarioDbContext())
                                    {
                                        var c = dbSync.Clientes.Find(idActualizado);
                                        if (c != null)
                                        {
                                            var srv = new OrySiPOS.Services.SupabaseService();
                                            await srv.SincronizarCliente(c);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine("Error sync update: " + ex.Message);
                                }
                            });
                        }
                    }
                }

                MessageBox.Show("¡Operación exitosa!", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
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
    }
}