using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrySiPOS.Models
{
    public class Factura
    {
        [Key]
        public int Id { get; set; }

        // Datos Fiscales simulados
        public string UUID { get; set; } // El folio fiscal largo (ej: A1B2C3...)
        public string SerieFolio { get; set; } // Tu folio interno (ej: F-001)

        // Datos del Receptor (guardamos una copia por si el cliente cambia sus datos después)
        public string ReceptorNombre { get; set; }
        public string ReceptorRFC { get; set; }

        public DateTime FechaEmision { get; set; }
        public decimal Total { get; set; }

        // Estado: "Vigente" o "Cancelada"
        public string Estado { get; set; }

        // Rutas de archivos (para abrir el PDF/XML después)
        public string ArchivoXml { get; set; }
        public string ArchivoPdf { get; set; }

        // Relación: ¿De qué venta vino esta factura?
        public int VentaId { get; set; }

        // Propiedad de navegación (opcional, por si quieres acceder a los detalles de la venta desde la factura)
        [ForeignKey("VentaId")]
        public virtual Venta VentaOriginal { get; set; }
    }
}