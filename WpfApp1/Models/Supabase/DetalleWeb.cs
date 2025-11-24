using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;
using System;

namespace WpfApp1.Models.Supabase
{
    [Table("cotizacion_detalles_web")]
    public class DetalleWeb : BaseModel
    {
        [PrimaryKey("id")]
        public int Id { get; set; }

        [Column("cotizacion_id")]
        public int CotizacionId { get; set; }

        [Column("producto_sku")]
        public string ProductoSku { get; set; }

        [Column("cantidad")]
        public int Cantidad { get; set; }

        [Column("precio_unitario")]
        public decimal Precio { get; set; }

        [Column("descripcion")]
        public string Descripcion { get; set; }
    }
}