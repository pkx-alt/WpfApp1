using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using WpfApp1.Data;
using WpfApp1.Models;

namespace WpfApp1.ViewModels
{
    // HEREDAMOS DE ViewModelBase PARA TENER OnPropertyChanged
    public class GastosViewModel : ViewModelBase
    {
        public ObservableCollection<Gasto> ListaGastos { get; set; }

        // PROPIEDAD COMPLETA (Full Property)
        private decimal _totalGastosMes;
        public decimal TotalGastosMes
        {
            get => _totalGastosMes;
            set
            {
                _totalGastosMes = value;
                OnPropertyChanged(); // <-- Ahora sí existe y funciona
            }
        }

        // GastosViewModel.cs (dentro de la clase)

        // Propiedades de filtro (necesitan OnPropertyChanged)
        private DateTime? _fechaInicio;
        public DateTime? FechaInicio
        {
            get => _fechaInicio;
            set
            {
                _fechaInicio = value;
                OnPropertyChanged();
                AplicarFiltros(); // Llama a la lógica de filtro cada vez que la fecha cambia
            }
        }

        private DateTime? _fechaFin;
        public DateTime? FechaFin
        {
            get => _fechaFin;
            set
            {
                _fechaFin = value;
                OnPropertyChanged();
                AplicarFiltros();
            }
        }

        private string _textoBusqueda;
        public string TextoBusqueda
        {
            get => _textoBusqueda;
            set
            {
                _textoBusqueda = value;
                OnPropertyChanged();
                AplicarFiltros(); // Aplica el filtro cada vez que el usuario escribe
            }
        }


        // GastosViewModel.cs (dentro de la clase)

        private List<Gasto> _todosLosGastos; // Lista completa sin filtrar
        public ICollectionView GastosFiltrados { get; set; } // Propiedad que la DataGrid usará

        public GastosViewModel()
        {
            // 1. Inicializa la lista observable (que se enlaza a la tabla)
            ListaGastos = new ObservableCollection<Gasto>();

            // Inicializamos la lista completa
            _todosLosGastos = new List<Gasto>();

            CargarGastos();

            // 2. Creamos la vista filtrada (ICollectionView)
            // Usamos CollectionViewSource.GetDefaultView para crear la "capa" filtrable
            GastosFiltrados = CollectionViewSource.GetDefaultView(ListaGastos);

            // 3. Asignamos el método de filtrado
            GastosFiltrados.Filter = new Predicate<object>(FiltroPersonalizado);
        }

        private void CargarGastos()
        {
            using (var db = new InventarioDbContext())
            {
                try
                {

                    // Lee todos los gastos y los guarda en la lista privada
                    _todosLosGastos = db.Gastos.OrderByDescending(g => g.Fecha).ToList();

                    // Llena la ObservableCollection con la lista completa inicialmente
                    ListaGastos.Clear();
                    foreach (var gasto in _todosLosGastos)
                    {
                        ListaGastos.Add(gasto);
                    }
                    var gastosDB = db.Gastos.ToList();

                    ListaGastos.Clear();
                    foreach (var gasto in gastosDB.OrderByDescending(g => g.Fecha))
                    {
                        ListaGastos.Add(gasto);
                    }

                    // Calcular total
                    DateTime inicioMes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

                    // Al asignar el valor aquí, el 'set' de arriba dispara OnPropertyChanged automáticamente
                    TotalGastosMes = gastosDB
                        .Where(g => g.Fecha >= inicioMes)
                        .Sum(g => g.Monto);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}");
                }
            }
        }

        public void AgregarGasto(Gasto nuevoGasto)
        {
            using (var db = new InventarioDbContext())
            {
                db.Gastos.Add(nuevoGasto);
                db.SaveChanges();
            }

            // Agregamos a la lista visual (arriba del todo)
            ListaGastos.Insert(0, nuevoGasto);

            // Actualizamos el total si corresponde al mes actual
            DateTime inicioMes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            if (nuevoGasto.Fecha >= inicioMes)
            {
                // Al sumar aquí, el 'set' dispara la notificación a la UI
                TotalGastosMes += nuevoGasto.Monto;
            }
        }

        // 2. Método de Filtrado (Se ejecuta en cada elemento de la lista)
        private bool FiltroPersonalizado(object item)
        {
            var gasto = item as Gasto;
            if (gasto == null) return false;

            // A. Filtro por Búsqueda (Texto)
            if (!string.IsNullOrWhiteSpace(TextoBusqueda))
            {
                string busqueda = TextoBusqueda.ToLower();
                if (!(gasto.Concepto?.ToLower().Contains(busqueda) == true ||
                      gasto.Categoria?.ToLower().Contains(busqueda) == true ||
                      gasto.Usuario?.ToLower().Contains(busqueda) == true))
                {
                    return false;
                }
            }

            // B. Filtro por Fecha de Inicio
            if (FechaInicio.HasValue && gasto.Fecha.Date < FechaInicio.Value.Date)
            {
                return false;
            }

            // C. Filtro por Fecha de Fin
            if (FechaFin.HasValue && gasto.Fecha.Date > FechaFin.Value.Date)
            {
                return false;
            }

            // Si pasó todos los filtros, mostrar
            return true;
        }

        // 3. Método para Aplicar el Filtro
        public void AplicarFiltros()
        {
            // Le decimos a la vista filtrada que re-evalúe todos los elementos.
            GastosFiltrados.Refresh();
        }
    }
}