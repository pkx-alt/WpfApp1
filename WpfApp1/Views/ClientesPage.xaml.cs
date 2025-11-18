using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfApp1.Data;
using WpfApp1.Models;
using WpfApp1.ViewModels;
using WpfApp1.Views;
using WpfApp1.Views.Dialogs;

namespace WpfApp1.Views
{
    public partial class ClientesPage : Page
    {
        public ClientesPage()
        {
            InitializeComponent();
            this.DataContext = new ClientesViewModel();

            // Hacemos una carga inicial con los filtros por defecto
            ActualizarFiltroClientes();
        }

        private void btnNuevoCliente_Click(object sender, RoutedEventArgs e)
        {
            var ventanaNuevoCliente = new NuevoClienteWindow();
            bool? resultado = ventanaNuevoCliente.ShowDialog();

            if (resultado == true)
            {
                // ¡Ahora usamos nuestro método maestro!
                ActualizarFiltroClientes();
            }
        }

        // --- MÉTODOS DEL PLACEHOLDER (SIN CAMBIOS) ---
        private void txtBusqueda_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtBusqueda.Text == "Buscar cliente por RFC o razón social")
            {
                txtBusqueda.Text = "";
                txtBusqueda.Foreground = Brushes.Black;
            }
        }

        private void txtBusqueda_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBusqueda.Text))
            {
                txtBusqueda.Text = "Buscar cliente por RFC o razón social";
                txtBusqueda.Foreground = Brushes.Gray;
            }
        }

        // --- EVENTOS DE FILTRADO (AQUÍ ESTÁ EL CAMBIO) ---

        // 1. Evento de los CheckBox
        private void chkStatus_Click(object sender, RoutedEventArgs e)
        {
            // Cada vez que un check cambie, actualiza la lista
            ActualizarFiltroClientes();
        }

        // 2. Evento del Texto de Búsqueda
        private void txtBusqueda_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Cada vez que el texto cambie, actualiza la lista
            ActualizarFiltroClientes();
        }

        // --- 3. ¡NUEVO MÉTODO "MAESTRO"! ---
        private void ActualizarFiltroClientes()
        {
            var viewModel = (ClientesViewModel)this.DataContext;
            if (viewModel == null)
            {
                return; // Evita errores al inicio
            }

            // --- A. Leemos el texto de búsqueda ---
            string textoParaBuscar = txtBusqueda.Text;
            if (textoParaBuscar == "Buscar cliente por RFC o razón social" ||
                string.IsNullOrWhiteSpace(textoParaBuscar))
            {
                textoParaBuscar = null;
            }

            // --- B. Leemos el estado de los CheckBoxes ---
            // IsChecked es un 'bool?' (nullable), así que '== true' 
            // lo convierte a un 'bool' simple (false si es null o false).
            bool verActivos = chkActivos.IsChecked == true;
            bool verInactivos = chkInactivos.IsChecked == true;

            // --- C. Llamamos al ViewModel con TODO ---
            viewModel.CargarClientes(textoParaBuscar, verActivos, verInactivos);
            // ¡AÑADIDO! Cada vez que filtremos, reseteamos el conteo.
            ActualizarConteoSeleccion();
        }

        // ... tus otros métodos (btnNuevoCliente_Click, chkStatus_Click, etc.) ...

        private void btnCambiarEstado_Click(object sender, RoutedEventArgs e)
        {
            // --- ¡Este es el truco! ---
            // El 'sender' es el botón en el que hicimos clic.
            // Su 'DataContext' (contexto de datos) es el objeto
            // de esa fila específica. ¡Es el Cliente!
            var clienteSeleccionado = (sender as FrameworkElement).DataContext as Cliente;

            if (clienteSeleccionado == null)
            {
                return; // Seguridad, aunque nunca debería pasar
            }

            // 1. Creamos el modal, pasándole el cliente de esta fila
            var modal = new ConfirmarEstadoClienteWindow(clienteSeleccionado);

            // 2. Lo mostramos y esperamos a que se cierre
            bool? resultado = modal.ShowDialog();

            // 3. Si se cerró con "éxito" (DialogResult == true)...
            if (resultado == true)
            {
                // ... ¡refrescamos toda la lista para ver el cambio!
                ActualizarFiltroClientes();
            }
        }

        // --- ¡¡NUEVA SECCIÓN DE LÓGICA DE SELECCIÓN!! ---

        // 1. Método de conteo
        private void ActualizarConteoSeleccion()
        {
            var viewModel = (ClientesViewModel)this.DataContext;
            if (viewModel == null) return;

            // Contamos cuántos clientes tienen IsSelected = true
            int conteo = viewModel.Clientes.Count(c => c.IsSelected);

            // Actualizamos el texto
            if (conteo == 1)
            {
                txtConteoSeleccion.Text = "1 cliente seleccionado";
            }
            else
            {
                txtConteoSeleccion.Text = $"{conteo} clientes seleccionados";
            }

            // Mostramos u ocultamos el panel de acciones
            spAccionesSeleccion.Visibility = (conteo > 0) ? Visibility.Visible : Visibility.Collapsed;

            // (Extra) Sincronizamos el check de "seleccionar todo"
            if (conteo == viewModel.Clientes.Count && conteo > 0)
            {
                chkSeleccionarTodo.IsChecked = true;
            }
            else
            {
                chkSeleccionarTodo.IsChecked = false;
            }
        }

        private void ClienteCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            // Estos eventos (Checked/Unchecked) se disparan DESPUÉS 
            // de que el binding 'IsSelected' se ha actualizado.
            // ¡Así que el conteo siempre será correcto!
            ActualizarConteoSeleccion();
        }

        // 3. Evento para el CheckBox de "Seleccionar Todo"
        private void chkSeleccionarTodo_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = (ClientesViewModel)this.DataContext;
            if (viewModel == null) return;

            // Vemos si el checkbox de "seleccionar todo" está marcado
            bool estaMarcado = (sender as CheckBox).IsChecked == true;

            // Marcamos o desmarcamos TODOS los clientes en la lista actual
            foreach (var cliente in viewModel.Clientes)
            {
                cliente.IsSelected = estaMarcado;
            }

            // Actualizamos el conteo
            ActualizarConteoSeleccion();
        }

        // 4. Evento para el botón "Deshabilitar Selección"
        private void btnDeshabilitarSeleccion_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = (ClientesViewModel)this.DataContext;
            if (viewModel == null) return;

            // Obtenemos la lista de clientes a deshabilitar
            var clientesParaCambiar = viewModel.Clientes
                .Where(c => c.IsSelected && c.Activo) // Solo seleccionados Y activos
                .ToList();

            if (clientesParaCambiar.Count == 0)
            {
                MessageBox.Show("No hay clientes activos seleccionados para deshabilitar.", "Aviso");
                return;
            }

            // Confirmación
            var resultado = MessageBox.Show(
                $"¿Estás seguro de que deseas deshabilitar {clientesParaCambiar.Count} cliente(s) seleccionado(s)?",
                "Confirmar deshabilitación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (resultado != MessageBoxResult.Yes)
            {
                return; // El usuario canceló
            }

            // Hacemos el cambio en la BD
            try
            {
                using (var db = new InventarioDbContext())
                {
                    // Obtenemos los IDs
                    var ids = clientesParaCambiar.Select(c => c.ID).ToList();

                    // Buscamos los clientes en la BD y los actualizamos
                    var clientesEnDb = db.Clientes.Where(c => ids.Contains(c.ID)).ToList();
                    foreach (var clienteDb in clientesEnDb)
                    {
                        clienteDb.Activo = false; // ¡Deshabilitados!
                    }
                    db.SaveChanges();
                }

                MessageBox.Show("¡Clientes deshabilitados con éxito!", "Éxito");

                // Refrescamos la lista entera para que los filtros se apliquen
                ActualizarFiltroClientes();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al deshabilitar los clientes: " + ex.Message, "Error");
            }
        }
    }
}