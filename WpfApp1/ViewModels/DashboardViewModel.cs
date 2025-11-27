using System;
using System.Linq;
using OrySiPOS.Data;
using OrySiPOS.ViewModels; // Para usar tu ViewModelBase

namespace OrySiPOS.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        // --- PROPIEDADES (Lo que la vista va a mostrar) ---

        private decimal _ventasHoy;
        public decimal VentasHoy
        {
            get { return _ventasHoy; }
            set { _ventasHoy = value; OnPropertyChanged(); }
        }

        private int _cotizacionesPendientes;
        public int CotizacionesPendientes
        {
            get { return _cotizacionesPendientes; }
            set { _cotizacionesPendientes = value; OnPropertyChanged(); }
        }

        private int _productosBajoStock;
        public int ProductosBajoStock
        {
            get { return _productosBajoStock; }
            set { _productosBajoStock = value; OnPropertyChanged(); }
        }

        // --- CONSTRUCTOR ---
        public DashboardViewModel()
        {
            // Cargamos los datos apenas se crea el ViewModel
            CargarMetricas();
        }

        // --- EL MÉTODO DE CARGA (La magia) ---
        public void CargarMetricas()
        {
            using (var db = new InventarioDbContext())
            {
                // 1. CALCULAR VENTAS DE HOY
                // Definimos "hoy" desde las 00:00:00 horas
                DateTime inicioDia = DateTime.Today;

                // Sumamos el TOTAL de todas las ventas cuya fecha sea hoy o después
                VentasHoy = db.Ventas
                              .Where(v => v.Fecha >= inicioDia)
                              .Sum(v => v.Total);

                // 2. CONTAR COTIZACIONES
                // Aquí contamos todas las cotizaciones. 
                // (Podrías filtrar por fecha si quisieras solo las "vigentes")
                CotizacionesPendientes = db.Cotizaciones.Count();

                // 3. CONTAR PRODUCTOS CON BAJO STOCK
                // Contamos productos activos que tengan 5 o menos unidades
                ProductosBajoStock = db.Productos
                                       .Count(p => p.Activo && p.Stock <= 5);
            }
        }
    }
}