using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;
using System;

namespace OrySiPOS.Models.Supabase
{
    [Table("productos_web")]
    public class ProductoWeb : BaseModel
    {
        // Mapeamos 'sku' a nuestro ID local
        [PrimaryKey("sku", false)] // 'false' significa que no es autoincremental en la nube, nosotros le damos el valor
        public long Sku { get; set; }

        [Column("descripcion")]
        public string Descripcion { get; set; }

        [Column("precio")]
        public decimal Precio { get; set; }

        [Column("stock")]
        public int Stock { get; set; }

        [Column("categoria")]
        public string Categoria { get; set; }

        [Column("imagen_url")]
        public string ImagenUrl { get; set; }

        [Column("activo")]
        public bool Activo { get; set; }

        // --- ¡NUEVO CAMPO! ---
        // Mapeamos a la columna 'porcentaje_iva' en Postgres
        [Column("porcentaje_iva")]
        public decimal PorcentajeIVA { get; set; }

        [Column("updated_at")]
        public DateTime UltimaActualizacion { get; set; }
    }
}