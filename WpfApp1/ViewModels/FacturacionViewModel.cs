using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using WpfApp1.Data;
using WpfApp1.Models;
using WpfApp1.Helpers; // Para RelayCommand

namespace WpfApp1.ViewModels
{
    public class FacturacionViewModel : ViewModelBase
    {
        // --- COLECCIONES ---
        public ObservableCollection<TicketPendienteItem> ListaPendientes { get; set; }
        public ObservableCollection<FacturaHistorialItem> ListaHistorial { get; set; }
        public ObservableCollection<Cliente> ListaClientes { get; set; }

        // --- FILTROS HISTORIAL ---

        // 1. Filtro por Mes (Simple: usaremos un entero 1-12, 0 para "Todos")
        public ObservableCollection<string> ListaMeses { get; set; } = new ObservableCollection<string>
{
    "Todos", "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
    "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"
};
        private int _indicePeriodoGlobal;
        public int IndicePeriodoGlobal
        {
            get => _indicePeriodoGlobal;
            set { _indicePeriodoGlobal = value; OnPropertyChanged(); }
        }
        private string _mesSeleccionadoHistorial;
        public string MesSeleccionadoHistorial
        {
            get => _mesSeleccionadoHistorial;
            set
            {
                _mesSeleccionadoHistorial = value;
                OnPropertyChanged();
                CargarHistorial(); // <--- ¡El truco! Recarga al cambiar
            }
        }

        // 2. Filtro por Estado ("Todas", "Vigentes", "Canceladas")
        public ObservableCollection<string> ListaEstados { get; set; } = new ObservableCollection<string> { "Todas", "Vigente", "Cancelada" };

        private string _estadoSeleccionadoHistorial;
        public string EstadoSeleccionadoHistorial
        {
            get => _estadoSeleccionadoHistorial;
            set
            {
                _estadoSeleccionadoHistorial = value;
                OnPropertyChanged();
                CargarHistorial(); // <--- ¡El truco! Recarga al cambiar
            }
        }
        // 3. Filtro por Cliente (Reutilizamos la ListaClientes que ya tienes)
        private Cliente _clienteFiltroHistorial;
        public Cliente ClienteFiltroHistorial
        {
            get => _clienteFiltroHistorial;
            set
            {
                _clienteFiltroHistorial = value;
                OnPropertyChanged();
                CargarHistorial(); // <--- ¡El truco! Recarga al cambiar
            }
        }
        // Comando para el botón "Buscar"
        public ICommand BuscarHistorialCommand { get; }

        // --- FILTROS PENDIENTES ---
        private string _textoBusquedaPendiente;
        public string TextoBusquedaPendiente
        {
            get => _textoBusquedaPendiente;
            set { _textoBusquedaPendiente = value; OnPropertyChanged(); CargarPendientes(); }
        }

        private Cliente _clienteFiltroPendiente;
        public Cliente ClienteFiltroPendiente
        {
            get => _clienteFiltroPendiente;
            set { _clienteFiltroPendiente = value; OnPropertyChanged(); CargarPendientes(); }
        }

        // --- COMANDOS ---
        public ICommand FacturarTicketCommand { get; }
        public ICommand GenerarGlobalCommand { get; }
        public ICommand RefrescarCommand { get; }

        public FacturacionViewModel()
        {
            ListaPendientes = new ObservableCollection<TicketPendienteItem>();
            ListaHistorial = new ObservableCollection<FacturaHistorialItem>();
            ListaClientes = new ObservableCollection<Cliente>();

            // Comandos (lógica vacía por ahora)
            FacturarTicketCommand = new RelayCommand(FacturarIndividual);
            GenerarGlobalCommand = new RelayCommand(GenerarGlobal);
            RefrescarCommand = new RelayCommand(p => CargarDatosIniciales());
            // Inicializar filtros historial
            MesSeleccionadoHistorial = "Todos";
            EstadoSeleccionadoHistorial = "Todas";
            // ClienteFiltroHistorial se iniciará con el primero de la lista automáticamente si usamos la misma lógica

            // Conectar el comando nuevo
            BuscarHistorialCommand = new RelayCommand(p => CargarHistorial());

            CargarDatosIniciales();
            CargarHistorial(); // <--- ¡AGREGA ESTA LÍNEA!
        }

