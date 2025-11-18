using System.Collections.Generic; // ¡Necesario para ICollection!

namespace WpfApp1.Models
{
    public class Categoria
    {
        // Esta es la Clave Primaria (PK)
        // EF Core es listo y sabe que "Id" (o "CategoriaId") es la PK.
        public int Id { get; set; }

        public string Nombre { get; set; }

        // --- La Relación (Lado "Uno") ---
        // Esto le dice a EF Core: "Una Categoría puede tener una
        // COLECCIÓN de Subcategorías".
        // La palabra 'virtual' ayuda con algo llamado "Lazy Loading" 
        // (carga perezosa), que es útil pero te explico luego si quieres.
        public virtual ICollection<Subcategoria> Subcategorias { get; set; }

        // Un buen constructor para inicializar la lista
        public Categoria()
        {
            Subcategorias = new HashSet<Subcategoria>();
        }
    }
}