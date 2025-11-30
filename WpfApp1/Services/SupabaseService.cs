using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OrySiPOS.Data;            // Para acceder a tu InventarioDbContext (SQLite)
using OrySiPOS.Models;          // Para tus modelos locales (Cotizacion, etc)
using OrySiPOS.Models.Supabase; // Para los modelos nube que acabas de arreglar (CotizacionWeb)

namespace OrySiPOS.Services
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

        /// <summary>
        /// Toma un producto local y lo sube a la nube (Crea o Actualiza)
        /// </summary>
        public async Task SincronizarProducto(Producto productoLocal)
        {
            try
            {
                // NUEVO: Si es servicio, abortamos la misión. No se sube nada.
                if (productoLocal.EsServicio) return;
                await Inicializar();

                // --- PASO EXTRA: Calcular ventas totales desde tu BD Local ---
                int totalVendido = 0;

                // Abrimos una conexión rápida solo para contar
                using (var db = new InventarioDbContext())
                {
                    // Sumamos la columna 'Cantidad' de la tabla 'VentasDetalle' para este producto
                    totalVendido = db.VentasDetalle
                                     .Where(d => d.ProductoId == productoLocal.ID)
                                     .Sum(d => d.Cantidad);
                }
                // -------------------------------------------------------------

                // 1. Preparamos el objeto para la nube
                string nombreCategoria = "General";
                if (productoLocal.Subcategoria != null && productoLocal.Subcategoria.Categoria != null)
                {
                    nombreCategoria = productoLocal.Subcategoria.Categoria.Nombre;
                }

                var productoWeb = new ProductoWeb
                {
                    Sku = productoLocal.ID,
                    Descripcion = productoLocal.Descripcion,
                    Precio = productoLocal.Precio,
                    Stock = productoLocal.Stock,
                    Activo = productoLocal.Activo,
                    ImagenUrl = productoLocal.ImagenUrl,
                    PorcentajeIVA = productoLocal.PorcentajeIVA,
                    Categoria = nombreCategoria,

                    // ¡AQUÍ VA EL DATO CLAVE!
                    VentasTotales = totalVendido,

                    UltimaActualizacion = DateTime.Now
                };

                // 2. ¡Upsert!
                await _client.From<ProductoWeb>().Upsert(productoWeb);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al subir producto: {ex.Message}");
            }
        }

        public async Task SincronizarCategoria(Categoria local)
        {
            try
            {
                await Inicializar();
                var webModel = new CategoriaWeb
                {
                    Id = local.Id,
                    Nombre = local.Nombre,
                    UltimaActualizacion = DateTime.Now
                };
                await _client.From<CategoriaWeb>().Upsert(webModel);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Error sync Categoria: " + ex.Message); }
        }

        public async Task SincronizarSubcategoria(Subcategoria local)
        {
            try
            {
                await Inicializar();
                var webModel = new SubcategoriaWeb
                {
                    Id = local.Id,
                    Nombre = local.Nombre,
                    CategoriaId = local.CategoriaId,
                    UltimaActualizacion = DateTime.Now
                };
                await _client.From<SubcategoriaWeb>().Upsert(webModel);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Error sync Subcategoria: " + ex.Message); }
        }

        public async Task EliminarSubcategoria(long id)
        {
            try
            {
                await Inicializar();
                // Borramos donde el ID coincida
                await _client.From<SubcategoriaWeb>().Where(x => x.Id == id).Delete();
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Error delete Subcategoria: " + ex.Message); }
        }

        public async Task EliminarCategoria(long id)
        {
            try
            {
                await Inicializar();
                await _client.From<CategoriaWeb>().Where(x => x.Id == id).Delete();
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Error delete Categoria: " + ex.Message); }
        }

        public async Task SincronizarCliente(Cliente local)
        {
            try
            {
                await Inicializar();

                var webModel = new ClienteWeb
                {
                    Id = local.ID,
                    Rfc = local.RFC,
                    RazonSocial = local.RazonSocial,
                    Telefono = local.Telefono,
                    Activo = local.Activo,
                    EsFactura = local.EsFactura,
                    CodigoPostal = local.CodigoPostal,
                    RegimenFiscal = local.RegimenFiscal,
                    UsoCfdi = local.UsoCFDI,
                    Creado = local.Creado,
                    UltimaActualizacion = DateTime.Now
                };

                await _client.From<ClienteWeb>().Upsert(webModel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error sync Cliente: " + ex.Message);
            }
        }
    }
}