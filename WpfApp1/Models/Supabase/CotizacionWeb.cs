using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Collections.Generic;

namespace OrySiPOS.Models.Supabase
{
    [Table("cotizaciones_web")]
    public class CotizacionWeb : BaseModel
    {
        // Es identity en SQL, así que aquí sí dejamos que Supabase lo genere si es subida nueva
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("created_at")]
        public DateTime FechaCreacion { get; set; }

        [Column("cliente_nombre")]
        public string ClienteNombre { get; set; }

        [Column("cliente_email")]
        public string ClienteEmail { get; set; }

        [Column("total")]
        public decimal Total { get; set; }

        [Column("estado")]
        public string Estado { get; set; } // PENDIENTE, DESCARGADA

        [Column("fecha_vencimiento")]
        public DateTime FechaVencimiento { get; set; }

        // Relación con los detalles (opcional, pero útil si usas joins)
        [Reference(typeof(DetalleWeb))]
        public List<DetalleWeb> Detalles { get; set; }
    }
}