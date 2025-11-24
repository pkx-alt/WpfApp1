using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore; // ¡Vital para .Include()!
using WpfApp1.Data;
using WpfApp1.Models;
using System.Threading.Tasks; // Para las tareas asíncronas
using WpfApp1.Services;       // Para encontrar tu SupabaseService

namespace WpfApp1.ViewModels
{
    public class CotizacionesViewModel : ViewModelBase
    {
        // --- COLECCIONES (Datos para la Vista) ---
        public ObservableCollection<CotizacionItemViewModel> ListaCotizaciones { get; set; }
        public ObservableCollection<Cliente> ListaClientes { get; set; }

        // Para el combo de "Origen" (lo dejamos fijo por ahora como en tu diseño)
        public ObservableCollection<string> ListaOrigenes { get; set; }

        // --- FILTROS ---
        private DateTime _fechaDesde;
        public DateTime FechaDesde
        {
            get { return _fechaDesde; }
            set
            {
                _fechaDesde = value;
                OnPropertyChanged();
                CargarCotizaciones(); // Recargar al cambiar fecha
            }
        }

        private DateTime _fechaHasta;
        public DateTime FechaHasta
        {
            get { return _fechaHasta; }
            set
            {
                _fechaHasta = value;
                OnPropertyChanged();
                CargarCotizaciones();
            }
        }

        private Cliente _clienteSeleccionado;
        public Cliente ClienteSeleccionado
        {
            get { return _clienteSeleccionado; }
            set
            {
                _clienteSeleccionado = value;
                OnPropertyChanged();
                CargarCotizaciones();
            }
        }

        private string _textoBusqueda;
        public string TextoBusqueda
        {
            get { return _textoBusqueda; }
            set
            {
                _textoBusqueda = value;
                OnPropertyChanged();
                // Podrías poner un temporizador aquí para no buscar en cada tecla,
                // pero por ahora buscaremos directo.
                CargarCotizaciones();
            }
        }
        // ... dentro de la clase CotizacionesViewModel ...

        // Propiedad para guardar qué eligió el usuario en el ComboBox
        private string _origenSeleccionado;
        public string OrigenSeleccionado
        {
            get { return _origenSeleccionado; }
            set
            {
                _origenSeleccionado = value;
                OnPropertyChanged();
                CargarCotizaciones(); // ¡Recargamos la lista al cambiar!
            }
        }

        // --- COMANDOS ---
        public ICommand VerDetalleCommand { get; }
        public ICommand ImprimirCommand { get; } // Para futura implementación
                                                 // ... tus otras propiedades ...
        public ICommand SincronizarWebCommand { get; }

        // --- CONSTRUCTOR ---
        public CotizacionesViewModel()
        {
            ListaCotizaciones = new ObservableCollection<CotizacionItemViewModel>();
            ListaClientes = new ObservableCollection<Cliente>();
            ListaOrigenes = new ObservableCollection<string> { "Todos", "Local", "Web" };
            // --- ¡AGREGAR ESTO! ---
            OrigenSeleccionado = "Todos"; // Seleccionamos "Todos" por defecto
            // Fechas por defecto: Mes actual
            FechaDesde = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            FechaHasta = DateTime.Now;

            // Agrega esta línea:
            SincronizarWebCommand = new RelayCommand(async (p) => await SincronizarWeb());

            VerDetalleCommand = new RelayCommand(VerDetalle);

            // Cargar datos iniciales
            CargarClientes();
            CargarCotizaciones();
        }

        // --- MÉTODOS DE CARGA ---

        private void CargarClientes()
        {
            using (var db = new InventarioDbContext())
            {
                var clientes = db.Clientes.Where(c => c.Activo).OrderBy(c => c.RazonSocial).ToList();
                ListaClientes.Clear();

                // Agregamos un cliente "filtro vacío"
                ListaClientes.Add(new Cliente { ID = -1, RazonSocial = "Todos los clientes" });

                foreach (var c in clientes) ListaClientes.Add(c);

                // Seleccionamos "Todos" por defecto
                _clienteSeleccionado = ListaClientes.First();
                OnPropertyChanged(nameof(ClienteSeleccionado));
            }
        }

