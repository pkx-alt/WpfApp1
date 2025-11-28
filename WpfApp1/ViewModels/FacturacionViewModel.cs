using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using OrySiPOS.Data;
using OrySiPOS.Models;
using OrySiPOS.Helpers; // Para RelayCommand
using System.Text;      // Para StringBuilder (CSV)
using System.IO;        // Para File (Guardar CSV)

namespace OrySiPOS.ViewModels
{
    public class FacturacionViewModel : ViewModelBase
    {
        // ==========================================
        // 1. PROPIEDADES DE LISTAS Y DATOS
        // ==========================================
        public ObservableCollection<TicketPendienteItem> ListaPendientes { get; set; }
        public ObservableCollection<FacturaHistorialItem> ListaHistorial { get; set; }
        public ObservableCollection<Cliente> ListaClientes { get; set; }

        // --- NUEVO: INTEGRACIÓN CATÁLOGO SAT ---
        // Aquí "inyectamos" el ViewModel del buscador que creaste antes
        public SatCatalogViewModel BuscadorSat { get; set; }

        // Lista de tus productos locales que les falta clave
        public ObservableCollection<Producto> ProductosSinClave { get; set; }

        // ==========================================
        // 2. PROPIEDADES DE SELECCIÓN Y FILTROS
        // ==========================================