        private void CargarDatosIniciales()
        {
            using (var db = new InventarioDbContext())
            {
                // 1. Cargar Clientes
                var clientes = db.Clientes.Where(c => c.Activo).OrderBy(c => c.RazonSocial).ToList();
                ListaClientes.Clear();
                ListaClientes.Add(new Cliente { ID = 0, RazonSocial = "Todos los clientes" });
                foreach (var c in clientes) ListaClientes.Add(c);

                // Seleccionamos "Todos" por defecto para PENDIENTES
                if (ClienteFiltroPendiente == null)
                    ClienteFiltroPendiente = ListaClientes.First();

                // --- ¡AGREGA ESTO! ---
                // Seleccionamos "Todos" por defecto para HISTORIAL
                if (ClienteFiltroHistorial == null)
                    ClienteFiltroHistorial = ListaClientes.First();

                // 2. Cargar Tickets
                CargarPendientes();
            }
        }

        private void CargarPendientes()
        {
            using (var db = new InventarioDbContext())
            {
                var ventasFacturadasIds = db.Facturas.Select(f => f.VentaId).ToList();

                var query = db.Ventas
                    .Include(v => v.Cliente)
                    .Where(v => !ventasFacturadasIds.Contains(v.VentaId)) // <--- ¡FILTRO NUEVO! "Que NO esté en las facturadas"
                    .AsQueryable();

                // --- FILTROS ---

                // 1. Filtro de Texto (Folio)
                if (!string.IsNullOrWhiteSpace(TextoBusquedaPendiente) && TextoBusquedaPendiente != "Buscar folio...")
                {
                    // Si es número, busca por ID de Venta
                    if (int.TryParse(TextoBusquedaPendiente, out int folio))
                    {
                        query = query.Where(v => v.VentaId == folio);
                    }
                }

                // 2. Filtro de Cliente
                if (ClienteFiltroPendiente != null && ClienteFiltroPendiente.ID != 0)
                {
                    query = query.Where(v => v.ClienteId == ClienteFiltroPendiente.ID);
                }

                // Ejecutamos consulta (Traemos las últimas 50 para no saturar)
                var ventasDb = query.OrderByDescending(v => v.Fecha).Take(50).ToList();

                ListaPendientes.Clear();
                foreach (var v in ventasDb)
                {
                    ListaPendientes.Add(new TicketPendienteItem
                    {
                        VentaId = v.VentaId,
                        Folio = v.VentaId.ToString(),
                        Fecha = v.Fecha,
                        ClienteNombre = v.Cliente != null ? v.Cliente.RazonSocial : "Público en General",
                        RFC = v.Cliente != null ? v.Cliente.RFC : "XAXX010101000",
                        Total = v.Total,
                        Seleccionado = false
                    });
                }
            }
        }

