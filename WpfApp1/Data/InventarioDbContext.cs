using Microsoft.EntityFrameworkCore;
using OrySiPOS.Helpers;
using OrySiPOS.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq; // Necesario para .Any()
using System.Reflection;
using System.Text;
using System.Windows;

namespace OrySiPOS.Data
{
    public class InventarioDbContext : DbContext
    {
        // TUS TABLAS
        public DbSet<Producto> Productos { get; set; }
        public DbSet<Venta> Ventas { get; set; }
        public DbSet<VentaDetalle> VentasDetalle { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Subcategoria> Subcategorias { get; set; }
        public DbSet<Cotizacion> Cotizaciones { get; set; }
        public DbSet<CotizacionDetalle> CotizacionDetalles { get; set; }
        // ¡NUESTRA TABLA DE GASTOS!
        public DbSet<Gasto> Gastos { get; set; }
        public DbSet<Ingreso> Ingresos { get; set; } // <--- AGREGA ESTO
        public DbSet<MovimientoInventario> Movimientos { get; set; }
        public DbSet<CorteCaja> CortesCaja { get; set; }
        // ¡AGREGA ESTA LÍNEA! 👇
        public DbSet<Factura> Facturas { get; set; }

        public DbSet<HistorialImportacion> HistorialImportaciones { get; set; }


        // --- ¡NUEVAS TABLAS SAT! ---
        public DbSet<SatProducto> SatProductos { get; set; }
        public DbSet<SatUnidad> SatUnidades { get; set; }
        // CONFIGURACIÓN DE LA RUTA DB
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string dbPath = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "inventario_v3.db"
            );
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }

        // Asegúrate de tener arriba: using System.IO; y using System.Text;

        public void ImportarCatalogoSAT(string rutaArchivo)
        {
            // Usamos UTF8 (o Latin1 si ves caracteres raros) para leer el archivo
            var lineas = File.ReadAllLines(rutaArchivo, Encoding.UTF8);

            var listaNuevos = new List<SatProducto>();

            // Empezamos en i = 5 porque en tu archivo las primeras 5 líneas son encabezados/basura
            //
            for (int i = 5; i < lineas.Length; i++)
            {
                var linea = lineas[i];
                if (string.IsNullOrWhiteSpace(linea)) continue;

                // Separamos por comas
                var partes = linea.Split(',');

                // Validamos que la línea tenga datos útiles
                if (partes.Length >= 2)
                {
                    string clave = partes[0].Trim();
                    string descripcion = partes[1].Trim();

                    // LIMPIEZA DE DATOS (Tips de Senior):
                    // 1. Excel a veces agrega ".0" al final de los números convertidos a texto
                    if (clave.EndsWith(".0")) clave = clave.Replace(".0", "");

                    // 2. Quitamos comillas si las hubiera
                    descripcion = descripcion.Replace("\"", "");

                    // 3. Validamos que sea una clave real (8 dígitos)
                    if (clave.Length >= 8)
                    {
                        // Cortamos a 8 por si acaso viene basura extra
                        clave = clave.Substring(0, 8);

                        listaNuevos.Add(new SatProducto
                        {
                            Clave = clave,
                            Descripcion = descripcion
                        });
                    }
                }
            }

            // Guardado Masivo (Bulk Insert)
            if (listaNuevos.Count > 0)
            {
                // Limpiamos la tabla primero para no duplicar si re-importas
                // (Ojo: Esto borra todo lo anterior en SatProductos)
                this.SatProductos.RemoveRange(this.SatProductos);
                this.SaveChanges();

                // Insertamos los nuevos (AddRange es mucho más rápido que Add uno por uno)
                this.SatProductos.AddRange(listaNuevos);
                this.SaveChanges();
            }
        }

        // --- SEMILLA DE DATOS (SEED DATA) ---

