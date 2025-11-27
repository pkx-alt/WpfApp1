using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows; // Para MessageBox
using Microsoft.EntityFrameworkCore;
using LiveCharts;
using LiveCharts.Wpf;
using OrySiPOS.Data;
using OrySiPOS.Models;
using OrySiPOS.Services;

namespace OrySiPOS.ViewModels
{
    public class EstadisticasViewModel : ViewModelBase
    {
        // --- SERVICIOS ---
        private ReportesVentasService _ventasService;

        // --- FILTROS ---
        private DateTime _fechaInicio;
        public DateTime FechaInicio
        {
            get { return _fechaInicio; }
            set { _fechaInicio = value; OnPropertyChanged(); CargarDatosReales(); }
        }

        private DateTime _fechaFin;
        public DateTime FechaFin
        {
            get { return _fechaFin; }
            set { _fechaFin = value; OnPropertyChanged(); CargarDatosReales(); }
        }

        // --- KPIs ---
        private decimal _ingresosNetos;
        public decimal IngresosNetos { get => _ingresosNetos; set { _ingresosNetos = value; OnPropertyChanged(); } }

        private decimal _gananciaBruta;
        public decimal GananciaBruta { get => _gananciaBruta; set { _gananciaBruta = value; OnPropertyChanged(); } }

        private int _ventasTotales;
        public int VentasTotales { get => _ventasTotales; set { _ventasTotales = value; OnPropertyChanged(); } }

        private decimal _gastosOperativos;
        public decimal GastosOperativos { get => _gastosOperativos; set { _gastosOperativos = value; OnPropertyChanged(); } }

        private string _comparativaIngreso;
        public string ComparativaIngreso { get => _comparativaIngreso; set { _comparativaIngreso = value; OnPropertyChanged(); } }

        // --- GRÁFICA Y TABLA ---
        public SeriesCollection SeriesVentas { get; set; }
        public string[] EtiquetasEjeX { get; set; }
        public Func<double, string> FormateadorMoneda { get; set; }
        public ObservableCollection<EstadisticaProducto> TopProductos { get; set; }

        // --- CONSTRUCTOR ---
        public EstadisticasViewModel()
        {
            try
            {
                // 1. PRIMERO INICIALIZAMOS TODO (Crucial para evitar NullReference)
                _ventasService = new ReportesVentasService();
                TopProductos = new ObservableCollection<EstadisticaProducto>();
                SeriesVentas = new SeriesCollection();
                FormateadorMoneda = value => value.ToString("C0");

                // 2. AL FINAL, ASIGNAMOS FECHAS
                // (Esto dispara CargarDatosReales, así que todo lo de arriba debe existir ya)
                FechaInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                FechaFin = DateTime.Now;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al iniciar estadísticas: " + ex.Message);
            }
        }

        // --- MÉTODO DE CARGA ---
        public void CargarDatosReales()
        {
            // GUARDIÁN 1: Si las listas no existen aún, no hagas nada (pasa en el arranque)
            if (_ventasService == null || TopProductos == null || SeriesVentas == null) return;

            // GUARDIÁN 2: Fechas lógicas
            if (_fechaInicio > _fechaFin) return;

            try
            {
                DateTime finAjustado = FechaFin.Date.AddDays(1).AddTicks(-1);

                // ------------------------------------------------
                // 1. CARGAR TOP PRODUCTOS
                // ------------------------------------------------
                var resultadosTop = _ventasService.ObtenerProductosMasVendidos(FechaInicio, finAjustado);
                TopProductos.Clear();

                if (resultadosTop != null)
                {
                    foreach (var item in resultadosTop)
                    {
                        TopProductos.Add(new EstadisticaProducto
                        {
                            // Usamos ?.ToString() por seguridad si viniera nulo
                            Nombre = item.Producto?.ToString() ?? "(Sin nombre)",
                            Cantidad = item.Unidades,
                            Monto = item.TotalDinero
                        });
                    }
                }

                // ------------------------------------------------
                // 2. CALCULAR KPIs
                // ------------------------------------------------
                using (var db = new InventarioDbContext())
                {
                    // A. Ingresos
                    var ventasPeriodo = db.Ventas
                        .Where(v => v.Fecha >= FechaInicio && v.Fecha <= finAjustado)
                        .Sum(v => (decimal?)v.Total) ?? 0; // El (decimal?) maneja si la suma da null

                    var otrosIngresos = db.Ingresos
                        .Where(i => i.Fecha >= FechaInicio && i.Fecha <= finAjustado)
                        .Sum(i => (decimal?)i.Monto) ?? 0;

                    IngresosNetos = ventasPeriodo + otrosIngresos;

                    // B. Gastos
                    GastosOperativos = db.Gastos
                        .Where(g => g.Fecha >= FechaInicio && g.Fecha <= finAjustado)
                        .Sum(g => (decimal?)g.Monto) ?? 0;

                    // C. Ventas Totales
                    VentasTotales = db.Ventas
                        .Count(v => v.Fecha >= FechaInicio && v.Fecha <= finAjustado);

                    // D. Ganancia (Aquí es donde solía fallar si había productos nulos)
                    // Hacemos la consulta defensiva: Filtramos donde Producto != null
                    GananciaBruta = db.VentasDetalle
                        .Include(d => d.Producto)
                        .Include(d => d.Venta)
                        .Where(d => d.Venta.Fecha >= FechaInicio && d.Venta.Fecha <= finAjustado)
                        .Where(d => d.Producto != null) // <--- ¡ESTA LÍNEA SALVA VIDAS!
                        .Sum(d => (d.Cantidad * d.PrecioUnitario) - (d.Cantidad * d.Producto.Costo));

                    ComparativaIngreso = "Periodo seleccionado";
                }

                // ------------------------------------------------
                // 3. GRÁFICA
                // ------------------------------------------------
                var datosGrafica = _ventasService.ObtenerComparativoMensual(FechaInicio, finAjustado);
                var valores = new ChartValues<double>();
                var etiquetas = new List<string>();

                if (datosGrafica != null)
                {
                    foreach (var item in datosGrafica.OrderBy(x => x.FechaOrden))
                    {
                        valores.Add((double)item.TotalVendido);
                        etiquetas.Add(item.Periodo.ToString());
                    }
                }

                // ... (código anterior donde calculas los valores y etiquetas) ...

                // CREAMOS LA COLECCIÓN NUEVA
                var nuevaColeccion = new SeriesCollection();

                // --- CAMBIO AQUÍ: Usamos ColumnSeries en vez de LineSeries ---
                nuevaColeccion.Add(new ColumnSeries
                {
                    Title = "Ventas",
                    Values = valores,
                    DataLabels = true,             // Muestra el número encima de la barra
                    MaxColumnWidth = 35,           // Para que no se vea gigante si es solo una
                    ColumnPadding = 2              // Espacio entre barras
                });

                // ASIGNAMOS
                SeriesVentas = nuevaColeccion;
                EtiquetasEjeX = etiquetas.ToArray();

                OnPropertyChanged(nameof(SeriesVentas));
                OnPropertyChanged(nameof(EtiquetasEjeX));

            }
            catch (Exception ex)
            {
                // Si algo falla, al menos sabremos qué fue sin cerrar la app
                MessageBox.Show($"Error calculando estadísticas: {ex.Message} \n\n{ex.StackTrace}");
            }
        }
    }
}