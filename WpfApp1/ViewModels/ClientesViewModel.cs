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
        public void CargarClientes(string busqueda = null, bool verActivos = true, bool verInactivos = false)
        {
            Clientes.Clear();

            using (var db = new InventarioDbContext())
            {
                var consulta = db.Clientes.AsQueryable();

                // --- 1. Filtro de Búsqueda (¡MODIFICADO!) ---
                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    // Convertimos el término de búsqueda a mayúsculas una sola vez
                    string busquedaUpper = busqueda.ToUpper();

                    consulta = consulta.Where(c =>
                        // Y comparamos todo en mayúsculas
                        c.RazonSocial.ToUpper().Contains(busquedaUpper) ||
                        c.RFC.ToUpper().Contains(busquedaUpper)
                    );
                }

                // --- 2. Filtro de estado (sin cambios) ---
                consulta = consulta.Where(c =>
                    (c.Activo && verActivos) ||
                    (!c.Activo && verInactivos)
                );

                // --- 3. Ejecución (sin cambios) ---
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