        public void SeedData()
        {
            // 1. Aseguramos que la BD exista
            this.Database.EnsureCreated();

            // --- 2. SEMBRAR CATÁLOGO SAT: UNIDADES ---
            if (!this.SatUnidades.Any())
            {
                foreach (var item in CatalogosSAT.UnidadesIniciales)
                {
                    this.SatUnidades.Add(new SatUnidad { Clave = item.Clave, Descripcion = item.Descripcion });
                }
            }

            // --- 3. SEMBRAR CATÁLOGO SAT: PRODUCTOS ---
            if (!this.SatProductos.Any())
            {
                foreach (var item in CatalogosSAT.ProductosIniciales)
                {
                    this.SatProductos.Add(new SatProducto { Clave = item.Clave, Descripcion = item.Descripcion });
                }
            }

            // --- 4. SEMBRAR CLIENTES ---
            if (!this.Clientes.Any())
            {
                var listaClientes = new List<Cliente>
        {
            // Cliente Genérico
            new Cliente {
                RFC = "XAXX010101000",
                RazonSocial = "Público en General",
                Telefono = "000-000-0000",
                Activo = true,
                CodigoPostal = "00000",
                RegimenFiscal = "616",
                UsoCFDI = "S01"
            },
            // Clientes de Prueba
            new Cliente {
                RFC = "GOME900101HDF",
                RazonSocial = "Abarrotes Doña Lupe",
                Telefono = "818-123-4567",
                Activo = true,
                CodigoPostal = "64000",
                RegimenFiscal = "612", // Persona Física Actividad Empresarial
                UsoCFDI = "G01"        // Adquisición de mercancías
            },
            new Cliente {
                RFC = "HEPR991212KL2",
                RazonSocial = "Escuela Primaria Benito Juárez",
                Telefono = "811-222-3333",
                Activo = true,
                CodigoPostal = "66000",
                RegimenFiscal = "603", // Personas Morales con Fines no Lucrativos
                UsoCFDI = "G03"        // Gastos en general
            }
        };

                this.Clientes.AddRange(listaClientes);
                this.SaveChanges();
            }

            // --- 5. SEMBRAR CATEGORÍAS Y PRODUCTOS ---
            if (!this.Categorias.Any())
            {
                // A) Categorías
                var generica = new Categoria { Nombre = "Genérica" };
                var papeleria = new Categoria { Nombre = "Papelería y Oficina" };
                var fiesta = new Categoria { Nombre = "Fiesta y Eventos" };
                var bolsas = new Categoria { Nombre = "Bolsas y Accesorios" };
                var zapateria = new Categoria { Nombre = "Zapatería" };

                // B) Subcategorías
                var subGeneral = new Subcategoria { Nombre = "General" };
                var subPapel = new Subcategoria { Nombre = "Papel" };
                var subCuadernos = new Subcategoria { Nombre = "Cuadernos y agendas" };
                var subPlumas = new Subcategoria { Nombre = "Bolígrafos y Lápices" };
                var subGlobos = new Subcategoria { Nombre = "Globos y Decoración" };
                var subDesechables = new Subcategoria { Nombre = "Desechables" };
                var subMochilas = new Subcategoria { Nombre = "Mochilas" };
                var subTenis = new Subcategoria { Nombre = "Tenis" };
                var subZapatos = new Subcategoria { Nombre = "Zapatos de Vestir" };

                // C) Asignar Hijos
                generica.Subcategorias.Add(subGeneral);
                papeleria.Subcategorias.Add(subPapel);
                papeleria.Subcategorias.Add(subCuadernos);
                papeleria.Subcategorias.Add(subPlumas);
                fiesta.Subcategorias.Add(subGlobos);
                fiesta.Subcategorias.Add(subDesechables);
                bolsas.Subcategorias.Add(subMochilas);
                zapateria.Subcategorias.Add(subTenis);
                zapateria.Subcategorias.Add(subZapatos);

                // D) Productos con Datos SAT básicos
                var productos = new List<Producto>
        {
            new Producto { Descripcion = "Cuaderno Profesional Scribe Raya", Precio = 28.50m, Costo = 18.00m, Stock = 100, Activo = true, Subcategoria = subCuadernos, ClaveSat = "14111500", ClaveUnidad = "H87" },
            new Producto { Descripcion = "Paquete Hojas Blancas 500pz", Precio = 115.00m, Costo = 85.00m, Stock = 50, Activo = true, Subcategoria = subPapel, ClaveSat = "14111507", ClaveUnidad = "XPK" },
            new Producto { Descripcion = "Bolígrafo BIC Azul Punto Medio", Precio = 7.00m, Costo = 3.50m, Stock = 200, Activo = true, Subcategoria = subPlumas, ClaveSat = "44121700", ClaveUnidad = "H87" },
            new Producto { Descripcion = "Bolsa Globos #9 Surtido 50pz", Precio = 45.00m, Costo = 25.00m, Stock = 80, Activo = true, Subcategoria = subGlobos, ClaveSat = "01010101", ClaveUnidad = "XPK" },
            new Producto { Descripcion = "Tenis Escolares Blancos Talla 24", Precio = 450.00m, Costo = 280.00m, Stock = 12, Activo = true, Subcategoria = subTenis, ClaveSat = "01010101", ClaveUnidad = "H87" }
        };

                this.Categorias.AddRange(generica, papeleria, fiesta, bolsas, zapateria);
                this.Productos.AddRange(productos);
                this.SaveChanges();
            }

            // --- 6. SEMBRAR GASTOS ---
            if (!this.Gastos.Any())
            {
                var listaGastos = new List<Gasto>
        {
            new Gasto { Fecha = DateTime.Now.AddDays(-5), Categoria = "Proveedores", Concepto = "Compra de insumos", Usuario = "Admin", MetodoPago = "Transferencia", Monto = 1500.00m },
            new Gasto { Fecha = DateTime.Now.AddDays(-2), Categoria = "Servicios", Concepto = "Pago de Luz", Usuario = "Admin", MetodoPago = "Efectivo", Monto = 300.00m }
        };
                this.Gastos.AddRange(listaGastos);
                this.SaveChanges();
            }

            // --- 7. SEMBRAR VENTAS (EL FIX ESTÁ AQUÍ) ---
            if (!this.Ventas.Any())
            {
                // Buscamos datos necesarios
                var clienteTienda = this.Clientes.FirstOrDefault(c => c.RazonSocial.Contains("Doña Lupe"));
                var clienteEscuela = this.Clientes.FirstOrDefault(c => c.RazonSocial.Contains("Benito Juárez"));
                var productoCuaderno = this.Productos.FirstOrDefault(p => p.Descripcion.Contains("Cuaderno"));
                var productoLapiz = this.Productos.FirstOrDefault(p => p.Descripcion.Contains("Bolígrafo"));

                if (clienteTienda != null && clienteEscuela != null && productoCuaderno != null)
                {
                    // Venta 1: Crédito Total
                    var ventaCreditoTotal = new Venta
                    {
                        Fecha = DateTime.Now.AddDays(-5),
                        ClienteId = clienteTienda.ID,
                        Subtotal = productoCuaderno.Precio * 10,
                        IVA = (productoCuaderno.Precio * 10) * 0.16m,
                        Total = (productoCuaderno.Precio * 10) * 1.16m,
                        PagoRecibido = 0,
                        Cambio = 0,
                        FormaPagoSAT = "99",
                        MetodoPagoSAT = "PPD"
                    };

                    // AGREGAR DETALLE CON FOTO (SNAPSHOT)
                    ventaCreditoTotal.Detalles.Add(new VentaDetalle
                    {
                        ProductoId = productoCuaderno.ID,
                        Cantidad = 10,
                        PrecioUnitario = productoCuaderno.Precio,
                        // ¡CORRECCIÓN APLICADA AQUÍ! 👇
                        Descripcion = productoCuaderno.Descripcion,
                        Costo = productoCuaderno.Costo
                    });

                    // Venta 2: Abonada
                    var ventaAbonada = new Venta
                    {
                        Fecha = DateTime.Now.AddDays(-2),
                        ClienteId = clienteEscuela.ID,
                        Subtotal = 500,
                        IVA = 80,
                        Total = 580,
                        PagoRecibido = 200.00m,
                        Cambio = 0,
                        FormaPagoSAT = "99",
                        MetodoPagoSAT = "PPD"
                    };

                    var prodParaVenta2 = productoLapiz ?? productoCuaderno;

                    // AGREGAR DETALLE CON FOTO (SNAPSHOT)
                    ventaAbonada.Detalles.Add(new VentaDetalle
                    {
                        ProductoId = prodParaVenta2.ID,
                        Cantidad = 50,
                        PrecioUnitario = 10,
                        // ¡CORRECCIÓN APLICADA AQUÍ! 👇
                        Descripcion = prodParaVenta2.Descripcion,
                        Costo = prodParaVenta2.Costo
                    });

                    // Venta 3: Contado (Sin detalles en el ejemplo original, pero si los agregaras, recuerda poner Descripcion/Costo)
                    var ventaContado = new Venta
                    {
                        Fecha = DateTime.Now.AddDays(-1),
                        ClienteId = clienteEscuela.ID,
                        Subtotal = 100,
                        IVA = 16,
                        Total = 116,
                        PagoRecibido = 116,
                        Cambio = 0,
                        FormaPagoSAT = "01",
                        MetodoPagoSAT = "PUE"
                    };

                    this.Ventas.AddRange(ventaCreditoTotal, ventaAbonada, ventaContado);
                    this.SaveChanges();
                }
            }

            // Guardado final por seguridad
            this.SaveChanges();
        }

    }
}