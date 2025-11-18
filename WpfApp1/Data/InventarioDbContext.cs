using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.IO;
using System.Linq; // Necesario para .Any()
using System.Collections.Generic;
using WpfApp1.Models;

namespace WpfApp1.Data
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

        // CONFIGURACIÓN DE LA RUTA DB
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string dbPath = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "inventario.db"
            );
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }

        // --- SEMILLA DE DATOS (SEED DATA) ---
        public void SeedData()
        {
            // Esto crea la DB si no existe (¡vital!)
            this.Database.EnsureCreated();

            // --- 1. SEMBRAR CLIENTES ---
            if (!this.Clientes.Any())
            {
                var listaClientes = new List<Cliente>
                {
                    // El cliente ID 0 o 1 suele ser el genérico
                    new Cliente { RFC = "XAXX010101000", RazonSocial = "Público en General", Telefono = "000-000-0000", Activo = true },
                    
                    // Clientes reales
                    new Cliente { RFC = "GOME900101HDF", RazonSocial = "Abarrotes Doña Lupe", Telefono = "818-123-4567", Activo = true },
                    new Cliente { RFC = "COMP8505201A1", RazonSocial = "Computadoras y Sistemas S.A.", Telefono = "554-888-9999", Activo = true },
                    new Cliente { RFC = "HEPR991212KL2", RazonSocial = "Escuela Primaria Benito Juárez", Telefono = "811-222-3333", Activo = true }
                };

                this.Clientes.AddRange(listaClientes);
                this.SaveChanges(); // Guardamos clientes primero
            }

            // --- 2. SEMBRAR CATEGORÍAS Y PRODUCTOS ---
            if (this.Categorias.Any())
            {
                return; // Si ya hay categorías, asumimos que ya hay productos y no hacemos nada más.
            }

            // --- 3. SEMBRAR GASTOS --- 
            if (!this.Gastos.Any())
            {
                var listaGastos = new List<Gasto>
                {
                    // Usamos los mismos datos de prueba que ya tenías, pero ahora los guardamos en la DB
                    new Gasto { Fecha = new DateTime(2025, 11, 18, 11, 22, 0), Categoria = "Proveedores", Concepto = "Compra de insumos papelería", Usuario = "Juan Pérez", MetodoPago = "Transferencia", Monto = 1500.00m },
                    new Gasto { Fecha = new DateTime(2025, 11, 17, 11, 22, 0), Categoria = "Servicios", Concepto = "Pago de Luz CFE", Usuario = "Admin", MetodoPago = "Tarjeta", Monto = 3200.50m },
                    new Gasto { Fecha = new DateTime(2025, 11, 16, 11, 22, 0), Categoria = "Mantenimiento", Concepto = "Reparación aire acondicionado", Usuario = "Ana Lopéz", MetodoPago = "Efectivo", Monto = 150.00m },
                    new Gasto { Fecha = new DateTime(2025, 11, 13, 11, 22, 0), Categoria = "Alquiler", Concepto = "Renta del local comercial Noviembre", Usuario = "Admin", MetodoPago = "Cheque", Monto = 8700.00m }
                };

                this.Gastos.AddRange(listaGastos);
                this.SaveChanges(); // Guardamos los gastos
            }

            // A) CREAMOS CATEGORÍAS
            var generica = new Categoria { Nombre = "Genérica" };
            var papeleria = new Categoria { Nombre = "Papelería y Oficina" };
            var fiesta = new Categoria { Nombre = "Fiesta y Eventos" };
            var bolsas = new Categoria { Nombre = "Bolsas y Accesorios" };
            var zapateria = new Categoria { Nombre = "Zapatería" };

            // B) CREAMOS SUBCATEGORÍAS (Las guardamos en variables para usarlas luego)
            var subGeneral = new Subcategoria { Nombre = "General" };

            var subPapel = new Subcategoria { Nombre = "Papel" };
            var subCuadernos = new Subcategoria { Nombre = "Cuadernos y agendas" };
            var subPlumas = new Subcategoria { Nombre = "Bolígrafos y Lápices" }; // Agregué esta

            var subGlobos = new Subcategoria { Nombre = "Globos y Decoración" };
            var subDesechables = new Subcategoria { Nombre = "Desechables" };

            var subMochilas = new Subcategoria { Nombre = "Mochilas" };

            var subTenis = new Subcategoria { Nombre = "Tenis" };
            var subZapatos = new Subcategoria { Nombre = "Zapatos de Vestir" };

            // C) ASIGNAMOS HIJOS A PADRES
            generica.Subcategorias.Add(subGeneral);

            papeleria.Subcategorias.Add(subPapel);
            papeleria.Subcategorias.Add(subCuadernos);
            papeleria.Subcategorias.Add(subPlumas);

            fiesta.Subcategorias.Add(subGlobos);
            fiesta.Subcategorias.Add(subDesechables);

            bolsas.Subcategorias.Add(subMochilas);

            zapateria.Subcategorias.Add(subTenis);
            zapateria.Subcategorias.Add(subZapatos);

            // D) CREAMOS PRODUCTOS (Vinculados a las Subcategorías de arriba)
            var productos = new List<Producto>
            {
                // Papelería
                new Producto { Descripcion = "Cuaderno Profesional Scribe Raya", Precio = 28.50m, Costo = 18.00m, Stock = 100, Activo = true, Subcategoria = subCuadernos },
                new Producto { Descripcion = "Paquete Hojas Blancas 500pz", Precio = 115.00m, Costo = 85.00m, Stock = 50, Activo = true, Subcategoria = subPapel },
                new Producto { Descripcion = "Bolígrafo BIC Azul Punto Medio", Precio = 7.00m, Costo = 3.50m, Stock = 200, Activo = true, Subcategoria = subPlumas },
                new Producto { Descripcion = "Caja Colores Maped 12pz", Precio = 65.00m, Costo = 40.00m, Stock = 30, Activo = true, Subcategoria = subCuadernos },

                // Fiesta
                new Producto { Descripcion = "Bolsa Globos #9 Surtido 50pz", Precio = 45.00m, Costo = 25.00m, Stock = 80, Activo = true, Subcategoria = subGlobos },
                new Producto { Descripcion = "Paquete Platos Pastel 20pz", Precio = 32.00m, Costo = 15.00m, Stock = 60, Activo = true, Subcategoria = subDesechables },
                new Producto { Descripcion = "Vela Chispa Mágica", Precio = 15.00m, Costo = 5.00m, Stock = 150, Activo = true, Subcategoria = subGlobos },

                // Zapatería y Bolsas
                new Producto { Descripcion = "Tenis Escolares Blancos Talla 24", Precio = 450.00m, Costo = 280.00m, Stock = 12, Activo = true, Subcategoria = subTenis },
                new Producto { Descripcion = "Zapato Negro Vestir Caballero Talla 28", Precio = 680.00m, Costo = 400.00m, Stock = 8, Activo = true, Subcategoria = subZapatos },
                new Producto { Descripcion = "Mochila Chenson Mario Bros", Precio = 550.00m, Costo = 350.00m, Stock = 5, Activo = true, Subcategoria = subMochilas }
            };

            // E) AGREGAMOS CATEGORÍAS AL CONTEXTO
            // Al agregar las categorías padre, EF Core añade automáticamente 
            // las subcategorías hijas y los productos nietos porque están enlazados.
            this.Categorias.AddRange(generica, papeleria, fiesta, bolsas, zapateria);

            // Por seguridad, agregamos explícitamente los productos también
            this.Productos.AddRange(productos);

            // F) GUARDAR CAMBIOS
            this.SaveChanges();
        }
    }
}