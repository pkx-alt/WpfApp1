using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WpfApp1.Data;            // Para acceder a tu InventarioDbContext (SQLite)
using WpfApp1.Models;          // Para tus modelos locales (Cotizacion, etc)
using WpfApp1.Models.Supabase; // Para los modelos nube que acabas de arreglar (CotizacionWeb)

namespace WpfApp1.Services
{
    public class SupabaseService
    {
        // ⚠️ IMPORTANTE: Aquí van tus credenciales reales de Supabase
        // Las encuentras en tu Dashboard de Supabase -> Settings -> API
        private readonly string _url = "https://giqxulwkjkokyomylkne.supabase.co";
        private readonly string _key = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImdpcXh1bHdramtva3lvbXlsa25lIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjM5MjM3NjIsImV4cCI6MjA3OTQ5OTc2Mn0.r5xZjgI83qmhKgCjeGUH6XZrGWJF438wbZ5nf-_uXu4";

        private Supabase.Client _client;

        public SupabaseService()
        {
            var options = new Supabase.SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = true
            };
            // Inicializamos el cliente con tu URL y Llave
            _client = new Supabase.Client(_url, _key, options);
        }

        public async Task Inicializar()
        {
            await _client.InitializeAsync();
        }

        // --- EL MÉTODO MÁGICO ---
        // Este método hace todo el trabajo sucio:
        // 1. Va a la nube
        // 2. Baja lo pendiente
        // 3. Lo guarda en tu SQLite local
        // 4. Marca en la nube como "DESCARGADA"
        public async Task<int> SincronizarCotizaciones()
        {
            // Asegurarnos que la conexión esté lista
            await Inicializar();

            // 1. Pedir a Supabase las cotizaciones que estén 'PENDIENTE'
            //    (Usamos el modelo CotizacionWeb que ya arreglaste)
            var response = await _client.From<CotizacionWeb>()
                            // .Where(x => x.Estado == "PENDIENTE") // Comentado para probar
                            .Get();

            var cotizacionesNube = response.Models;
            int importadas = 0;

            // Si no hay nada nuevo, nos vamos temprano
            if (cotizacionesNube.Count == 0) return 0;

            using (var db = new InventarioDbContext())
            {
                foreach (var cotWeb in cotizacionesNube)
                {
                    // 2. Convertir: De "Nube" a "Local"
                    //    Creamos una nueva Cotización local con los datos que bajamos
                    var nuevaCotLocal = new Cotizacion
                    {
                        FechaEmision = cotWeb.FechaCreacion,
                        FechaVencimiento = cotWeb.FechaCreacion.AddDays(15), // Damos 15 días de vigencia
                        Origen = "Web", // ¡Esto es clave! Así sabrás en tu lista cuáles vinieron de internet
                        ClienteId = null, // Por ahora lo dejamos como "Público General" o null
                        Subtotal = 0, // Lo calcularemos sumando los detalles abajo
                        IVA = 0,
                        Total = 0
                    };

                    // 3. Traer los PRODUCTOS de ESA cotización específica
                    //    Buscamos en la tabla detalle_web donde coincida el ID
                    var detallesResponse = await _client.From<DetalleWeb>()
                                                        .Where(x => x.CotizacionId == cotWeb.Id)
                                                        .Get();

                    decimal sumaSubtotal = 0;

                    foreach (var detWeb in detallesResponse.Models)
                    {
                        // 4. INTENTAR ENLAZAR CON PRODUCTO LOCAL
                        //    Buscamos en tu SQLite si existe un producto con el mismo nombre
                        var productoLocal = db.Productos.FirstOrDefault(p => p.Descripcion == detWeb.Descripcion);

                        // Si lo encontramos, usamos su ID. Si no, ¿qué hacemos?
                        // Opción segura: Si no existe, no lo agregamos o lo ponemos con un ID genérico.
                        // Aquí asumiremos que si no existe, nos saltamos ese renglón para no romper la BD.
                        if (productoLocal != null)
                        {
                            var nuevoDetalle = new CotizacionDetalle
                            {
                                ProductoId = productoLocal.ID,
                                Descripcion = detWeb.Descripcion,
                                Cantidad = detWeb.Cantidad,
                                PrecioUnitario = detWeb.Precio,
                                Cotizacion = nuevaCotLocal // Enlazamos con la cabecera local
                            };

                            nuevaCotLocal.Detalles.Add(nuevoDetalle);
                            sumaSubtotal += (detWeb.Cantidad * detWeb.Precio);
                        }
                    }

                    // Si la cotización quedó vacía porque no encontramos ningún producto, mejor no la guardamos
                    if (nuevaCotLocal.Detalles.Count == 0) continue;

                    // Cálculos finales de dinero
                    // (Ajusta esto según si tus precios web ya tienen IVA o no)
                    nuevaCotLocal.Total = sumaSubtotal;
                    nuevaCotLocal.Subtotal = sumaSubtotal / 1.16m;
                    nuevaCotLocal.IVA = sumaSubtotal - nuevaCotLocal.Subtotal;

                    // 5. Guardar en SQLite (Tu base de datos POS)
                    db.Cotizaciones.Add(nuevaCotLocal);

                    // 6. CONFIRMACIÓN A LA NUBE
                    //    Le decimos a Supabase: "Ya bajé la cotización X, cámbiale el estado a DESCARGADA"
                    //    Así no la volveremos a bajar la próxima vez.
                    await _client.From<CotizacionWeb>()
                                 .Where(x => x.Id == cotWeb.Id)
                                 .Set(x => x.Estado, "DESCARGADA")
                                 .Update();

                    importadas++;
                }

                // Guardamos todos los cambios en SQLite de un golpe
                db.SaveChanges();
            }

            return importadas; // Devolvemos cuántas bajamos para mostrar un mensajito
        }
    }
}