using Microsoft.EntityFrameworkCore; // ¡Vital para .Include()!
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using WpfApp1.Data;
using WpfApp1.Models;

namespace WpfApp1.ViewModels
{
    public class MovimientosViewModel : ViewModelBase
    {
        // La lista que verá la tabla
        public ObservableCollection<MovimientoInventario> ListaMovimientos { get; set; }

        // Constructor
        public MovimientosViewModel()
        {
            ListaMovimientos = new ObservableCollection<MovimientoInventario>();
            CargarMovimientos();
        }

        // Método para leer la BD
        public void CargarMovimientos(int? productoIdFiltro = null)
        {
            using (var db = new InventarioDbContext())
            {
                try
                {
                    // 1. Preparamos la consulta
                    var query = db.Movimientos
                                  .Include(m => m.Producto) // ¡Traemos el nombre del producto!
                                  .AsQueryable();

                    // 2. ¿Hay filtro? (Para cuando vengas desde el botón de Inventario)
                    if (productoIdFiltro.HasValue)
                    {
                        query = query.Where(m => m.ProductoId == productoIdFiltro.Value);
                    }

                    // 3. Ordenamos: Lo más nuevo arriba
                    var resultados = query.OrderByDescending(m => m.Fecha).ToList();

                    // 4. Llenamos la lista
                    ListaMovimientos.Clear();
                    foreach (var item in resultados)
                    {
                        ListaMovimientos.Add(item);
                    }
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show("Error al cargar movimientos: " + ex.Message);
                }
            }
        }
    }
}