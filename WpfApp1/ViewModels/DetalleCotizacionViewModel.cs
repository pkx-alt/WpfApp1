using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using OrySiPOS.Data;
using OrySiPOS.Models;
using OrySiPOS.Helpers; // Para NumeroALetras

namespace OrySiPOS.ViewModels
{
    public class DetalleCotizacionViewModel : ViewModelBase
    {
        // --- COLECCIÓN PARA LA TABLA ---
        public ObservableCollection<DetalleItem> Detalles { get; set; }

        // --- ENCABEZADO ---
        private int _folio;
        public int Folio { get => _folio; set { _folio = value; OnPropertyChanged(); } }

        private string _nombreCliente;
        public string NombreCliente { get => _nombreCliente; set { _nombreCliente = value; OnPropertyChanged(); } }

        private DateTime _fechaEmision;
        public DateTime FechaEmision { get => _fechaEmision; set { _fechaEmision = value; OnPropertyChanged(); } }

        private DateTime _fechaVencimiento;
        public DateTime FechaVencimiento { get => _fechaVencimiento; set { _fechaVencimiento = value; OnPropertyChanged(); } }

        // --- TOTALES (PIE DERECHO) ---
        private decimal _subtotal;
        public decimal Subtotal { get => _subtotal; set { _subtotal = value; OnPropertyChanged(); } }

        private decimal _iva;
        public decimal IVA { get => _iva; set { _iva = value; OnPropertyChanged(); } }

        private decimal _descuento; // ¡Nuevo!
        public decimal Descuento { get => _descuento; set { _descuento = value; OnPropertyChanged(); } }

        private decimal _total;
        public decimal Total { get => _total; set { _total = value; OnPropertyChanged(); } }

        private string _totalEnLetra;
        public string TotalEnLetra { get => _totalEnLetra; set { _totalEnLetra = value; OnPropertyChanged(); } }

        // --- COMANDOS ---
        public ICommand CerrarCommand { get; }
        public ICommand ConvertirCommand { get; }

        public Action<bool> CloseAction { get; set; } // Para cerrar la ventana
        public Action ConvertirAction { get; set; }   // Para avisar que queremos convertir

        // --- CONSTRUCTOR ---
        public DetalleCotizacionViewModel(int cotizacionId)
        {
            Detalles = new ObservableCollection<DetalleItem>();

            CerrarCommand = new RelayCommand(p => CloseAction?.Invoke(false));
            ConvertirCommand = new RelayCommand(EjecutarConversion);

            CargarDatos(cotizacionId);
        }

        private void CargarDatos(int id)
        {
            using (var db = new InventarioDbContext())
            {
                var cot = db.Cotizaciones
                            .Include(c => c.Cliente)
                            .Include(c => c.Detalles)
                            .FirstOrDefault(c => c.ID == id);

                if (cot == null) return;

                Folio = cot.ID;
                NombreCliente = cot.Cliente?.RazonSocial ?? "Público General";
                FechaEmision = cot.FechaEmision;
                FechaVencimiento = cot.FechaVencimiento;

                Subtotal = cot.Subtotal;
                IVA = cot.IVA;
                Total = cot.Total;

                // Calculamos descuento si el total no cuadra con subtotal+iva
                decimal totalTeorico = Subtotal + IVA;
                Descuento = (totalTeorico > Total) ? (totalTeorico - Total) : 0;

                TotalEnLetra = NumeroALetras.Convertir(Total);

                // Llenar la tabla
                foreach (var d in cot.Detalles)
                {
                    Detalles.Add(new DetalleItem
                    {
                        Cantidad = d.Cantidad,
                        ID = d.ProductoId.ToString(),
                        Descripcion = d.Descripcion,
                        Precio = d.PrecioUnitario,
                        Subtotal = d.ImporteTotal
                    });
                }
            }
        }

        private void EjecutarConversion(object obj)
        {
            // Aquí disparas la acción para que la Vista (CodeBehind) sepa qué hacer
            ConvertirAction?.Invoke();
        }
    }

    // Clase sencilla para mostrar en el Grid (DTO)
    public class DetalleItem
    {
        public int Cantidad { get; set; }
        public string ID { get; set; }
        public string Descripcion { get; set; }
        public decimal Precio { get; set; }
        public decimal Subtotal { get; set; }
    }
}