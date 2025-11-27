using OrySiPOS.Data;
using OrySiPOS.Models;
using OrySiPOS.ViewModels;
using OrySiPOS.Views;
using OrySiPOS.Views.Dialogs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.Generic;

namespace OrySiPOS.Views
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
        // En ClientesPage.xaml.cs

        // En Views/ClientesPage.xaml.cs

        private void ActualizarFiltroClientes()
        {
            var viewModel = (ClientesViewModel)this.DataContext;
            if (viewModel == null) return;

            // 1. Texto de búsqueda
            string textoParaBuscar = txtBusqueda.Text;
            if (textoParaBuscar == "Buscar cliente por RFC o razón social" || string.IsNullOrWhiteSpace(textoParaBuscar))
            {
                textoParaBuscar = null;
            }

            // 2. Estado (Activos/Inactivos)
            bool verActivos = chkActivos.IsChecked == true;
            bool verInactivos = chkInactivos.IsChecked == true;

            // 3. ¡NUEVO! Leemos tus nuevos checkboxes de tipo de cliente
            bool verFacturados = chkConFactura.IsChecked == true;
            bool verNoFacturados = chkSinFactura.IsChecked == true;

            // 4. Enviamos TODO al ViewModel (fíjate que ahora pasamos 5 cosas)
            viewModel.CargarClientes(textoParaBuscar, verActivos, verInactivos, verFacturados, verNoFacturados);

            // Actualizamos el conteo de selección si tienes esa función
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

            var clientesParaCambiar = viewModel.Clientes.Where(c => c.IsSelected && c.Activo).ToList();

            if (clientesParaCambiar.Count == 0)
            {
                MessageBox.Show("No hay clientes activos seleccionados.", "Aviso");
                return;
            }

            if (MessageBox.Show($"¿Deshabilitar {clientesParaCambiar.Count} clientes?", "Confirmar", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            try
            {
                var idsAfectados = clientesParaCambiar.Select(c => c.ID).ToList();

                // 1. CAMBIO LOCAL MASIVO
                using (var db = new InventarioDbContext())
                {
                    var clientesEnDb = db.Clientes.Where(c => idsAfectados.Contains(c.ID)).ToList();
                    foreach (var clienteDb in clientesEnDb)
                    {
                        clienteDb.Activo = false;
                    }
                    db.SaveChanges();
                }

                // 2. SYNC NUBE MASIVO
                Task.Run(async () =>
                {
                    try
                    {
                        using (var dbSync = new InventarioDbContext())
                        {
                            // Recuperamos los clientes ya actualizados
                            var listaParaNube = dbSync.Clientes.Where(c => idsAfectados.Contains(c.ID)).ToList();

                            var srv = new OrySiPOS.Services.SupabaseService();
                            foreach (var c in listaParaNube)
                            {
                                await srv.SincronizarCliente(c);
                            }
                        }
                    }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Error sync masivo clientes: " + ex.Message); }
                });

                MessageBox.Show("¡Clientes deshabilitados con éxito!", "Éxito");
                ActualizarFiltroClientes();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        // 1. Abre el menú al dar clic en "..."
        private void BtnOpciones_Click(object sender, RoutedEventArgs e)
        {
            var boton = sender as Button;
            if (boton?.ContextMenu != null)
            {
                // Pasamos el "Cliente" de la fila al menú para que sepa con quién trabajar
                boton.ContextMenu.DataContext = boton.DataContext;
                boton.ContextMenu.PlacementTarget = boton;
                boton.ContextMenu.IsOpen = true;
            }
        }

        // 2. Ejecuta el comando de Editar del ViewModel
        private void MenuEditarCliente_Click(object sender, RoutedEventArgs e)
        {
            // Recuperamos el cliente y el ViewModel
            if (sender is MenuItem menuItem && menuItem.DataContext is Cliente cliente)
            {
                if (this.DataContext is ClientesViewModel vm)
                {
                    if (vm.EditarClienteCommand.CanExecute(cliente))
                        vm.EditarClienteCommand.Execute(cliente);
                }
            }
        }

        // 3. Ejecuta el comando de Cambiar Estado del ViewModel
        private void MenuCambiarEstado_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is Cliente cliente)
            {
                if (this.DataContext is ClientesViewModel vm)
                {
                    if (vm.CambiarEstadoClienteCommand.CanExecute(cliente))
                        vm.CambiarEstadoClienteCommand.Execute(cliente);
                }
            }
        }

        // 1. MÉTODO AYUDANTE (Hace el trabajo real)
        // Este método recibe el Cliente directamente y abre la ventana.
        private void AbrirDetallesCliente(Cliente cliente)
        {
            // Preparamos los datos a mostrar (Copiamos tu lógica original aquí)
            var listaDetalles = new List<ReporteItem>
    {
        new ReporteItem { Propiedad = "ID Cliente", Valor = cliente.ID.ToString() },
        new ReporteItem { Propiedad = "Razón Social", Valor = cliente.RazonSocial },
        new ReporteItem { Propiedad = "RFC", Valor = cliente.RFC },
        new ReporteItem { Propiedad = "Teléfono", Valor = cliente.Telefono },
        new ReporteItem { Propiedad = "Código Postal", Valor = cliente.CodigoPostal },
        new ReporteItem { Propiedad = "Régimen Fiscal", Valor = cliente.RegimenFiscal },
        new ReporteItem { Propiedad = "Uso CFDI", Valor = cliente.UsoCFDI },
        new ReporteItem { Propiedad = "Tipo", Valor = cliente.EsFactura ? "Facturación" : "Público General" },
        new ReporteItem { Propiedad = "Estado", Valor = cliente.Activo ? "Activo" : "Inactivo" },
        new ReporteItem { Propiedad = "Fecha Registro", Valor = cliente.Creado.ToString("dd/MM/yyyy HH:mm") }
    };

            // Abrimos el visor
            var visor = new VisorReporteWindow($"Cliente: {cliente.RazonSocial}", listaDetalles);
            visor.Owner = Window.GetWindow(this);
            visor.ShowDialog();
        }

        // 2. EVENTO PARA EL DOBLE CLIC (Lo mantenemos, pero ahora usa el ayudante)
        private void GridClientes_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var grid = sender as DataGrid;
            if (grid?.SelectedItem is Cliente cliente)
            {
                AbrirDetallesCliente(cliente); // <--- Llamamos al ayudante
            }
        }

        // 3. ¡NUEVO! EVENTO PARA EL MENÚ CONTEXTUAL (Este recibe RoutedEventArgs)
        private void VerDetallesMenu_Click(object sender, RoutedEventArgs e)
        {
            // El "sender" es el MenuItem. Su DataContext es el Cliente de la fila donde hiciste clic derecho.
            if (sender is MenuItem menuItem && menuItem.DataContext is Cliente cliente)
            {
                AbrirDetallesCliente(cliente); // <--- Llamamos al mismo ayudante
            }
        }
    }
}