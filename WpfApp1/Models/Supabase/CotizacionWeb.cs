using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;
using System;

namespace WpfApp1.Models.Supabase
{
    [Table("cotizaciones_web")]
    public class CotizacionWeb : BaseModel
    {
        [PrimaryKey("id")]
        public long Id { get; set; }

        [Column("cliente_nombre")]
        public string ClienteNombre { get; set; }

        [Column("total")]
        public decimal Total { get; set; }

        [Column("estado")]
        public string Estado { get; set; }

        [Column("created_at")]
        public DateTime FechaCreacion { get; set; }
    }
}