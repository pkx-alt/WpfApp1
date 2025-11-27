using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using OrySiPOS.Data;
using OrySiPOS.Models;

namespace OrySiPOS.ViewModels
{
    public class MovimientosViewModel : ViewModelBase
    {
        public ObservableCollection<MovimientoInventario> ListaMovimientos { get; set; }

        // --- NUEVA PROPIEDAD ---
        private string _tituloPagina;
        public string TituloPagina
        {
            get { return _tituloPagina; }
            set { _tituloPagina = value; OnPropertyChanged(); }
        }
        private int _totalMovimientos;
        public int TotalMovimientos
        {
            get { return _totalMovimientos; }
            set { _totalMovimientos = value; OnPropertyChanged(); }
        }
        // -----------------------

        public MovimientosViewModel()
        {
            ListaMovimientos = new ObservableCollection<MovimientoInventario>();
            // Carga inicial por defecto (sin filtro)
            CargarMovimientos();
        }

        // Método para leer la BD
        public void CargarMovimientos(int? productoIdFiltro = null)
        {
            using (var db = new InventarioDbContext())
            {
                try
                {
                    var query = db.Movimientos
                                  .Include(m => m.Producto)
                                  .AsQueryable();

                    if (productoIdFiltro.HasValue)
                    {
                        query = query.Where(m => m.ProductoId == productoIdFiltro.Value);

                        // --- DETALLE PROFESIONAL ---
                        // Buscamos el nombre del producto para ponerlo en el título
                        var prod = db.Productos.Find(productoIdFiltro.Value);
                        TituloPagina = prod != null ? $"Kardex: {prod.Descripcion}" : "Historial de Movimientos";
                    }
                    else
                    {
                        TituloPagina = "Kardex General de Movimientos";
                    }

                    var resultados = query.OrderByDescending(m => m.Fecha).ToList();

                    ListaMovimientos.Clear();
                    foreach (var item in resultados)
                    {
                        ListaMovimientos.Add(item);
                    }

                    TotalMovimientos = ListaMovimientos.Count;
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show("Error al cargar movimientos: " + ex.Message);
                }
            }
        }
    }
}