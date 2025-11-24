using System.Collections.ObjectModel; // Para ObservableCollection
using System.Linq; // Para .ToList()
using WpfApp1.Data;
using WpfApp1.Models;

namespace WpfApp1.ViewModels
{
    // NOTA: Para un MVVM "completo" esto implementaría INotifyPropertyChanged,
    // pero vamos a mantenerlo simple por ahora.
    public class ClientesViewModel
    {
        // Esta es la propiedad que tu DataGrid está buscando.
        // La inicializamos de una vez para que no esté vacía.
        public ObservableCollection<Cliente> Clientes { get; set; } = new ObservableCollection<Cliente>();

        // El constructor: se ejecuta cuando se crea el objeto
        public ClientesViewModel()
        {
            CargarClientes(); // Cargamos los clientes al iniciar
        }

        // El método para cargar (o recargar) los clientes
        // En ClientesViewModel.cs, dentro de CargarClientes
        // En ClientesViewModel.cs

        // Modifica la firma del método para aceptar los nuevos filtros (verFacturados, verNoFacturados)
        public void CargarClientes(string busqueda = null, bool verActivos = true, bool verInactivos = false, bool verFacturados = true, bool verNoFacturados = true)
        {
            Clientes.Clear();

            using (var db = new InventarioDbContext())
            {
                var consulta = db.Clientes.AsQueryable();

                // 1. Filtro de Búsqueda
                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    string busquedaUpper = busqueda.ToUpper();
                    consulta = consulta.Where(c =>
                        c.RazonSocial.ToUpper().Contains(busquedaUpper) ||
                        c.RFC.ToUpper().Contains(busquedaUpper)
                    );
                }

                // 2. Filtro de Estado (Activo/Inactivo)
                consulta = consulta.Where(c =>
                    (c.Activo && verActivos) ||
                    (!c.Activo && verInactivos)
                );

                // 3. NUEVO: Filtro de Facturación
                // "Traeme los que (Son Factura Y quiero ver facturados) O (No son Factura Y quiero ver no facturados)"
                consulta = consulta.Where(c =>
                    (c.EsFactura && verFacturados) ||
                    (!c.EsFactura && verNoFacturados)
                );

                // Ejecución
                var listaClientes = consulta
                                    .OrderByDescending(c => c.Creado)
                                    .ToList();

                foreach (var cliente in listaClientes)
                {
                    Clientes.Add(cliente);
                }
            }
        }
    }
}