        // --- MÉTODOS DE ACCIÓN (STUBS) ---
        private void FacturarIndividual(object parameter)
        {
            if (parameter is TicketPendienteItem ticket)
            {
                // 1. Preguntar confirmación (opcional pero recomendado)
                var confirm = MessageBox.Show($"¿Generar factura para el ticket #{ticket.Folio}?\n\nCliente: {ticket.ClienteNombre}\nTotal: {ticket.Total:C}",
                                              "Confirmar Facturación",
                                              MessageBoxButton.YesNo,
                                              MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes) return;

                try
                {
                    using (var db = new InventarioDbContext())
                    {
                        // 2. Verificar que la venta exista y no esté facturada ya (seguridad)
                        var ventaExiste = db.Ventas.FirstOrDefault(v => v.VentaId == ticket.VentaId);
                        if (ventaExiste == null) return;

                        // (Aquí iría la llamada al PAC real para timbrar. Nosotros la simularemos)
                        string uuidSimulado = Guid.NewGuid().ToString().ToUpper(); // Generamos un folio "falso"
                        string folioInterno = "F-" + (db.Facturas.Count() + 1).ToString("D4"); // F-0001

                        // 3. Crear el objeto Factura
                        var nuevaFactura = new Factura
                        {
                            VentaId = ticket.VentaId,
                            FechaEmision = DateTime.Now,
                            ReceptorNombre = ticket.ClienteNombre,
                            ReceptorRFC = ticket.RFC,
                            Total = ticket.Total,
                            Estado = "Vigente",
                            UUID = uuidSimulado,
                            SerieFolio = folioInterno,
                            ArchivoXml = "ruta/ficticia/factura.xml", // Pendiente implementar generación de archivos
                            ArchivoPdf = "ruta/ficticia/factura.pdf"
                        };

                        // 4. Guardar en BD
                        db.Facturas.Add(nuevaFactura);
                        db.SaveChanges();

                        // 5. Feedback visual
                        MessageBox.Show($"¡Factura Generada Exitosamente!\nFolio: {folioInterno}\nUUID: {uuidSimulado}", "Éxito");

                        // 6. Refrescar las listas
                        // El ticket ya no debería salir en pendientes si filtramos las ya facturadas.
                        // (Para esto necesitamos un pequeño ajuste en 'CargarPendientes', ver abajo).
                        CargarPendientes();
                        CargarHistorial();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al facturar: " + ex.Message);
                }
            }
        }

        // 2. EL MÉTODO CON LÓGICA REAL
        private void GenerarGlobal(object parameter)
        {
            try
            {
                using (var db = new InventarioDbContext())
                {
                    // A. Definir el rango de fechas según lo que seleccionaste en el ComboBox
                    DateTime fechaFin = DateTime.Now.Date.AddDays(1).AddTicks(-1); // Final de hoy
                    DateTime fechaInicio;

                    switch (IndicePeriodoGlobal)
                    {
                        case 0: // Del Día
                            fechaInicio = DateTime.Today;
                            break;
                        case 1: // Semanal (últimos 7 días)
                            fechaInicio = DateTime.Today.AddDays(-7);
                            break;
                        case 2: // Mensual (día 1 del mes actual)
                            fechaInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                            break;
                        default:
                            fechaInicio = DateTime.Today;
                            break;
                    }

                    // B. Buscar ventas NO facturadas de "Público en General"
                    //    (Asumimos que Público General es cuando ClienteId es nulo o es el cliente genérico)

                    // Primero obtenemos los IDs de ventas que YA tienen factura para excluirlas
                    var ventasFacturadasIds = db.Facturas.Select(f => f.VentaId).ToList();

                    var ventasParaGlobal = db.Ventas
                        .Where(v => v.Fecha >= fechaInicio && v.Fecha <= fechaFin)
                        .Where(v => !ventasFacturadasIds.Contains(v.VentaId)) // Que no estén facturadas
                        .Where(v => v.ClienteId == null || v.ClienteId == 1)  // AJUSTA ESTE "1" al ID de tu cliente Público General si es diferente
                        .ToList();

                    if (ventasParaGlobal.Count == 0)
                    {
                        MessageBox.Show("No se encontraron ventas pendientes de facturar en este periodo.", "Sin datos");
                        return;
                    }

                    decimal totalGlobal = ventasParaGlobal.Sum(v => v.Total);

                    var confirm = MessageBox.Show(
                        $"Se encontraron {ventasParaGlobal.Count} ventas por un total de {totalGlobal:C}.\n" +
                        $"¿Deseas generar la Factura Global ahora?",
                        "Confirmar Global",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (confirm != MessageBoxResult.Yes) return;

                    // C. Truco Técnico: Como tu tabla Facturas pide UN VentaId, 
                    // creamos una "Venta Agrupadora" para representar este conjunto.

                    var ventaGlobal = new Venta
                    {
                        Fecha = DateTime.Now,
                        Total = totalGlobal,
                        Subtotal = totalGlobal / 1.16m, // Asumiendo IVA 16%
                        IVA = totalGlobal - (totalGlobal / 1.16m),
                        ClienteId = null, // O el ID de Público General
                        PagoRecibido = totalGlobal,
                        Cambio = 0,
                        FormaPagoSAT = "01", // Efectivo (lo más común en global)
                        MetodoPagoSAT = "PUE",
                        Moneda = "MXN"
                        // Nota: No le agregamos 'Detalles' para no duplicar el inventario
                    };

                    db.Ventas.Add(ventaGlobal);
                    db.SaveChanges(); // Guardamos para obtener el ID nuevo

                    // D. Crear la Factura enlazada a esa Venta Global
                    string folioInterno = "FG-" + (db.Facturas.Count() + 1).ToString("D4");
                    string uuidSimulado = Guid.NewGuid().ToString().ToUpper();

                    var nuevaFactura = new Factura
                    {
                        VentaId = ventaGlobal.VentaId, // Enlazamos a la venta contenedora
                        FechaEmision = DateTime.Now,
                        ReceptorNombre = "PUBLICO EN GENERAL",
                        ReceptorRFC = "XAXX010101000",
                        Total = totalGlobal,
                        Estado = "Vigente",
                        SerieFolio = folioInterno,
                        UUID = uuidSimulado,
                        ArchivoXml = "ruta/ficticia/global.xml",
                        ArchivoPdf = "ruta/ficticia/global.pdf"
                    };

                    db.Facturas.Add(nuevaFactura);
                    db.SaveChanges();

                    MessageBox.Show($"¡Factura Global {folioInterno} generada exitosamente!", "Éxito");

                    // E. Refrescar las listas
                    CargarPendientes();
                    CargarHistorial(); // Este método ya lo habías agregado antes
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un error: {ex.Message}", "Error");
            }
        }

        private void CargarHistorial()
        {
            using (var db = new InventarioDbContext())
            {
                var query = db.Facturas.AsQueryable();

                // FILTRO 1: Estado
                // Validamos que no sea nulo y que no sea "Todas"
                if (!string.IsNullOrEmpty(EstadoSeleccionadoHistorial) && EstadoSeleccionadoHistorial != "Todas")
                {
                    query = query.Where(f => f.Estado == EstadoSeleccionadoHistorial);
                }

                // FILTRO 2: Cliente
                // Validamos que el objeto cliente no sea nulo y que su ID no sea 0 (Todos)
                if (ClienteFiltroHistorial != null && ClienteFiltroHistorial.ID != 0)
                {
                    query = query.Where(f => f.ReceptorNombre == ClienteFiltroHistorial.RazonSocial);
                }

                // FILTRO 3: Mes
                // Validamos nulo y "Todos"
                if (!string.IsNullOrEmpty(MesSeleccionadoHistorial) && MesSeleccionadoHistorial != "Todos")
                {
                    int numeroMes = ListaMeses.IndexOf(MesSeleccionadoHistorial);
                    if (numeroMes > 0) // Validación extra
                    {
                        query = query.Where(f => f.FechaEmision.Month == numeroMes && f.FechaEmision.Year == DateTime.Now.Year);
                    }
                }

                var facturasDb = query.OrderByDescending(f => f.FechaEmision).ToList();

                ListaHistorial.Clear();
                foreach (var f in facturasDb)
                {
                    ListaHistorial.Add(new FacturaHistorialItem
                    {
                        UUID = f.UUID,
                        SerieFolio = f.SerieFolio,
                        Receptor = f.ReceptorNombre,
                        Total = f.Total,
                        Estado = f.Estado
                    });
                }
            }
        }

        // --- CLASES AUXILIARES (Wrappers para el DataGrid) ---

        public class TicketPendienteItem : ViewModelBase
        {
            public int VentaId { get; set; }
            public string Folio { get; set; }
            public DateTime Fecha { get; set; }
            public string ClienteNombre { get; set; }
            public string RFC { get; set; }
            public decimal Total { get; set; }

            private bool _seleccionado;
            public bool Seleccionado
            {
                get => _seleccionado;
                set { _seleccionado = value; OnPropertyChanged(); }
            }
        }

        public class FacturaHistorialItem
        {
            public string UUID { get; set; }
            public string SerieFolio { get; set; }
            public string Receptor { get; set; }
            public decimal Total { get; set; }
            public string Estado { get; set; } // "Vigente" o "Cancelada"
        }
    }
}