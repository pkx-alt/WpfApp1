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


        // En el constructor:


        // En FacturacionViewModel.cs

        private int _indicePeriodoGlobal;
        public int IndicePeriodoGlobal
        {
            get => _indicePeriodoGlobal;
            set
            {
                _indicePeriodoGlobal = value;
                OnPropertyChanged();

                // ¡AGREGA ESTO!
                // Al cambiar "Diario/Semanal/Mensual", recargamos la lista
                CargarPendientes();
            }
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
        public ICommand AbrirPdfCommand { get; }
        public ICommand CancelarFacturaCommand { get; }
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
            AbrirPdfCommand = new RelayCommand(AbrirPdfDesdeHistorial);
            CancelarFacturaCommand = new RelayCommand(CancelarFactura);
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
                // ---------------------------------------------------------
                // 1. CALCULAR RANGO DE FECHAS (Según el ComboBox)
                // ---------------------------------------------------------
                DateTime fechaFin = DateTime.Now.Date.AddDays(1).AddTicks(-1); // Final del día de hoy
                DateTime fechaInicio = DateTime.Today; // Inicio del día de hoy

                switch (IndicePeriodoGlobal)
                {
                    case 0: // Diario
                        fechaInicio = DateTime.Today;
                        break;
                    case 1: // Semanal (Últimos 7 días)
                        fechaInicio = DateTime.Today.AddDays(-7);
                        break;
                    case 2: // Mensual (Desde el día 1 del mes actual)
                        fechaInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                        break;
                }

                // ---------------------------------------------------------
                // 2. FILTRAR VENTAS YA FACTURADAS
                // ---------------------------------------------------------
                // (Solo ignoramos las que tienen factura VIGENTE, si está cancelada vuelve a salir)
                var ventasFacturadasIds = db.Facturas
                                            .Where(f => f.Estado != "Cancelada")
                                            .Select(f => f.VentaId)
                                            .ToList();

                var query = db.Ventas
                    .Include(v => v.Cliente)
                    .Where(v => !ventasFacturadasIds.Contains(v.VentaId))
                    // AQUI APLICAMOS EL FILTRO DE FECHAS
                    .Where(v => v.Fecha >= fechaInicio && v.Fecha <= fechaFin)
                    .AsQueryable();

                // ---------------------------------------------------------
                // 3. FILTRO POR FOLIO (Búsqueda manual)
                // ---------------------------------------------------------
                if (!string.IsNullOrWhiteSpace(TextoBusquedaPendiente) && TextoBusquedaPendiente != "Buscar folio...")
                {
                    if (int.TryParse(TextoBusquedaPendiente, out int folio))
                        query = query.Where(v => v.VentaId == folio);
                }

                // ---------------------------------------------------------
                // 4. FILTRO POR CLIENTE (Con corrección de Público Gral)
                // ---------------------------------------------------------
                if (ClienteFiltroPendiente != null && ClienteFiltroPendiente.ID != 0)
                {
                    bool esPublicoGeneral = ClienteFiltroPendiente.ID == 1 ||
                                            ClienteFiltroPendiente.RFC == "XAXX010101000";

                    if (esPublicoGeneral)
                    {
                        // ID 1 O Nulos
                        query = query.Where(v => v.ClienteId == ClienteFiltroPendiente.ID || v.ClienteId == null);
                    }
                    else
                    {
                        // Búsqueda exacta para clientes registrados
                        query = query.Where(v => v.ClienteId == ClienteFiltroPendiente.ID);
                    }
                }

                // ---------------------------------------------------------
                // 5. EJECUTAR Y LLENAR LISTA
                // ---------------------------------------------------------
                var ventasDb = query.OrderByDescending(v => v.Fecha).ToList();

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
                var confirm = MessageBox.Show($"¿Generar factura para el ticket #{ticket.Folio}?",
                                              "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes) return;

                try
                {
                    using (var db = new InventarioDbContext())
                    {
                        // 1. RECUPERAR LA VENTA COMPLETA (Con detalles y productos)
                        // Usamos .Include para traer las tablas relacionadas "pegadas" a la consulta
                        var ventaCompleta = db.Ventas
                                              .Include(v => v.Detalles)
                                              .ThenInclude(d => d.Producto) // Por si necesitas datos del producto como la clave SAT
                                              .FirstOrDefault(v => v.VentaId == ticket.VentaId);

                        if (ventaCompleta == null) return;

                        // 2. Preparar datos de la Factura
                        string folioInterno = "F-" + (db.Facturas.Count() + 1).ToString("D4");
                        string uuidSimulado = Guid.NewGuid().ToString().ToUpper();

                        // 3. Crear carpeta si no existe
                        string carpetaFacturas = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Facturas");
                        if (!Directory.Exists(carpetaFacturas)) Directory.CreateDirectory(carpetaFacturas);

                        string nombreArchivo = $"Factura_{folioInterno}.pdf";
                        string rutaCompleta = Path.Combine(carpetaFacturas, nombreArchivo);

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
                            ArchivoXml = "N/A", // Por ahora sin XML real
                            ArchivoPdf = rutaCompleta // <--- ¡AQUÍ GUARDAMOS LA RUTA REAL!
                        };

                        // 4. GENERAR EL PDF FISICAMENTE USANDO EL SERVICIO
                        var pdfService = new OrySiPOS.Services.FacturaPdfService();
                        pdfService.GenerarPdf(ventaCompleta, nuevaFactura, rutaCompleta);

                        // 5. Guardar en BD
                        db.Facturas.Add(nuevaFactura);
                        db.SaveChanges();

                        // 6. Preguntar si quiere abrirla
                        var abrir = MessageBox.Show("Factura generada. ¿Deseas abrirla ahora?", "Éxito", MessageBoxButton.YesNo);
                        if (abrir == MessageBoxResult.Yes)
                        {
                            // Truco para abrir archivos en .NET Core / .NET 5+
                            var p = new System.Diagnostics.Process();
                            p.StartInfo = new System.Diagnostics.ProcessStartInfo(rutaCompleta)
                            {
                                UseShellExecute = true
                            };
                            p.Start();
                        }

                        // Refrescar las listas
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
                    // 1. Definir rango de fechas
                    DateTime fechaFin = DateTime.Now.Date.AddDays(1).AddTicks(-1);
                    DateTime fechaInicio = DateTime.Today;

                    string periodoTexto = "Del Día";
                    switch (IndicePeriodoGlobal)
                    {
                        case 0: fechaInicio = DateTime.Today; periodoTexto = "Del Día"; break;
                        case 1: fechaInicio = DateTime.Today.AddDays(-7); periodoTexto = "Semanal"; break;
                        case 2: fechaInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1); periodoTexto = "Mensual"; break;
                    }

                    // 2. Buscar ventas NO facturadas
                    var ventasFacturadasIds = db.Facturas
                            .Where(f => f.Estado != "Cancelada") // Solo las vigentes cuentan
                            .Select(f => f.VentaId)
                            .ToList();

                    var ventasParaGlobal = db.Ventas
                        .Where(v => v.Fecha >= fechaInicio && v.Fecha <= fechaFin)
                        .Where(v => !ventasFacturadasIds.Contains(v.VentaId)) //
                                                                              // ❌ BORRA O COMENTA ESTA LÍNEA:
                                                                              // .Where(v => v.ClienteId == null || v.ClienteId == 1) 
                        .ToList();

                    if (ventasParaGlobal.Count == 0)
                    {
                        MessageBox.Show("No se encontraron ventas pendientes para el global.", "Sin datos");
                        return;
                    }

                    // 3. Exportar CSV (Mantenemos tu lógica original, es muy útil)
                    Microsoft.Win32.SaveFileDialog saveDialog = new Microsoft.Win32.SaveFileDialog
                    {
                        Filter = "Excel (CSV)|*.csv",
                        FileName = $"GLOBAL_{DateTime.Now:yyyy-MM-dd}.csv",
                        Title = "Guardar desglose para Contador"
                    };

                    if (saveDialog.ShowDialog() == true)
                    {
                        // ... (Tu código de generación de CSV se queda igual aquí) ...
                        var sb = new StringBuilder();
                        sb.AppendLine("Folio,Fecha,Monto,IVA,Total");
                        foreach (var v in ventasParaGlobal)
                            sb.AppendLine($"{v.VentaId},{v.Fecha:dd/MM},{v.Subtotal:F2},{v.IVA:F2},{v.Total:F2}");
                        File.WriteAllText(saveDialog.FileName, sb.ToString(), Encoding.UTF8);

                        // CORRECCIÓN SUGERIDA:
                        decimal totalGlobal = ventasParaGlobal.Sum(v => v.Total);
                        decimal subtotalGlobal = ventasParaGlobal.Sum(v => v.Subtotal); // Suma directa de subtotales
                        decimal ivaGlobal = ventasParaGlobal.Sum(v => v.IVA);           // Suma directa de IVAs

                        var ventaGlobal = new Venta
                        {
                            Fecha = DateTime.Now,
                            Total = totalGlobal,
                            Subtotal = subtotalGlobal,
                            IVA = ivaGlobal,
                            ClienteId = null, // Público General
                            PagoRecibido = totalGlobal,
                            Cambio = 0,
                            FormaPagoSAT = "01",
                            MetodoPagoSAT = "PUE",
                            Moneda = "MXN"
                        };

                        db.Ventas.Add(ventaGlobal);
                        db.SaveChanges(); // Guardamos para obtener el ID

                        // 5. Preparar PDF de la Global
                        string folioInterno = "FG-" + (db.Facturas.Count() + 1).ToString("D4");
                        string uuidSimulado = Guid.NewGuid().ToString().ToUpper();

                        string carpetaFacturas = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Facturas");
                        if (!Directory.Exists(carpetaFacturas)) Directory.CreateDirectory(carpetaFacturas);
                        string rutaPdf = Path.Combine(carpetaFacturas, $"Global_{folioInterno}.pdf");

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
                            ArchivoPdf = rutaPdf // <--- ¡Ahora sí guardamos la ruta!
                        };

                        // TRUCO: Creamos una venta "falsa" en memoria solo para que el PDF salga bonito
                        // (No la guardamos en BD, solo se la pasamos al generador)
                        var ventaParaPdf = new Venta
                        {
                            Subtotal = ventaGlobal.Subtotal,
                            IVA = ventaGlobal.IVA,
                            Total = ventaGlobal.Total,
                            Detalles = new System.Collections.Generic.List<VentaDetalle>
                    {
                        new VentaDetalle
                        {
                            Cantidad = 1,
                            Descripcion = $"Venta Global ({periodoTexto}) - {ventasParaGlobal.Count} Tickets",
                            PrecioUnitario = ventaGlobal.Subtotal
                        }
                    }
                        };

                        // Generar el PDF
                        var pdfService = new OrySiPOS.Services.FacturaPdfService();
                        pdfService.GenerarPdf(ventaParaPdf, nuevaFactura, rutaPdf);

                        // Guardar Factura en BD
                        db.Facturas.Add(nuevaFactura);
                        db.SaveChanges();

                        // 6. Mensaje final y abrir archivos
                        var result = MessageBox.Show($"¡Global Generada!\n\nSe creó el CSV y el PDF.\n¿Deseas abrir el PDF ahora?",
                                                     "Éxito", MessageBoxButton.YesNo);

                        if (result == MessageBoxResult.Yes)
                        {
                            new System.Diagnostics.Process
                            {
                                StartInfo = new System.Diagnostics.ProcessStartInfo(rutaPdf) { UseShellExecute = true }
                            }.Start();
                        }

                        CargarPendientes();
                        CargarHistorial();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error");
            }
        }

        private void AbrirPdfDesdeHistorial(object param)
        {
            if (param is FacturaHistorialItem item)
            {
                // Necesitamos buscar la ruta en la BD porque el item del historial es ligero
                using (var db = new InventarioDbContext())
                {
                    var factura = db.Facturas.FirstOrDefault(f => f.UUID == item.UUID);
                    if (factura != null && File.Exists(factura.ArchivoPdf))
                    {
                        new System.Diagnostics.Process
                        {
                            StartInfo = new System.Diagnostics.ProcessStartInfo(factura.ArchivoPdf) { UseShellExecute = true }
                        }.Start();
                    }
                    else
                    {
                        MessageBox.Show("El archivo PDF no se encuentra en la ruta guardada.");
                    }
                }
            }
        }

        // Método:
        private void CancelarFactura(object param)
        {
            if (param is FacturaHistorialItem item)
            {
                if (MessageBox.Show("¿Seguro que deseas cancelar esta factura (Internamente)?", "Confirmar", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    using (var db = new InventarioDbContext())
                    {
                        var factura = db.Facturas.FirstOrDefault(f => f.UUID == item.UUID);
                        if (factura != null)
                        {
                            factura.Estado = "Cancelada";

                            // IMPORTANTE: Si es una factura individual, liberar la venta original
                            // para que se pueda volver a facturar.
                            // (Esto solo si NO usas la lógica de Global, pero como ya vimos, 
                            //  la individual liga por VentaId, así que al cancelar, 
                            //  basta con que tu filtro de pendientes cheque el estado de la factura).

                            db.SaveChanges();
                            CargarHistorial();
                            CargarPendientes(); // Para que la venta vuelva a aparecer disponible
                        }
                    }
                }
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