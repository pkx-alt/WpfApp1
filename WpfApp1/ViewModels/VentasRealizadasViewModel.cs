// --- ViewModels/VentasRealizadasViewModel.cs ---

using Microsoft.EntityFrameworkCore; // Para el .Include()
using System; // Para DateTime
using System.Collections.Generic; // Para List
using System.Collections.ObjectModel;
using System.Linq;
using OrySiPOS.Data;
using OrySiPOS.Models;
using OrySiPOS.Views.Dialogs; // ¡Para poder usar DetalleVentaDialog!
using System.Windows.Input; // ¡ESTA ES LA LÍNEA QUE FALTA!
using System.Windows; // ¡ESTA ES LA LÍNEA QUE FALTA!

namespace OrySiPOS.ViewModels
{
    // Usamos tu ViewModelBase para poder notificar a la Vista
    public class VentasRealizadasViewModel : ViewModelBase
    {
        // --- 1. Propiedades para el DataGrid ---
        // (Esta es la clase "VentaHistorialItem" que ya tenías
        //  en tu .xaml.cs. ¡Debemos moverla!)
        public ObservableCollection<VentaHistorialItem> Ventas { get; set; }
        public ICommand VerDetalleCommand { get; }
        
        
        private int _totalVentas;
        public int TotalVentas
        {
            get { return _totalVentas; }
            set { _totalVentas = value; OnPropertyChanged(); }
        }
        // 1. DEFINIR EL TEXTO PREDETERMINADO
        public const string PlaceholderBusqueda = "Buscar ventas por folio o cliente....";

        // --- ¡NUEVAS PROPIEDADES PARA EL RESUMEN! ---
        private string _resumenTitulo;
        public string ResumenTitulo
        {
            get { return _resumenTitulo; }
            set { _resumenTitulo = value; OnPropertyChanged(); }
        }

        private decimal _ingresosTotales;
        public decimal IngresosTotales
        {
            get { return _ingresosTotales; }
            set { _ingresosTotales = value; OnPropertyChanged(); }
        }

        private decimal _gananciaTotal;
        public decimal GananciaTotal
        {
            get { return _gananciaTotal; }
            set { _gananciaTotal = value; OnPropertyChanged(); }
        }

        private int _numVentas;
        public int NumVentas
        {
            get { return _numVentas; }
            set { _numVentas = value; OnPropertyChanged(); }
        }

        private decimal _ticketPromedio;
        public decimal TicketPromedio
        {
            get { return _ticketPromedio; }
            set { _ticketPromedio = value; OnPropertyChanged(); }
        }

        // --- 2. Propiedades para los Filtros ---

        // Esta es la "memoria" de la barra de búsqueda
        private string _textoBusqueda;
        public string TextoBusqueda
        {
            get { return _textoBusqueda; }
            set
            {
                _textoBusqueda = value;
                OnPropertyChanged();
                // ¡LA MAGIA! Cada vez que el texto cambia,
                // llamamos a la lógica de recarga.
                CargarHistorialVentas();
                ActualizarResumen(); // ¡AÑADE ESTA LÍNEA!
            }
        }

        // Memoria para el ComboBox de Cliente
        private Cliente _clienteSeleccionado;
        public Cliente ClienteSeleccionado
        {
            get { return _clienteSeleccionado; }
            set
            {
                _clienteSeleccionado = value;
                OnPropertyChanged();
                CargarHistorialVentas(); // Recargamos si cambia el cliente
                ActualizarResumen(); // ¡AÑADE ESTA LÍNEA!
            }
        }

        private Categoria _selectedCategoria;
        public Categoria SelectedCategoria
        {
            get { return _selectedCategoria; }
            set
            {
                _selectedCategoria = value;
                OnPropertyChanged();

                // ¡Llama a AMBOS métodos de actualización!
                CargarHistorialVentas();
                ActualizarResumen(); // ¡El nuevo!
            }
        }


        private DateTime? _fechaDesde;
        public DateTime? FechaDesde
        {
            get { return _fechaDesde; }
            set
            {
                _fechaDesde = value;
                OnPropertyChanged();
                CargarHistorialVentas(); // Recarga el Grid
                ActualizarResumen();   // Recarga el Resumen
            }
        }

        private DateTime? _fechaHasta;
        public DateTime? FechaHasta
        {
            get { return _fechaHasta; }
            set
            {
                _fechaHasta = value;
                OnPropertyChanged();
                CargarHistorialVentas(); // Recarga el Grid
                ActualizarResumen();   // Recarga el Resumen
            }
        }

        // (Podríamos añadir FechaDesde, FechaHasta, etc. de la misma forma)

