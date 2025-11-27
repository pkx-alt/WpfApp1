using System.Windows;
using System.Windows.Input;
using OrySiPOS.Data;
using OrySiPOS.Models;

namespace OrySiPOS.Views.Dialogs
{
    public partial class NuevoClienteWindow : Window
    {
        public NuevoClienteWindow()
        {
            InitializeComponent();
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

            // Crear el objeto
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

            try
            {
                int nuevoId = 0;

                // 1. GUARDAR LOCALMENTE (SQLite)
                using (var db = new InventarioDbContext())
                {
                    db.Clientes.Add(nuevoCliente);
                    db.SaveChanges();
                    nuevoId = nuevoCliente.ID; // Aquí obtenemos el ID generado por la BD
                }

                // 2. ¡AQUÍ ESTABA EL FALTANTE! -> SINCRONIZAR CON SUPABASE
                if (nuevoId > 0)
                {
                    // Le asignamos el ID real al objeto antes de enviarlo
                    nuevoCliente.ID = nuevoId;

                    // Lanzamos la tarea en segundo plano (Fire and Forget)
                    Task.Run(async () =>
                    {
                        try
                        {
                            var srv = new OrySiPOS.Services.SupabaseService();
                            await srv.SincronizarCliente(nuevoCliente);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine("Error sync cliente background: " + ex.Message);
                        }
                    });
                }

                MessageBox.Show("¡Cliente registrado exitosamente!", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Error al guardar: " + ex.Message, "Error de Base de Datos");
            }
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}