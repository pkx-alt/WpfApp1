using System.ComponentModel.DataAnnotations; // Necesario para [Key]

namespace OrySiPOS.Models
{
    public class Producto
    {
        [Key] // Esto le dice a EF Core que 'ID' es la clave primaria.
        public int ID { get; set; }

        public string Descripcion { get; set; }

        public decimal Precio { get; set; }

        public decimal Costo { get; set; }

        // ¡Ojo aquí! La ganancia la calculamos, no la guardamos.
        // El DataGrid la leerá igual.
        public decimal Ganancia => Precio - Costo;

        public int Stock { get; set; }

        public string ImagenUrl { get; set; } // Ruta a la imagen
        // --- ¡AQUÍ ESTÁ LA NUEVA PROPIEDAD! ---
        public bool Activo { get; set; }
        // --- ¡AÑADE ESTAS DOS LÍNEAS! ---

        // 1. La Clave Foránea (FK)
        //    Esta columna guardará el 'Id' de la Subcategoría
        //    a la que este producto pertenece.
        public int SubcategoriaId { get; set; }

        // --- DATOS FISCALES CFDI CONCEPTO ---
        [MaxLength(8)]
        public string ClaveSat { get; set; } // Clave de Producto/Servicio

        [MaxLength(3)]
        public string ClaveUnidad { get; set; } // Clave de Unidad (Ej. H87 - Pieza)
        // ------------------------------------

        // 2. La Propiedad de Navegación
        //    Esto le dice a EF Core: "Ese 'SubcategoriaId' de arriba
        //    se 'enlaza' a un objeto 'Subcategoria' completo".
        public virtual Subcategoria Subcategoria { get; set; }
        public Producto()
        {
            // Es buena idea inicializar valores por defecto
            Descripcion = string.Empty;
            ImagenUrl = string.Empty;
            Activo = true; // Por defecto, un producto siempre nace "Activo"
            ClaveSat = "01010101"; // Genérico: "No existe en el catálogo"
            ClaveUnidad = "H87";    // Genérico: "Pieza"
        }
    }
}