        // --- 3. Listas para los ComboBox ---
        public ObservableCollection<Cliente> ListaClientes { get; set; }
        public ObservableCollection<Categoria> ListaCategorias { get; set; }
        // (Aquí iría la ListaCategorias, etc.)


        public VentasRealizadasViewModel()
        {
            Ventas = new ObservableCollection<VentaHistorialItem>();
            ListaClientes = new ObservableCollection<Cliente>();
            ListaCategorias = new ObservableCollection<Categoria>(); // ¡NUEVO!
            FechaHasta = DateTime.Now.Date; // Hoy, sin la hora
            FechaDesde = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1); // 1ro del mes
            TextoBusqueda = PlaceholderBusqueda;
            CargarFiltros();
            CargarHistorialVentas();
            ActualizarResumen(); // ¡NUEVO!
            VerDetalleCommand = new RelayCommand(OnVerDetalle);
        }

        // --- 5. Lógica de Carga ---

        public void CargarFiltros()
        {
            // Limpiamos listas
            ListaClientes.Clear();
            ListaCategorias.Clear();

            using (var db = new InventarioDbContext())
            {
                // --- Cargar Clientes (Esto ya lo tenías) ---
                var todosLosClientes = new Cliente { ID = 0, RazonSocial = "Todos los clientes" };
                ListaClientes.Add(todosLosClientes);
                // --- AGREGAMOS ESTO ---
                // 2. Opción "Público en General" (Representa los NULOS)
                ListaClientes.Add(new Cliente { ID = -999, RazonSocial = "Público en General (Ventas rápidas)" });
                // ----------------------
                var clientesDb = db.Clientes.Where(c => c.Activo).OrderBy(c => c.RazonSocial).ToList();
                foreach (var cliente in clientesDb)
                {
                    ListaClientes.Add(cliente);
                }
                // ¡OJO! Asegúrate de que la selección por defecto se asigne
                // DESPUÉS de cargar la lista, o puede fallar.
                ClienteSeleccionado = todosLosClientes;


                // --- ¡AQUÍ ESTÁ LA PARTE QUE FALTA! ---
                // --- Cargar Categorías ---
                var todasCategorias = new Categoria { Id = 0, Nombre = "Todas las categorías" };
                ListaCategorias.Add(todasCategorias);

                var categoriasDb = db.Categorias.OrderBy(c => c.Nombre).ToList();
                foreach (var cat in categoriasDb)
                {
                    ListaCategorias.Add(cat);
                }
                // Asignamos la selección por defecto
                SelectedCategoria = todasCategorias;
            }
        }

        // --- ¡NUEVO MÉTODO EN EL VIEWMODEL! ---
        public void ActualizarResumen()
        {
            // --- ¡LA GUARDIA PRIMERO! ---
            // Si los filtros aún no se han cargado, no hacemos nada.
            if (ClienteSeleccionado == null || SelectedCategoria == null)
            {
                return;
            }
            // --- FIN DE LA GUARDIA ---

            // --- 1. Leemos los filtros (Ahora es seguro) ---
            int categoriaId = SelectedCategoria.Id;
            string nombreCategoria = SelectedCategoria.Nombre;

            string fechaDesdeStr = FechaDesde.HasValue ? FechaDesde.Value.ToString("dd/MM/yyyy") : "N/A";
            string fechaHastaStr = FechaHasta.HasValue ? FechaHasta.Value.ToString("dd/MM/yyyy") : "N/A";

            using (var db = new InventarioDbContext())
            {
                var queryDetalles = db.VentasDetalle
                                    .Include(d => d.Producto)
                                    .ThenInclude(p => p.Subcategoria)
                                    .Include(d => d.Venta)
                                    .AsQueryable();

                // --- 2. Aplicamos filtro de Categoría ---
                if (categoriaId != 0)
                {
                    queryDetalles = queryDetalles.Where(d => d.Producto.Subcategoria.CategoriaId == categoriaId);
                }

                // --- 3. Aplicamos filtro de Fecha ---
                if (FechaDesde != null)
                {
                    queryDetalles = queryDetalles.Where(d => d.Venta.Fecha >= FechaDesde.Value);
                }
                if (FechaHasta != null)
                {
                    var fechaHastaMasUnDia = FechaHasta.Value.AddDays(1);
                    queryDetalles = queryDetalles.Where(d => d.Venta.Fecha < fechaHastaMasUnDia);
                }

                // --- 4. Calculamos ---
                var detallesFiltrados = queryDetalles.ToList();

                IngresosTotales = detallesFiltrados.Sum(d => d.Cantidad * d.PrecioUnitario);
                try
                {
                    GananciaTotal = detallesFiltrados.Sum(d => d.Cantidad * (d.PrecioUnitario - d.Producto.Costo));
                }
                catch (Exception) { GananciaTotal = 0; }

                NumVentas = detallesFiltrados.Select(d => d.VentaId).Distinct().Count();
                TicketPromedio = (NumVentas > 0) ? (IngresosTotales / NumVentas) : 0;

                // --- 5. ¡AHORA SÍ SE ASIGNA EL TÍTULO! ---
                ResumenTitulo = $"Resumen para: {nombreCategoria} ({fechaDesdeStr} - {fechaHastaStr})";
            }
        }