        private void CargarCotizaciones()
        {
            using (var db = new InventarioDbContext())
            {
                // 1. Incluimos al cliente para poder ver su Razón Social
                var query = db.Cotizaciones
                              .Include(c => c.Cliente)
                              .AsQueryable();

                // --- CORRECCIÓN DE FECHAS AQUÍ ---

                // Fecha Inicio: Nos aseguramos que sea a las 00:00:00
                DateTime inicio = FechaDesde.Date;

                // Fecha Fin: Le sumamos 1 día y restamos un "tick" para obtener las 23:59:59.999
                // Así incluimos todo lo que pasó ese último día.
                DateTime fin = FechaHasta.Date.AddDays(1).AddTicks(-1);

                query = query.Where(c => c.FechaEmision >= inicio && c.FechaEmision <= fin);

                // ----------------------------------

                // 2. Filtro de Cliente
                if (ClienteSeleccionado != null && ClienteSeleccionado.ID != -1)
                {
                    query = query.Where(c => c.ClienteId == ClienteSeleccionado.ID);
                }

                if (!string.IsNullOrEmpty(OrigenSeleccionado) && OrigenSeleccionado != "Todos")
                {
                    // Filtramos: Que el campo Origen de la BD coincida con lo seleccionado
                    query = query.Where(c => c.Origen == OrigenSeleccionado);
                }

                // 3. Filtro de Texto (Buscador)
                if (!string.IsNullOrWhiteSpace(TextoBusqueda) && TextoBusqueda != "Buscar por folio, cliente....")
                {
                    string texto = TextoBusqueda.ToLower();
                    query = query.Where(c => c.ID.ToString().Contains(texto) ||
                                             (c.Cliente != null && c.Cliente.RazonSocial.ToLower().Contains(texto)));
                }

                // 4. Ejecutamos la consulta (ordenando por más reciente)
                var resultados = query.OrderByDescending(c => c.FechaEmision).ToList();

                // 5. Mapear a la lista visual
                ListaCotizaciones.Clear();
                foreach (var cot in resultados)
                {
                    ListaCotizaciones.Add(new CotizacionItemViewModel(cot));
                }
            }
        }



        private void VerDetalle(object parameter)
        {
            if (parameter is CotizacionItemViewModel item)
            {
                MessageBox.Show($"Aquí abriríamos el detalle de la cotización #{item.Folio}.\n\n" +
                                $"Esto lo implementaremos luego para poder 'cargar' la cotización en la pantalla de ventas.",
                                "Próximamente");
            }
        }

        private async Task SincronizarWeb()
        {
            try
            {
                // Opcional: Mostrar un cursor de espera
                Mouse.OverrideCursor = Cursors.Wait;

                var servicio = new SupabaseService();

                // Llamamos al servicio que creamos antes
                int cantidad = await servicio.SincronizarCotizaciones();

                if (cantidad > 0)
                {
                    MessageBox.Show($"¡Éxito! Se descargaron {cantidad} cotizaciones de la nube.",
                                    "Sincronización Completada",
                                    MessageBoxButton.OK, MessageBoxImage.Information);

                    // IMPORTANTE: Recargar la lista visual para ver los nuevos datos
                    CargarCotizaciones();
                }
                else
                {
                    MessageBox.Show("Conexión exitosa, pero no hay cotizaciones nuevas con estado 'PENDIENTE'.",
                                    "Sin novedades",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al conectar con la nube: {ex.Message}\n\nRevisa tu internet o las credenciales.",
                                "Error de Conexión",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null; // Regresamos el cursor a la normalidad
            }
        }


        // --- CLASE AUXILIAR (Wrapper) PARA MOSTRAR EN LA TABLA ---
        // Usamos esto para dar formato fácil a los datos crudos de la BD
        public class CotizacionItemViewModel
        {
            public Cotizacion _cotizacion;

            public int Folio => _cotizacion.ID;
            public DateTime FechaEmision => _cotizacion.FechaEmision;
            public DateTime FechaVencimiento => _cotizacion.FechaVencimiento;

            public string ClienteNombre => _cotizacion.Cliente != null ? _cotizacion.Cliente.RazonSocial : "Público General";
            public string Origen => _cotizacion.Origen;
            public decimal Total => _cotizacion.Total;

            // Lógica simple de estado: Si ya pasó la fecha de vence, está "Vencida"
            public string Estado => DateTime.Now > _cotizacion.FechaVencimiento ? "Vencida" : "Vigente";

            public CotizacionItemViewModel(Cotizacion cot)
            {
                _cotizacion = cot;
            }
        }
    }
}