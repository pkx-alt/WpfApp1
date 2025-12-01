using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;
using System;

namespace OrySiPOS.Models.Supabase
{
    [Table("clientes_web")]
    public class ClienteWeb : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("rfc")]
        public string Rfc { get; set; }

        [Column("razon_social")]
        public string RazonSocial { get; set; }

        // En tu clase ClienteWeb
        [Column("email")] // O como se llame en tu tabla de supabase
        public string Correo { get; set; }

        [Column("telefono")]
        public string Telefono { get; set; }

        [Column("activo")]
        public bool Activo { get; set; }

        [Column("es_factura")]
        public bool EsFactura { get; set; }

        [Column("codigo_postal")]
        public string CodigoPostal { get; set; }

        [Column("regimen_fiscal")]
        public string RegimenFiscal { get; set; }

        [Column("uso_cfdi")]
        public string UsoCfdi { get; set; }

        [Column("creado")]
        public DateTime Creado { get; set; }

        [Column("updated_at")]
        public DateTime UltimaActualizacion { get; set; }
    }
}