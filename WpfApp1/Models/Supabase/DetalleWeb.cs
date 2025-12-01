using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace OrySiPOS.Models.Supabase
{
    [Table("cotizacion_detalles_web")]
    public class DetalleWeb : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("cotizacion_id")]
        public long CotizacionId { get; set; }

        // En tu SQL es 'text', así que aquí usamos string para evitar errores de conversión
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