        // --- FILTROS HISTORIAL ---
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
                CargarHistorial();
            }
        }

        // --- CONTADORES ---
        private int _totalPendientes;
        public int TotalPendientes
        {
            get { return _totalPendientes; }
            set { _totalPendientes = value; OnPropertyChanged(); }
        }

        private int _totalHistorial;
        public int TotalHistorial
        {
            get { return _totalHistorial; }
            set { _totalHistorial = value; OnPropertyChanged(); }
        }

        // --- FILTROS ESTADO ---
        public ObservableCollection<string> ListaEstados { get; set; } = new ObservableCollection<string> { "Todas", "Vigente", "Cancelada" };

        private string _estadoSeleccionadoHistorial;
        public string EstadoSeleccionadoHistorial
        {
            get => _estadoSeleccionadoHistorial;
            set
            {
                _estadoSeleccionadoHistorial = value;
                OnPropertyChanged();
                CargarHistorial();
            }
        }

        private Cliente _clienteFiltroHistorial;
        public Cliente ClienteFiltroHistorial
        {
            get => _clienteFiltroHistorial;
            set
            {
                _clienteFiltroHistorial = value;
                OnPropertyChanged();
                CargarHistorial();
            }
        }

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

        // --- SELECCIONES PARA ASIGNACIÓN SAT (MATCHMAKER) ---
        private Producto _productoLocalSeleccionado;
        public Producto ProductoLocalSeleccionado
        {
            get => _productoLocalSeleccionado;
            set
            {
                _productoLocalSeleccionado = value;
                OnPropertyChanged();
                // Validamos si el botón de asignar se activa
                (AsignarClaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        private SatProducto _claveSatSeleccionada;
        public SatProducto ClaveSatSeleccionada
        {
            get => _claveSatSeleccionada;
            set
            {
                _claveSatSeleccionada = value;
                OnPropertyChanged();
                (AsignarClaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        // ==========================================
        // 3. COMANDOS
        // ==========================================
        public ICommand FacturarTicketCommand { get; }
        public ICommand GenerarGlobalCommand { get; }
        public ICommand RefrescarCommand { get; }
        public ICommand BuscarHistorialCommand { get; }
        public ICommand AsignarClaveCommand { get; } // <--- Nuevo comando SAT

        // ==========================================
        // 4. CONSTRUCTOR
        // ==========================================
        public FacturacionViewModel()
        {
            // Inicializar listas
            ListaPendientes = new ObservableCollection<TicketPendienteItem>();
            ListaHistorial = new ObservableCollection<FacturaHistorialItem>();
            ListaClientes = new ObservableCollection<Cliente>();
            ProductosSinClave = new ObservableCollection<Producto>(); // <--- Lista SAT

            // Inicializar el ViewModel hijo (SAT)
            BuscadorSat = new SatCatalogViewModel();

            // Configurar Comandos
            FacturarTicketCommand = new RelayCommand(FacturarIndividual);
            GenerarGlobalCommand = new RelayCommand(GenerarGlobal);
            RefrescarCommand = new RelayCommand(p => CargarDatosIniciales());
            BuscarHistorialCommand = new RelayCommand(p => CargarHistorial());
            AsignarClaveCommand = new RelayCommand(AsignarClave, PuedeAsignarClave);

            // Valores por defecto
            MesSeleccionadoHistorial = "Todos";
            EstadoSeleccionadoHistorial = "Todas";

            // Cargas iniciales
            CargarDatosIniciales();
            CargarHistorial();
            CargarProductosSinClave(); // <--- Carga inicial de productos con problemas SAT
        }

        // ==========================================
        // 5. MÉTODOS DE LÓGICA
        // ==========================================

        private void CargarDatosIniciales()
        {
            using (var db = new InventarioDbContext())
            {
                // 1. Cargar Clientes
                var clientes = db.Clientes.Where(c => c.Activo).OrderBy(c => c.RazonSocial).ToList();
                ListaClientes.Clear();
                ListaClientes.Add(new Cliente { ID = 0, RazonSocial = "Todos los clientes" });
                foreach (var c in clientes) ListaClientes.Add(c);

                if (ClienteFiltroPendiente == null) ClienteFiltroPendiente = ListaClientes.First();
                if (ClienteFiltroHistorial == null) ClienteFiltroHistorial = ListaClientes.First();

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
                    .Where(v => !ventasFacturadasIds.Contains(v.VentaId))
                    .AsQueryable();

                // Filtros
                if (!string.IsNullOrWhiteSpace(TextoBusquedaPendiente) && TextoBusquedaPendiente != "Buscar folio...")
                {
                    if (int.TryParse(TextoBusquedaPendiente, out int folio))
                        query = query.Where(v => v.VentaId == folio);
                }

                if (ClienteFiltroPendiente != null && ClienteFiltroPendiente.ID != 0)
                {
                    query = query.Where(v => v.ClienteId == ClienteFiltroPendiente.ID);
                }

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
                TotalPendientes = ListaPendientes.Count;
            }
        }

        private void CargarHistorial()
        {
            using (var db = new InventarioDbContext())
            {
                var query = db.Facturas.AsQueryable();

                if (!string.IsNullOrEmpty(EstadoSeleccionadoHistorial) && EstadoSeleccionadoHistorial != "Todas")
                    query = query.Where(f => f.Estado == EstadoSeleccionadoHistorial);

                if (ClienteFiltroHistorial != null && ClienteFiltroHistorial.ID != 0)
                    query = query.Where(f => f.ReceptorNombre == ClienteFiltroHistorial.RazonSocial);

                if (!string.IsNullOrEmpty(MesSeleccionadoHistorial) && MesSeleccionadoHistorial != "Todos")
                {
                    int numeroMes = ListaMeses.IndexOf(MesSeleccionadoHistorial);
                    if (numeroMes > 0)
                        query = query.Where(f => f.FechaEmision.Month == numeroMes && f.FechaEmision.Year == DateTime.Now.Year);
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
                TotalHistorial = ListaHistorial.Count;
            }
        }

        // --- LÓGICA NUEVA: ASIGNACIÓN DE CLAVES SAT ---

        private void CargarProductosSinClave()
        {
            using (var db = new InventarioDbContext())
            {
                // Buscamos productos activos con clave genérica o vacía
                var pendientes = db.Productos
                    .Where(p => p.Activo && (p.ClaveSat == "01010101" || string.IsNullOrEmpty(p.ClaveSat)))
                    .OrderBy(p => p.Descripcion)
                    .ToList();

                ProductosSinClave.Clear();
                foreach (var p in pendientes) ProductosSinClave.Add(p);
            }
        }

        private bool PuedeAsignarClave(object param)
        {
            return ProductoLocalSeleccionado != null && ClaveSatSeleccionada != null;
        }

        private void AsignarClave(object param)
        {
            try
            {
                using (var db = new InventarioDbContext())
                {
                    var productoDb = db.Productos.Find(ProductoLocalSeleccionado.ID);
                    if (productoDb != null)
                    {
                        productoDb.ClaveSat = ClaveSatSeleccionada.Clave;
                        db.SaveChanges();

                        // Quitar de la lista visual
                        ProductosSinClave.Remove(ProductoLocalSeleccionado);

                        // Limpiar selección
                        ProductoLocalSeleccionado = null;
                        ClaveSatSeleccionada = null;

                        MessageBox.Show("¡Producto actualizado correctamente!", "Éxito");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al asignar clave: " + ex.Message);
            }
        }

        // --- LÓGICA DE FACTURACIÓN (SIMULADA & EXPORTACIÓN) ---

        private void FacturarIndividual(object parameter)
        {
            if (parameter is TicketPendienteItem ticket)
            {
                var confirm = MessageBox.Show($"¿Generar factura para el ticket #{ticket.Folio}?\n\nCliente: {ticket.ClienteNombre}\nTotal: {ticket.Total:C}",
                                              "Confirmar Facturación", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes) return;

                try
                {
                    using (var db = new InventarioDbContext())
                    {
                        var ventaExiste = db.Ventas.FirstOrDefault(v => v.VentaId == ticket.VentaId);
                        if (ventaExiste == null) return;

                        string uuidSimulado = Guid.NewGuid().ToString().ToUpper();
                        string folioInterno = "F-" + (db.Facturas.Count() + 1).ToString("D4");

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
                            ArchivoXml = "ruta/ficticia/factura.xml",
                            ArchivoPdf = "ruta/ficticia/factura.pdf"
                        };

                        db.Facturas.Add(nuevaFactura);
                        db.SaveChanges();

                        MessageBox.Show($"¡Factura Generada Exitosamente!\nFolio: {folioInterno}\nUUID: {uuidSimulado}", "Éxito");

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

        private void GenerarGlobal(object parameter)
        {
            try
            {
                using (var db = new InventarioDbContext())
                {
                    // 1. Definir rango
                    DateTime fechaFin = DateTime.Now.Date.AddDays(1).AddTicks(-1);
                    DateTime fechaInicio = DateTime.Today;

                    switch (IndicePeriodoGlobal)
                    {
                        case 0: fechaInicio = DateTime.Today; break;
                        case 1: fechaInicio = DateTime.Today.AddDays(-7); break;
                        case 2: fechaInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1); break;
                    }

                    // 2. Buscar ventas NO facturadas de Público General
                    var ventasFacturadasIds = db.Facturas.Select(f => f.VentaId).ToList();
                    var ventasParaGlobal = db.Ventas
                        .Where(v => v.Fecha >= fechaInicio && v.Fecha <= fechaFin)
                        .Where(v => !ventasFacturadasIds.Contains(v.VentaId))
                        .Where(v => v.ClienteId == null || v.ClienteId == 1) // Ajusta ID si tienes uno específico
                        .ToList();

                    if (ventasParaGlobal.Count == 0)
                    {
                        MessageBox.Show("No se encontraron ventas pendientes de facturar en este periodo.", "Sin datos");
                        return;
                    }

                    // 3. Exportar CSV para el Contador
                    Microsoft.Win32.SaveFileDialog saveDialog = new Microsoft.Win32.SaveFileDialog
                    {
                        Filter = "Excel (CSV)|*.csv",
                        FileName = $"PARA_CONTADOR_Global_{DateTime.Now:yyyy-MM-dd}.csv",
                        Title = "Guardar desglose para Factura Global"
                    };

                    if (saveDialog.ShowDialog() == true)
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine("Folio Ticket,Fecha Hora,Forma Pago,Subtotal,IVA,Total");

                        foreach (var venta in ventasParaGlobal)
                        {
                            sb.AppendLine($"{venta.VentaId},{venta.Fecha:yyyy-MM-dd HH:mm},{venta.FormaPagoSAT},{venta.Subtotal:F2},{venta.IVA:F2},{venta.Total:F2}");
                        }

                        sb.AppendLine(",,,,,,");
                        sb.AppendLine($",,TOTALES,{ventasParaGlobal.Sum(v => v.Subtotal):F2},{ventasParaGlobal.Sum(v => v.IVA):F2},{ventasParaGlobal.Sum(v => v.Total):F2}");

                        File.WriteAllText(saveDialog.FileName, sb.ToString(), Encoding.UTF8);

                        // 4. Registrar en Sistema (Cerrar ventas)
                        decimal totalGlobal = ventasParaGlobal.Sum(v => v.Total);

                        // Creamos Venta Agrupadora (Contenedor)
                        var ventaGlobal = new Venta
                        {
                            Fecha = DateTime.Now,
                            Total = totalGlobal,
                            Subtotal = totalGlobal / 1.16m,
                            IVA = totalGlobal - (totalGlobal / 1.16m),
                            ClienteId = null,
                            PagoRecibido = totalGlobal,
                            Cambio = 0,
                            FormaPagoSAT = "01",
                            MetodoPagoSAT = "PUE",
                            Moneda = "MXN"
                        };

                        db.Ventas.Add(ventaGlobal);
                        db.SaveChanges();

                        // Creamos Factura (Simulada)
                        string folioInterno = "FG-" + (db.Facturas.Count() + 1).ToString("D4");
                        string uuidSimulado = Guid.NewGuid().ToString().ToUpper();

                        var nuevaFactura = new Factura
                        {
                            VentaId = ventaGlobal.VentaId,
                            FechaEmision = DateTime.Now,
                            ReceptorNombre = "PUBLICO EN GENERAL",
                            ReceptorRFC = "XAXX010101000",
                            Total = totalGlobal,
                            Estado = "Vigente",
                            SerieFolio = folioInterno,
                            UUID = uuidSimulado,
                            ArchivoXml = "N/A",
                            ArchivoPdf = "N/A"
                        };

                        db.Facturas.Add(nuevaFactura);
                        db.SaveChanges();

                        MessageBox.Show($"¡Proceso completado!\n\n1. Archivo generado: {saveDialog.FileName}\n2. Ventas marcadas como facturadas (Folio {folioInterno}).", "Éxito");

                        CargarPendientes();
                        CargarHistorial();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un error: {ex.Message}", "Error");
            }
        }
    }

    // ==========================================
    // 6. CLASES AUXILIARES (Items Visuales)
    // ==========================================

    public class TicketPendienteItem : ViewModelBase
    {
        public int VentaId { get; set; }
        public string Folio { get; set; }
        public DateTime Fecha { get; set; }
        public string ClienteNombre { get; set; }
        public string RFC { get; set; }
        public decimal Total { get; set; }

        // Validación visual para avisar si faltan datos fiscales
        public bool DatosCompletos => !string.IsNullOrEmpty(RFC) && RFC.Length >= 12;

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
        public string Estado { get; set; }
    }
}