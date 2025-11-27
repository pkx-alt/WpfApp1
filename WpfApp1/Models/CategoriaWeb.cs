using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;
using System;

namespace OrySiPOS.Models.Supabase
{
    [Table("categorias_web")]
    public class CategoriaWeb : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; }

        [Column("updated_at")]
        public DateTime UltimaActualizacion { get; set; }
    }
}