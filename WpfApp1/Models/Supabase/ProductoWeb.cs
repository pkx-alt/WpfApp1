using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace OrySiPOS.Models.Supabase
{
    [Table("productos_web")]
    public class ProductoWeb : BaseModel
    {
        [PrimaryKey("sku", false)]
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

        // --- NUEVOS CAMPOS ---
        [Column("costo")]
        public decimal Costo { get; set; }

        [Column("clave_sat")]
        public string ClaveSat { get; set; }

        [Column("clave_unidad")]
        public string ClaveUnidad { get; set; }

        [Column("es_servicio")]
        public bool EsServicio { get; set; }

        [Column("porcentaje_iva")]
        public decimal PorcentajeIVA { get; set; }

        [Column("ventas_totales")]
        public int VentasTotales { get; set; }

        [Column("updated_at")]
        public DateTime UltimaActualizacion { get; set; }
    }
}