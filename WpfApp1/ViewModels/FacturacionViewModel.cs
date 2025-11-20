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
                ListaClientes.Add(new Cliente { ID = 0, RazonSocial = "Todos los clientes" }); // Opción por defecto
                foreach (var c in clientes) ListaClientes.Add(c);

                // Seleccionamos "Todos" por defecto si no hay nada seleccionado
                if (ClienteFiltroPendiente == null)
                    ClienteFiltroPendiente = ListaClientes.First();

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

        private void GenerarGlobal(object parameter)
        {
            MessageBox.Show("Generando factura global del periodo seleccionado...", "Factura Global");
        }

        private void CargarHistorial()
        {
            using (var db = new InventarioDbContext())
            {
                // Traemos las facturas ordenadas por fecha (la más nueva arriba)
                var facturasDb = db.Facturas
                                   .OrderByDescending(f => f.FechaEmision)
                                   .ToList();

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