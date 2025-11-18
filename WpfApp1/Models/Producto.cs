using System.ComponentModel.DataAnnotations; // Necesario para [Key]

namespace WpfApp1.Models
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
        }
    }
}