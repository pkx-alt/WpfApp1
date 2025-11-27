// --- ViewModels/DetalleVentaViewModel.cs ---
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using OrySiPOS.Data;
using OrySiPOS.Models;
using OrySiPOS.Helpers; // ¡Para poder usar NumeroALetras!

namespace OrySiPOS.ViewModels
{
    // Usamos ViewModelBase para poder "notificar" a la vista
    public class DetalleVentaViewModel : ViewModelBase
    {
        // --- 1. Propiedades para la Vista (UI) ---
        // Estas son las "cajas" que el XAML va a leer

        public ObservableCollection<VentaDetalleGridItem> DetallesVenta { get; set; }

        // --- Info del Encabezado ---
        private int _folio;
        public int Folio
        {
            get { return _folio; }
            set { _folio = value; OnPropertyChanged(); }
        }

        private string _nombreCliente;
        public string NombreCliente
        {
            get { return _nombreCliente; }
            set { _nombreCliente = value; OnPropertyChanged(); }
        }

        private DateTime _fechaHora;
        public DateTime FechaHora
        {
            get { return _fechaHora; }
            set { _fechaHora = value; OnPropertyChanged(); }
        }

        // --- Info del Pie (Izquierdo) ---
        private decimal _recibido;
        public decimal Recibido
        {
            get { return _recibido; }
            set { _recibido = value; OnPropertyChanged(); }
        }

        private decimal _cambio;
        public decimal Cambio
        {
            get { return _cambio; }
            set { _cambio = value; OnPropertyChanged(); }
        }

        private decimal _ganancia;
        public decimal Ganancia
        {
            get { return _ganancia; }
            set { _ganancia = value; OnPropertyChanged(); }
        }

        // --- Info del Pie (Derecho) ---
        private decimal _subtotal;
        public decimal Subtotal
        {
            get { return _subtotal; }
            set { _subtotal = value; OnPropertyChanged(); }
        }

        private decimal _iva;
        public decimal IVA
        {
            get { return _iva; }
            set { _iva = value; OnPropertyChanged(); }
        }

        private decimal _total;
        public decimal Total
        {
            get { return _total; }
            set { _total = value; OnPropertyChanged(); }
        }
        private string _totalEnLetra;
        public string TotalEnLetra
        {
            get { return _totalEnLetra; }
            set { _totalEnLetra = value; OnPropertyChanged(); }
        }

        // --- 2. Comandos y Acciones ---
        public ICommand CerrarDialogoCommand { get; }
        public Action<bool> CloseAction { get; set; } // El "truco" para cerrar la ventana


        // --- 3. Constructor ---
        public DetalleVentaViewModel(int ventaId)
        {
            // Inicializamos la lista
            DetallesVenta = new ObservableCollection<VentaDetalleGridItem>();

            // "Enchufamos" los comandos
            CerrarDialogoCommand = new RelayCommand(p => CloseAction?.Invoke(false));

            // ¡Cargamos los datos!
            CargarDetallesVenta(ventaId);
        }

        // --- 4. Lógica de Carga de Datos ---
        private void CargarDetallesVenta(int ventaId)
        {
            using (var db = new InventarioDbContext())
            {
                // ¡Esta es la consulta mágica!
                // Pedimos la Venta, y le decimos a EF Core que "Incluya"
                // los datos de las tablas relacionadas que necesitamos.
                var venta = db.Ventas
                    .Include(v => v.Cliente) // Para el nombre del cliente
                    .Include(v => v.Detalles) // Para la lista de productos
                        .ThenInclude(d => d.Producto) // ¡Para la Descripción y Costo!
                    .FirstOrDefault(v => v.VentaId == ventaId);

                if (venta == null)
                {
                    MessageBox.Show("No se encontró la venta.", "Error");
                    return;
                }

                // --- Llenamos las propiedades del ViewModel ---

                // Encabezado
                Folio = venta.VentaId;
                NombreCliente = venta.Cliente?.RazonSocial ?? "Público General";
                FechaHora = venta.Fecha;

                // Pie Derecho (los totales)
                Subtotal = venta.Subtotal;
                IVA = venta.IVA;
                Total = venta.Total;

                TotalEnLetra = NumeroALetras.Convertir(venta.Total);
                // Pie Izquierdo (el pago)
                Recibido = venta.PagoRecibido;
                Cambio = venta.Cambio;

                // Pie Izquierdo (Cálculo de Ganancia)
                // OJO: ¡Esto asume que tu 'Producto' tiene una propiedad 'Costo'!
                try
                {
                    decimal costoTotal = venta.Detalles.Sum(d => (d.Producto.Costo * d.Cantidad));
                    Ganancia = venta.Total - costoTotal;
                }
                catch (Exception)
                {
                    Ganancia = 0; // Pasa si un producto no tiene costo
                }

                // Llenamos el DataGrid
                foreach (var detalle in venta.Detalles.OrderBy(d => d.Producto.Descripcion))
                {
                    DetallesVenta.Add(new VentaDetalleGridItem
                    {
                        ID = detalle.Producto.ID.ToString(), // O el Cód. de Barras
                        Descripcion = detalle.Producto.Descripcion,
                        UD = detalle.Cantidad,
                        Precio = detalle.PrecioUnitario,
                        Subtotal = detalle.PrecioUnitario * detalle.Cantidad
                    });
                }
            }
        }
    }
}