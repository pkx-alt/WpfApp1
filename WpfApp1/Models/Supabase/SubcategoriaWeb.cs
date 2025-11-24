using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;
using System;

namespace WpfApp1.Models.Supabase
{
    [Table("subcategorias_web")]
    public class SubcategoriaWeb : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; }

        [Column("categoria_id")]
        public long CategoriaId { get; set; }

        [Column("updated_at")]
        public DateTime UltimaActualizacion { get; set; }
    }
}