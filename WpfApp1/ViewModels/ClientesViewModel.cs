using OrySiPOS.Data;
using OrySiPOS.Models;
using OrySiPOS.Services;
using OrySiPOS.Views.Dialogs;
using System.Collections.ObjectModel; // Para ObservableCollection
using System.Linq; // Para .ToList()
using System.Windows;
using System.Windows.Input;


namespace OrySiPOS.ViewModels
{
    // NOTA: Para un MVVM "completo" esto implementaría INotifyPropertyChanged,
    // pero vamos a mantenerlo simple por ahora.
    public class ClientesViewModel : ViewModelBase
    {
        // Esta es la propiedad que tu DataGrid está buscando.
        // La inicializamos de una vez para que no esté vacía.
        // COMANDO NUEVO
        public ICommand EditarClienteCommand { get; private set; }

        // COMANDO PARA DESHABILITAR (Reutilizando lógica)
        public ICommand CambiarEstadoClienteCommand { get; private set; }
        public ObservableCollection<Cliente> Clientes { get; set; } = new ObservableCollection<Cliente>();

        // El constructor: se ejecuta cuando se crea el objeto
        public ClientesViewModel()
        {
            // Inicializar comandos
            EditarClienteCommand = new RelayCommand(OnEditarCliente);
            CambiarEstadoClienteCommand = new RelayCommand(OnCambiarEstadoCliente);
            CargarClientes(); // Cargamos los clientes al iniciar
        }

        // El método para cargar (o recargar) los clientes
        // En ClientesViewModel.cs, dentro de CargarClientes
        // En ClientesViewModel.cs

        // Modifica la firma del método para aceptar los nuevos filtros (verFacturados, verNoFacturados)
        // En ClientesViewModel.cs

        // Nota cómo agregamos dos nuevos booleanos al final: verFacturados y verNoFacturados

        private int _totalClientes;
        public int TotalClientes
        {
            get { return _totalClientes; }
            set { _totalClientes = value; OnPropertyChanged(); } // Ahora sí funcionará OnPropertyChanged
        }
        public void CargarClientes(string busqueda = null, bool verActivos = true, bool verInactivos = false, bool verFacturados = true, bool verNoFacturados = true)
        {
            Clientes.Clear();

            using (var db = new InventarioDbContext())
            {
                var consulta = db.Clientes.AsQueryable();

                // 1. Filtro de Búsqueda (Esto ya lo tenías)
                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    string busquedaUpper = busqueda.ToUpper();
                    consulta = consulta.Where(c =>
                        c.RazonSocial.ToUpper().Contains(busquedaUpper) ||
                        c.RFC.ToUpper().Contains(busquedaUpper)
                    );
                }

                // 2. Filtro de Estado (Activo/Inactivo)
                consulta = consulta.Where(c =>
                    (c.Activo && verActivos) ||
                    (!c.Activo && verInactivos)
                );

                // 3. ¡AQUÍ ESTÁ LA MAGIA NUEVA! Filtro de Facturación
                // Le decimos: "Traeme al cliente SI (es factura Y queremos ver facturados) O (no es factura Y queremos ver público general)"
                consulta = consulta.Where(c =>
                    (c.EsFactura && verFacturados) ||
                    (!c.EsFactura && verNoFacturados)
                );

                // Ejecución y llenado de la lista
                var listaClientes = consulta.OrderByDescending(c => c.Creado).ToList();

                foreach (var cliente in listaClientes)
                {
                    Clientes.Add(cliente);
                }

                TotalClientes = Clientes.Count;
            }
        }

        // Cambiamos "void" por "async void" para poder usar await
        // En ClientesViewModel.cs

        private void OnEditarCliente(object parametro)
        {
            if (parametro is Cliente clienteSeleccionado)
            {
                // Pasamos el cliente (que tiene datos viejos en memoria) solo para llenar los campos iniciales
                var ventana = new NuevoClienteWindow(clienteSeleccionado);

                ventana.Owner = Application.Current.MainWindow;

                bool? resultado = ventana.ShowDialog();

                if (resultado == true)
                {
                    // SOLO recargamos la lista para traer los datos nuevos desde SQLite.
                    // La sincronización con la nube YA LA HIZO la ventana internamente.
                    CargarClientes();
                }
            }
        }

        private void OnCambiarEstadoCliente(object parametro)
        {
            if (parametro is Cliente clienteSeleccionado)
            {
                var modal = new ConfirmarEstadoClienteWindow(clienteSeleccionado);
                bool? resultado = modal.ShowDialog();

                if (resultado == true)
                {
                    CargarClientes();
                }
            }
        }
    }
}