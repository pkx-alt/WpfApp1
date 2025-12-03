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
        private readonly string _key = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImdpcXh1bHdramtva3lvbXlsa25lIiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc2MzkyMzc2MiwiZXhwIjoyMDc5NDk5NzYyfQ.1f0UgMaGbGIxwMJ7vijnHelna9XekEJGCnAkLHoPvy0";

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
        // --- REEMPLAZA ESTE MÉTODO EN SupabaseService.cs ---

        public async Task<int> SincronizarCotizaciones()
        {
            await Inicializar();

            try
            {
                // 1. Traer cabeceras PENDIENTES
                // Nota: Asegúrate de que tu RLS en Supabase permita SELECT a 'anon' o usa la Service_Role Key
                var response = await _client.From<CotizacionWeb>()
                                            .Where(x => x.Estado == "PENDIENTE")
                                            .Get();

                var cotizacionesNube = response.Models;
                int importadas = 0;

                if (cotizacionesNube == null || cotizacionesNube.Count == 0) return 0;

                using (var db = new InventarioDbContext())
                {
                    // PASO A: Asegurar Producto Comodín
                    var productoComodin = db.Productos.FirstOrDefault(p => p.Descripcion == "ITEM WEB NO ENCONTRADO");
                    if (productoComodin == null)
                    {
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

                    // PASO B: Procesar CADA cotización
                    foreach (var cotWeb in cotizacionesNube)
                    {
                        try
                        {
                            int? clienteIdFinal = null;

                            // --- 1. LÓGICA DE CLIENTE ---

                            // Intento 1: Buscar localmente por Email
                            if (!string.IsNullOrEmpty(cotWeb.ClienteEmail))
                            {
                                var clienteExistente = db.Clientes.FirstOrDefault(c => c.Correo == cotWeb.ClienteEmail);
                                if (clienteExistente != null) clienteIdFinal = clienteExistente.ID;
                            }

                            // Intento 2: Buscar localmente por Nombre
                            if (clienteIdFinal == null)
                            {
                                var clientePorNombre = db.Clientes.FirstOrDefault(c => c.RazonSocial.ToLower() == cotWeb.ClienteNombre.ToLower());
                                if (clientePorNombre != null)
                                {
                                    clienteIdFinal = clientePorNombre.ID;
                                    // Actualizamos el correo si falta
                                    if (string.IsNullOrEmpty(clientePorNombre.Correo))
                                    {
                                        clientePorNombre.Correo = cotWeb.ClienteEmail;
                                        db.SaveChanges();
                                    }
                                }
                            }

                            // Intento 3: CREAR NUEVO (Si no existe localmente)
                            if (clienteIdFinal == null)
                            {
                                string rfcFinal = "XAXX010101000";
                                string telefonoFinal = "";
                                string cpFinal = "64000"; // CP Genérico
                                string regimenFinal = "616";
                                string usoCfdiFinal = "S01";
                                bool esFacturaFinal = false;

                                // --- RECUPERACIÓN SEGURA DE DATOS EXTRA ---
                                // Usamos Get() + FirstOrDefault para que NO explote si no hay datos
                                if (!string.IsNullOrEmpty(cotWeb.ClienteEmail))
                                {
                                    var respCliente = await _client.From<ClienteWeb>()
                                                                   .Where(c => c.Correo == cotWeb.ClienteEmail)
                                                                   .Get();

                                    var datosClienteNube = respCliente.Models.FirstOrDefault();

                                    if (datosClienteNube != null)
                                    {
                                        if (!string.IsNullOrEmpty(datosClienteNube.Rfc)) rfcFinal = datosClienteNube.Rfc;
                                        if (!string.IsNullOrEmpty(datosClienteNube.Telefono)) telefonoFinal = datosClienteNube.Telefono;
                                        if (!string.IsNullOrEmpty(datosClienteNube.CodigoPostal)) cpFinal = datosClienteNube.CodigoPostal;
                                        if (!string.IsNullOrEmpty(datosClienteNube.RegimenFiscal)) regimenFinal = datosClienteNube.RegimenFiscal;
                                        esFacturaFinal = datosClienteNube.EsFactura;
                                    }
                                }

                                // Validación de RFC Duplicado
                                bool esRfcGenerico = (rfcFinal == "XAXX010101000");
                                var clienteConMismoRfc = db.Clientes.FirstOrDefault(c => c.RFC == rfcFinal);

                                if (clienteConMismoRfc != null && !esRfcGenerico)
                                {
                                    clienteIdFinal = clienteConMismoRfc.ID;
                                }
                                else
                                {
                                    var nuevoCliente = new Cliente
                                    {
                                        RazonSocial = cotWeb.ClienteNombre ?? "Cliente Web",
                                        Correo = cotWeb.ClienteEmail,
                                        RFC = rfcFinal,
                                        Activo = true,
                                        Creado = DateTime.Now,
                                        EsFactura = esFacturaFinal,
                                        Telefono = telefonoFinal,
                                        CodigoPostal = cpFinal,
                                        RegimenFiscal = regimenFinal,
                                        UsoCFDI = usoCfdiFinal
                                    };

                                    db.Clientes.Add(nuevoCliente);
                                    db.SaveChanges(); // Guardamos para obtener el ID
                                    clienteIdFinal = nuevoCliente.ID;
                                }
                            }

                            // --- 2. CREAR COTIZACIÓN LOCAL ---
                            var nuevaCotLocal = new Cotizacion
                            {
                                FechaEmision = cotWeb.FechaCreacion,
                                FechaVencimiento = cotWeb.FechaVencimiento ?? cotWeb.FechaCreacion.AddDays(15),
                                Origen = "Web",
                                ClienteId = clienteIdFinal,
                                Subtotal = 0,
                                IVA = 0,
                                Total = 0
                            };

                            // --- 3. PROCESAR DETALLES ---
                            var detallesResponse = await _client.From<DetalleWeb>()
                                                                .Where(x => x.CotizacionId == cotWeb.Id)
                                                                .Get();

                            decimal sumaTotal = 0;
                            bool tieneDetalles = false;

                            foreach (var detWeb in detallesResponse.Models)
                            {
                                Producto productoLocal = null;

                                // A. Intentar buscar por SKU (ID) primero (Más preciso)
                                if (long.TryParse(detWeb.ProductoSku, out long skuBuscado))
                                {
                                    productoLocal = db.Productos.FirstOrDefault(p => p.ID == skuBuscado);
                                }

                                // B. Si falla, intentar por Descripción
                                if (productoLocal == null)
                                {
                                    productoLocal = db.Productos.FirstOrDefault(p => p.Descripcion == detWeb.Descripcion);
                                }

                                // C. Si falla, usar comodín
                                int idProductoFinal = (productoLocal != null) ? productoLocal.ID : (productoComodin?.ID ?? 0);

                                if (idProductoFinal == 0) continue;

                                var nuevoDetalle = new CotizacionDetalle
                                {
                                    ProductoId = idProductoFinal,
                                    Descripcion = detWeb.Descripcion,
                                    Cantidad = detWeb.Cantidad,
                                    PrecioUnitario = detWeb.Precio,
                                    Cotizacion = nuevaCotLocal // Vinculación EF Core
                                };

                                nuevaCotLocal.Detalles.Add(nuevoDetalle);
                                sumaTotal += (detWeb.Cantidad * detWeb.Precio);
                                tieneDetalles = true;
                            }

                            // --- 4. GUARDAR Y ACTUALIZAR ESTADO ---
                            if (tieneDetalles)
                            {
                                // Recalcular montos finales
                                nuevaCotLocal.Total = sumaTotal;
                                // Asumiendo que el precio web ya incluye IVA
                                nuevaCotLocal.Subtotal = sumaTotal / 1.16m;
                                nuevaCotLocal.IVA = sumaTotal - nuevaCotLocal.Subtotal;

                                db.Cotizaciones.Add(nuevaCotLocal);
                                db.SaveChanges();

                                // Marcar en la nube como DESCARGADA
                                await _client.From<CotizacionWeb>()
                                             .Where(x => x.Id == cotWeb.Id)
                                             .Set(x => x.Estado, "DESCARGADA")
                                             .Update();

                                importadas++;
                            }
                        }
                        catch (Exception exItem)
                        {
                            // Tip de depuración: Si algo falla, descomenta esto para ver el error en pantalla
                            // System.Windows.MessageBox.Show($"Error en cotización {cotWeb.Id}: {exItem.Message}");
                            System.Diagnostics.Debug.WriteLine($"Error importando item: {exItem.Message}");
                        }
                    }
                }
                return importadas;
            }
            catch (Exception ex)
            {
                throw new Exception("Error general en Sync: " + ex.Message);
            }
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

                    // --- AQUÍ MAPEA LOS NUEVOS DATOS ---
                    Costo = productoLocal.Costo,
                    ClaveSat = productoLocal.ClaveSat ?? "01010101", // Default por si es nulo
                    ClaveUnidad = productoLocal.ClaveUnidad ?? "H87",
                    EsServicio = productoLocal.EsServicio,
                    PorcentajeIVA = productoLocal.PorcentajeIVA,
                    // -----------------------------------

                    Categoria = nombreCategoria,
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
                    // 1. CORRECCIÓN DE FORMATO: Convertimos el ID numérico a formato UUID falso
                    Id = $"00000000-0000-0000-0000-{local.ID:D12}",

                    Correo = local.Correo,
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

                // 2. ESTRATEGIA DE CONFLICTO:
                // Le decimos a Supabase: "Si ya existe este EMAIL, actualiza ese registro.
                // Ignora si el ID que te mando no coincide con el que tú tienes".
                var options = new Supabase.Postgrest.QueryOptions
                {
                    OnConflict = "email" // ✅ CORRECTO: Así se llama en .NET
                };

                await _client.From<ClienteWeb>().Upsert(webModel, options);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error sync Cliente: " + ex.Message);
            }
        }

        // ==========================================
        //  SECCIÓN DE BAJADA (DESCARGAR DE LA NUBE)
        // ==========================================

        /// <summary>
        /// Baja las categorías de Supabase y las convierte a tu modelo local.
        /// </summary>
        public async Task<List<Categoria>> ObtenerCategoriasDeNube()
        {
            await Inicializar();

            // 1. Bajamos la lista cruda de la web
            var response = await _client.From<CategoriaWeb>().Get();
            var listaWeb = response.Models;

            var listaLocal = new List<Categoria>();

            // 2. Convertimos cada objeto "Web" a objeto "Local"
            foreach (var itemWeb in listaWeb)
            {
                listaLocal.Add(new Categoria
                {
                    // Tratamos de mantener el mismo ID para que coincidan
                    Id = (int)itemWeb.Id,
                    Nombre = itemWeb.Nombre,
                });
            }

            return listaLocal;
        }

        /// <summary>
        /// Baja las subcategorías. Importante bajarlas antes que los productos.
        /// </summary>
        public async Task<List<Subcategoria>> ObtenerSubcategoriasDeNube()
        {
            await Inicializar();

            var response = await _client.From<SubcategoriaWeb>().Get();
            var listaWeb = response.Models;

            var listaLocal = new List<Subcategoria>();

            foreach (var itemWeb in listaWeb)
            {
                listaLocal.Add(new Subcategoria
                {
                    Id = (int)itemWeb.Id,
                    Nombre = itemWeb.Nombre,
                    CategoriaId = (int)itemWeb.CategoriaId, // Relación con el padre
                });
            }

            return listaLocal;
        }

        /// <summary>
        /// Baja los clientes de la nube.
        /// </summary>
        public async Task<List<Cliente>> ObtenerClientesDeNube()
        {
            await Inicializar();

            var response = await _client.From<ClienteWeb>().Get();
            var listaWeb = response.Models;

            var listaLocal = new List<Cliente>();

            foreach (var w in listaWeb)
            {
                listaLocal.Add(new Cliente
                {
                    // Si el ID web es muy grande y tu ID local es int, 
                    // aquí podría haber conflicto, pero por ahora asumimos que caben.
                    // Lo ideal es buscar por RFC o Correo antes de insertar.
                    RazonSocial = w.RazonSocial,
                    RFC = w.Rfc,
                    Correo = w.Correo,
                    Telefono = w.Telefono,
                    CodigoPostal = w.CodigoPostal,
                    RegimenFiscal = w.RegimenFiscal,
                    UsoCFDI = w.UsoCfdi,
                    Activo = w.Activo,
                    EsFactura = w.EsFactura,
                    Creado = w.Creado
                });
            }

            return listaLocal;
        }

        /// <summary>
        /// OJO ALUMNO: Este devuelve el objeto WEB directo.
        /// ¿Por qué? Porque para convertirlo a Local necesitamos buscar la Subcategoría ID
        /// usando la base de datos, y este servicio NO tiene acceso a la BD local directa.
        /// Esa conversión la haremos en el botón (en el archivo .xaml.cs).
        /// </summary>
        public async Task<List<ProductoWeb>> ObtenerProductosDeNube()
        {
            await Inicializar();

            // Traemos todo. Si son miles, aquí deberíamos paginar, pero para empezar está bien.
            var response = await _client.From<ProductoWeb>().Get();
            return response.Models;
        }

        // --- MÉTODOS DE SUBIDA MASIVA (RAPIDÍSIMOS ⚡) ---

        public async Task SincronizarCategoriasMasivo(List<CategoriaWeb> lista)
        {
            if (lista.Count == 0) return;
            await Inicializar();
            // ¡Enviamos toda la lista de un jalón!
            await _client.From<CategoriaWeb>().Upsert(lista);
        }

        public async Task SincronizarSubcategoriasMasivo(List<SubcategoriaWeb> lista)
        {
            if (lista.Count == 0) return;
            await Inicializar();
            await _client.From<SubcategoriaWeb>().Upsert(lista);
        }

        public async Task SincronizarClientesMasivo(List<ClienteWeb> lista)
        {
            if (lista.Count == 0) return;
            await Inicializar();
            await _client.From<ClienteWeb>().Upsert(lista);
        }

        public async Task SincronizarProductosMasivo(List<ProductoWeb> lista)
        {
            if (lista.Count == 0) return;
            await Inicializar();

            // Supabase a veces se queja si mandamos demasiados de golpe (ej: más de 5000).
            // Si tuvieras miles, haríamos lotes de 1000, pero para empezar envíalos todos.
            await _client.From<ProductoWeb>().Upsert(lista);
        }
    }
}