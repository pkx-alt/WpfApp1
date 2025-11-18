using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using WpfApp1.Data;
using WpfApp1.Models;
using System.ComponentModel; // Para ICollectionView
using System.Windows.Data;   // Para CollectionViewSource

namespace WpfApp1.ViewModels
{
    public class IngresosViewModel : ViewModelBase
    {
        // --- PROPIEDADES DE LISTA Y VISTA ---
        private List<Ingreso> _todosLosIngresos; // Lista completa sin filtrar
        public ObservableCollection<Ingreso> ListaIngresos { get; set; } // Lista observable (obsoleta, usamos la vista)
        public ICollectionView IngresosFiltrados { get; set; } // <-- LO NUEVO: La fuente de datos de la DataGrid

        // --- PROPIEDADES DE FILTRO (Con OnPropertyChanged y AplicarFiltros) ---
        private DateTime? _fechaInicio;
        public DateTime? FechaInicio
        {
            get => _fechaInicio;
            set { _fechaInicio = value; OnPropertyChanged(); AplicarFiltros(); }
        }

        private DateTime? _fechaFin;
        public DateTime? FechaFin
        {
            get => _fechaFin;
            set { _fechaFin = value; OnPropertyChanged(); AplicarFiltros(); }
        }

        private string _textoBusqueda;
        public string TextoBusqueda
        {
            get => _textoBusqueda;
            set { _textoBusqueda = value; OnPropertyChanged(); AplicarFiltros(); }
        }

        // --- PROPIEDAD DEL TOTAL ---
        private decimal _totalIngresosMes;
        public decimal TotalIngresosMes
        {
            get => _totalIngresosMes;
            set { _totalIngresosMes = value; OnPropertyChanged(); }
        }

        public IngresosViewModel()
        {
            ListaIngresos = new ObservableCollection<Ingreso>();
            _todosLosIngresos = new List<Ingreso>();

            CargarIngresos();

            // 1. Creamos la vista filtrada a partir de la lista observable
            IngresosFiltrados = CollectionViewSource.GetDefaultView(ListaIngresos);

            // 2. Asignamos nuestro método de filtrado personalizado
            IngresosFiltrados.Filter = new Predicate<object>(FiltroPersonalizado);
        }

        // --- MÉTODOS DE LÓGICA ---

        private void CargarIngresos()
        {
            using (var db = new InventarioDbContext())
            {
                try
                {
                    // Lee todos los ingresos una sola vez y los guarda en la lista privada
                    _todosLosIngresos = db.Ingresos.OrderByDescending(i => i.Fecha).ToList();

                    // Llenamos la ObservableCollection con la lista completa (para la vista filtrada)
                    ListaIngresos.Clear();
                    foreach (var ingreso in _todosLosIngresos)
                    {
                        ListaIngresos.Add(ingreso);
                    }

                    // Recalcula el total del mes
                    DateTime inicioMes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                    TotalIngresosMes = _todosLosIngresos
                        .Where(i => i.Fecha >= inicioMes)
                        .Sum(i => i.Monto);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cargar ingresos: {ex.Message}");
                }
            }
        }

        // El motor de filtrado: se ejecuta por cada fila de la tabla
        private bool FiltroPersonalizado(object item)
        {
            var ingreso = item as Ingreso;
            if (ingreso == null) return false;

            // 1. Filtro por Búsqueda (Concepto, Categoría, Usuario)
            if (!string.IsNullOrWhiteSpace(TextoBusqueda))
            {
                string busqueda = TextoBusqueda.ToLower();
                if (!(ingreso.Concepto?.ToLower().Contains(busqueda) == true ||
                      ingreso.Categoria?.ToLower().Contains(busqueda) == true ||
                      ingreso.Usuario?.ToLower().Contains(busqueda) == true))
                {
                    return false;
                }
            }

            // 2. Filtro por Rango de Fecha de Inicio
            if (FechaInicio.HasValue && ingreso.Fecha.Date < FechaInicio.Value.Date)
            {
                return false;
            }

            // 3. Filtro por Rango de Fecha de Fin
            if (FechaFin.HasValue && ingreso.Fecha.Date > FechaFin.Value.Date)
            {
                return false;
            }

            // Si pasó todos los filtros, es visible
            return true;
        }

        // Método que se llama desde los setters de las propiedades de filtro
        public void AplicarFiltros()
        {
            IngresosFiltrados.Refresh(); // <-- Fuerza a que FiltroPersonalizado se ejecute de nuevo
        }

        public void AgregarIngreso(Ingreso nuevoIngreso)
        {
            // 1. Guardar en Base de Datos
            using (var db = new InventarioDbContext())
            {
                db.Ingresos.Add(nuevoIngreso);
                db.SaveChanges();
            }

            // 2. Actualizar la lista COMPLETA y la lista visible (la ObservableCollection)
            _todosLosIngresos.Insert(0, nuevoIngreso); // Importante para la persistencia
            ListaIngresos.Insert(0, nuevoIngreso);     // Actualiza la tabla

            // 3. Actualizar el total (si aplica)
            DateTime inicioMes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            if (nuevoIngreso.Fecha >= inicioMes)
            {
                TotalIngresosMes += nuevoIngreso.Monto;
            }

            // 4. Si hay filtros activos, el nuevo ingreso aparecerá o no según los filtros.
            AplicarFiltros();
        }
    }
}