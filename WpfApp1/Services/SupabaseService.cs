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
            await Inicializar();

            // 1. Traer cabeceras PENDIENTES
            var response = await _client.From<CotizacionWeb>()
                            .Where(x => x.Estado == "PENDIENTE") // <--- IMPORTANTE: Asegúrate que en la nube diga 'PENDIENTE'
                            .Get();

            var cotizacionesNube = response.Models;
            int importadas = 0;

            if (cotizacionesNube.Count == 0) return 0;

            using (var db = new InventarioDbContext())
            {
                // Buscar (o crear si no existe) un producto comodín para ítems no encontrados
                var productoComodin = db.Productos.FirstOrDefault(p => p.Descripcion == "ITEM WEB NO ENCONTRADO");

                if (productoComodin == null)
                {
                    // Creamos uno por defecto si no existe, para no perder la venta
                    // (Necesitamos una subcategoría válida, agarramos la primera)
                    var subcat = db.Subcategorias.FirstOrDefault();
                    if (subcat != null)
                    {
                        productoComodin = new Producto
                        {
                            Descripcion = "ITEM WEB NO ENCONTRADO",
                            Precio = 0,
                            Costo = 0,
                            Stock = 0,
                            Activo = true,
                            SubcategoriaId = subcat.Id,
                            ClaveSat = "01010101",
                            ClaveUnidad = "H87"
                        };
                        db.Productos.Add(productoComodin);
                        db.SaveChanges();
                    }
                }

                foreach (var cotWeb in cotizacionesNube)
                {
                    // Validar si ya existe localmente para no duplicar (por seguridad)
                    // (Esto asume que podríamos guardar el ID web en algún lado, pero por ahora lo omitimos para simplificar)

                    var nuevaCotLocal = new Cotizacion
                    {
                        FechaEmision = cotWeb.FechaCreacion,
                        FechaVencimiento = cotWeb.FechaCreacion.AddDays(15),
                        Origen = "Web",
                        ClienteId = null, // Dejamos null o podrías buscar al cliente por nombre
                        Subtotal = 0,
                        IVA = 0,
                        Total = 0
                    };

                    // Traer detalles
                    var detallesResponse = await _client.From<DetalleWeb>()
                                                        .Where(x => x.CotizacionId == cotWeb.Id)
                                                        .Get();

                    decimal sumaSubtotal = 0;

                    foreach (var detWeb in detallesResponse.Models)
                    {
                        // Buscamos producto local
                        var productoLocal = db.Productos.FirstOrDefault(p => p.Descripcion == detWeb.Descripcion);

                        // Si no existe, usamos el comodín. Si aun así no hay comodín (caso raro), saltamos.
                        int idProductoFinal = (productoLocal != null) ? productoLocal.ID : (productoComodin?.ID ?? 0);

                        if (idProductoFinal == 0) continue; // No hay forma de ligarlo

                        var nuevoDetalle = new CotizacionDetalle
                        {
                            ProductoId = idProductoFinal,
                            // Guardamos la descripción original de la web, aunque usemos el ID del comodín
                            Descripcion = detWeb.Descripcion,
                            Cantidad = detWeb.Cantidad,
                            PrecioUnitario = detWeb.Precio,
                            Cotizacion = nuevaCotLocal
                        };

                        nuevaCotLocal.Detalles.Add(nuevoDetalle);
                        sumaSubtotal += (detWeb.Cantidad * detWeb.Precio);
                    }

                    // Si se agregaron detalles, guardamos
                    if (nuevaCotLocal.Detalles.Count > 0)
                    {
                        nuevaCotLocal.Total = sumaSubtotal;
                        nuevaCotLocal.Subtotal = sumaSubtotal / 1.16m; // Ajusta según tu lógica de impuestos
                        nuevaCotLocal.IVA = sumaSubtotal - nuevaCotLocal.Subtotal;

                        db.Cotizaciones.Add(nuevaCotLocal);

                        // ACTUALIZAR EN NUBE A 'DESCARGADA'
                        await _client.From<CotizacionWeb>()
                                     .Where(x => x.Id == cotWeb.Id)
                                     .Set(x => x.Estado, "DESCARGADA")
                                     .Update();

                        importadas++;
                    }
                }
                db.SaveChanges();
            }

            return importadas;
        }

        // --- MÉTODO DE DIAGNÓSTICO RÁPIDO ---
        public async Task<string> ProbarConexionYTraerDatos()
        {
            try
            {
                await Inicializar();

                // 1. Intentamos traer TODO sin filtros, para ver si el problema es el estado 'PENDIENTE'
                var response = await _client.From<CotizacionWeb>().Get();
                var datos = response.Models;

                // 2. Construimos un reporte simple
                if (datos.Count == 0)
                {
                    return "✅ Conexión exitosa, pero Supabase devolvió 0 registros.\n\n" +
                           "Posibles causas:\n" +
                           "- La tabla está vacía.\n" +
                           "- RLS (Seguridad) te está ocultando los datos (usa la 'service_role' key).\n" +
                           "- No coinciden los nombres de tabla/columnas.";
                }

                string reporte = $"¡ÉXITO! Se encontraron {datos.Count} registros en la nube:\n------------------------------------------------\n";

                // Mostramos los primeros 5 para no saturar la ventana
                foreach (var c in datos.Take(5))
                {
                    reporte += $"ID: {c.Id} | Cliente: {c.ClienteNombre} | Total: {c.Total:C} | Estado: {c.Estado}\n";
                }

                if (datos.Count > 5) reporte += "... y más registros.";

                return reporte;
            }
            catch (Exception ex)
            {
                return $"❌ ERROR GRAVE DE CONEXIÓN:\n{ex.Message}";
            }
        }
    }
}