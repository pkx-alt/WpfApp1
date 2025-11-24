using System.Windows;
using System.Windows.Input;
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
            // Validaciones básicas
            if (string.IsNullOrWhiteSpace(txtRazonSocial.Text))
            {
                MessageBox.Show("El nombre o razón social es obligatorio.", "Falta nombre", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validación Estricta (Solo si pidió factura)
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

                // Nota: No necesitamos lógica especial aquí porque los TextBox 
                // ya tienen el valor correcto (sea el genérico o el real) gracias al evento del checkbox.
                RFC = txtRfc.Text.Trim().ToUpper(),
                CodigoPostal = txtCodigoPostal.Text.Trim(),
                RegimenFiscal = txtRegimenFiscal.Text.Trim(),
                UsoCFDI = txtUsoCFDI.Text.Trim()
            };

            try
            {
                using (var db = new InventarioDbContext())
                {
                    db.Clientes.Add(nuevoCliente);
                    db.SaveChanges();
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