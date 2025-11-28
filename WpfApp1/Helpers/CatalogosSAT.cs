using Microsoft.EntityFrameworkCore;
using OrySiPOS.Data;
using OrySiPOS.Models;
using System.Collections.Generic;
using System.Globalization;

namespace OrySiPOS.Helpers
{
    // Clase auxiliar para transportar datos
    public class OpcionSAT
    {
        public string Clave { get; set; }
        public string Descripcion { get; set; }
        public string Display => $"{Clave} - {Descripcion}";
    }

    public static class CatalogosSAT
    {

        // --- CACHÉ EN MEMORIA (Para velocidad extrema) ---
        // Guardamos la lista aquí para no ir a la BD en cada búsqueda
        private static List<SatProducto> _cacheProductos = null;

        public static List<SatProducto> BuscarPorDescripcion(string texto)
        {
            // 1. Si es la primera vez, cargamos todo el catálogo a la RAM
            if (_cacheProductos == null)
            {
                using (var db = new InventarioDbContext())
                {
                    // AsNoTracking() hace que sea mucho más rápido y ligero
                    _cacheProductos = db.SatProductos.AsNoTracking().ToList();
                }
            }

            // 2. Preparamos las herramientas de comparación (Ignorar Mayúsculas y Acentos)
            var compareInfo = CultureInfo.InvariantCulture.CompareInfo;
            var opciones = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

            // 3. Lógica "Google Style" (Palabras separadas)
            var palabras = texto.Split(' ')
                                .Where(x => x.Length > 1) // Ignoramos letras sueltas
                                .ToList();

            // Empezamos con todo el universo de productos en memoria
            IEnumerable<SatProducto> resultados = _cacheProductos;

            // Filtramos palabra por palabra
            foreach (var palabra in palabras)
            {
                // Usamos IndexOf con las opciones de ignorar acentos
                // Si IndexOf devuelve >= 0, significa que encontró la palabra
                resultados = resultados.Where(p =>
                    compareInfo.IndexOf(p.Descripcion, palabra, opciones) >= 0 ||
                    p.Clave.StartsWith(palabra) // La clave sí debe ser exacta al inicio
                );
            }

            // 4. Devolvemos los primeros 50
            return resultados.OrderBy(p => p.Descripcion.Length) // Priorizar los nombres cortos
                             .Take(50)
                             .ToList();
        }

        // --- Listas auxiliares pequeñas (se quedan igual) ---
        public static List<SatUnidad> ObtenerUnidades()
        {
            using (var db = new InventarioDbContext())
            {
                return db.SatUnidades.AsNoTracking().OrderBy(u => u.Descripcion).ToList();
            }
        }
        // --- 1. PREPARADO PARA CLIENTES (Lo usaremos en el siguiente paso) ---
        public static List<OpcionSAT> Regimenes = new List<OpcionSAT>
        {
            new OpcionSAT { Clave = "616", Descripcion = "Sin obligaciones fiscales (Público Gral)" },
            new OpcionSAT { Clave = "626", Descripcion = "RESICO (Simplificado de Confianza)" },
            new OpcionSAT { Clave = "612", Descripcion = "Personas Físicas (Empresarial/Profesional)" },
            new OpcionSAT { Clave = "601", Descripcion = "General de Ley Personas Morales" },
            new OpcionSAT { Clave = "603", Descripcion = "Personas Morales con Fines no Lucrativos" },
            new OpcionSAT { Clave = "605", Descripcion = "Sueldos y Salarios" }
        };

        public static List<OpcionSAT> UsosCFDI = new List<OpcionSAT>
        {
            new OpcionSAT { Clave = "S01", Descripcion = "Sin efectos fiscales (Público Gral / Nómina)" },
            new OpcionSAT { Clave = "G03", Descripcion = "Gastos en general (Oficina, insumos)" },
            new OpcionSAT { Clave = "G01", Descripcion = "Adquisición de mercancías (Para revender)" },
            new OpcionSAT { Clave = "I04", Descripcion = "Equipo de cómputo y accesorios" },
            new OpcionSAT { Clave = "I02", Descripcion = "Mobiliario y equipo de oficina" }
        };

        // --- 2. DATOS INICIALES PARA PRODUCTOS (Para sembrar la BD) ---
        public static List<OpcionSAT> UnidadesIniciales = new List<OpcionSAT>
        {
            new OpcionSAT { Clave = "H87", Descripcion = "Pieza (Lápiz, cuaderno, goma)" },
            new OpcionSAT { Clave = "E48", Descripcion = "Unidad de servicio (Copias, engargolado)" },
            new OpcionSAT { Clave = "XPK", Descripcion = "Paquete (Hojas, sobres)" },
            new OpcionSAT { Clave = "XBX", Descripcion = "Caja (Clips, plumas mayoreo)" },
            new OpcionSAT { Clave = "KT",  Descripcion = "Kit (Juego geometría, pqte escolar)" },
            new OpcionSAT { Clave = "MTR", Descripcion = "Metro (Plástico, listón, cable)" }
        };

        public static List<OpcionSAT> ProductosIniciales = new List<OpcionSAT>
        {
            new OpcionSAT { Clave = "01010101", Descripcion = "No existe en el catálogo" },
            new OpcionSAT { Clave = "14111507", Descripcion = "Papel para impresora o fotocopiadora" },
            new OpcionSAT { Clave = "44121700", Descripcion = "Instrumentos de escritura (Plumas, lápices)" },
            new OpcionSAT { Clave = "14111500", Descripcion = "Papel de imprenta y escribir (Cuadernos)" },
            new OpcionSAT { Clave = "44122000", Descripcion = "Carpetas y archiveros" },
            new OpcionSAT { Clave = "44121600", Descripcion = "Suministros de escritorio (Grapas, clips)" },
            new OpcionSAT { Clave = "44103100", Descripcion = "Cartuchos de tinta" },
            new OpcionSAT { Clave = "60121000", Descripcion = "Pinturas y medios (Acuarelas, óleo)" },
            new OpcionSAT { Clave = "53131600", Descripcion = "Baterías o pilas" }
        };

        //public static List<SatProducto> BuscarPorDescripcion(string texto)
        //{
        //    using (var db = new InventarioDbContext())
        //    {
        //        // 1. Si el texto es una clave numérica exacta (ej: "01010101"), buscar directo
        //        if (long.TryParse(texto, out _))
        //        {
        //            return db.SatProductos
        //                     .Where(p => p.Clave.StartsWith(texto))
        //                     .Take(20)
        //                     .ToList();
        //        }

        //        // 2. Búsqueda inteligente por palabras
        //        // Si escribe "aceite carro", separamos en ["aceite", "carro"]
        //        var palabras = texto.Split(' ').Where(x => x.Length > 2).ToList();

        //        var query = db.SatProductos.AsQueryable();

        //        foreach (var palabra in palabras)
        //        {
        //            string p = palabra; // Variable local para EF
        //                                // Filtramos registros que contengan CADA una de las palabras
        //            query = query.Where(x => x.Descripcion.Contains(p));
        //        }

        //        return query.OrderBy(p => p.Descripcion)
        //                    .Take(50) // Traemos solo 50 para no saturar
        //                    .ToList();
        //    }
        //}
    }


}