        // --- REEMPLAZA TU MÉTODO CON ESTE ---
        public void CargarHistorialVentas()
        {
            // --- ¡LA GUARDIA PRIMERO! ---
            // Si los filtros aún no se han cargado, no hacemos nada.
            if (ClienteSeleccionado == null || SelectedCategoria == null)
            {
                return;
            }
            // --- FIN DE LA GUARDIA ---

            Ventas.Clear();

            using (var db = new InventarioDbContext())
            {
                var query = db.Ventas.AsQueryable();

                // --- CORRECCIÓN DEL FILTRO CLIENTE ---
                if (ClienteSeleccionado.ID == -999)
                {
                    // Si eligió "Público General", buscamos los que tienen NULL
                    query = query.Where(v => v.ClienteId == null);
                }
                else if (ClienteSeleccionado.ID != 0)
                {
                    // Si es un cliente normal, buscamos por su ID
                    query = query.Where(v => v.ClienteId == ClienteSeleccionado.ID);
                }

                // --- 2. FILTRO BÚSQUEDA ---
                if (!string.IsNullOrWhiteSpace(TextoBusqueda) &&
                TextoBusqueda != PlaceholderBusqueda)
                {
                    string busquedaLower = TextoBusqueda.ToLower().Trim();
                    if (int.TryParse(busquedaLower, out int folio))
                    {
                        query = query.Where(v => v.VentaId == folio);
                    }
                    else if ("público general".Contains(busquedaLower))
                    {
                        query = query.Where(v => v.ClienteId == null);
                    }
                    else
                    {
                        query = query.Where(v => v.Cliente != null &&
                                                 v.Cliente.RazonSocial.ToLower().Contains(busquedaLower));
                    }
                }

                // --- 3. FILTRO FECHA ---
                if (FechaDesde != null)
                {
                    query = query.Where(v => v.Fecha >= FechaDesde.Value);
                }
                if (FechaHasta != null)
                {
                    var fechaHastaMasUnDia = FechaHasta.Value.AddDays(1);
                    query = query.Where(v => v.Fecha < fechaHastaMasUnDia);
                }

                // --- 4. EJECUTAR CONSULTA ---
                var ventasDb = query
                    .Include(v => v.Detalles)
                    .Include(v => v.Cliente)
                    .OrderByDescending(v => v.Fecha)
                    .ToList();

                // --- 5. CONSTRUIR LISTA ---
                foreach (var venta in ventasDb)
                {
                    string nombreCliente = (venta.Cliente == null) ? "Público General" : venta.Cliente.RazonSocial;
                    Ventas.Add(new VentaHistorialItem
                    {
                        Folio = venta.VentaId,
                        Cliente = nombreCliente,
                        FechaHora = venta.Fecha,
                        Productos = venta.Detalles.Count(),
                        Total = venta.Total
                    });
                }

                TotalVentas = Ventas.Count;
            }
        }
        // --- ¡NUEVO MÉTODO! ---
        private void OnVerDetalle(object parametro)
        {
            // 1. El 'parametro' es el objeto VentaHistorialItem de la fila
            if (parametro is VentaHistorialItem ventaItem)
            {
                // 2. Obtenemos el Folio (que es el VentaId)
                int folio = ventaItem.Folio;

                // 3. Creamos nuestro nuevo diálogo, pasándole el Folio
                var dialog = new DetalleVentaDialog(folio);

                // (Opcional pero recomendado) Asigna el "dueño" para que se centre
                dialog.Owner = Application.Current.MainWindow;

                // 4. ¡Mostramos el diálogo!
                dialog.ShowDialog();
            }
        }
    }

    // --- ¡IMPORTANTE! MUEVE ESTA CLASE AQUÍ ---
    // (O mejor, a su propio archivo en /Models)
    // Esta clase "traduce" la Venta para el DataGrid
    public class VentaHistorialItem
    {
        public int Folio { get; set; }
        public string Cliente { get; set; }
        public DateTime FechaHora { get; set; }
        public int Productos { get; set; }
        public decimal Total { get; set; }
    }
}