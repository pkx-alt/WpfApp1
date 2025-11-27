// Models/Subcategoria.cs

// ¡Añade este 'using' en la parte de arriba!
using System.ComponentModel.DataAnnotations.Schema;

namespace OrySiPOS.Models
{
    public class Subcategoria
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public int CategoriaId { get; set; }
        public virtual Categoria Categoria { get; set; }

        // --- ¡AÑADE ESTA LÍNEA! ---
        // Esto le dice a EF Core: "Ignora esta propiedad.
        // No es una columna de la base de datos."
        [NotMapped]
        public bool IsSelected { get; set; }
    }
}