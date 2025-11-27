using System.Collections.Generic;

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
    }
}