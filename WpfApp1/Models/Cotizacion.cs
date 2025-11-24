using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WpfApp1.Models
{
    [Table("Cotizaciones")] // Nombre de la tabla en SQL
    public class Cotizacion
    {
        [Key]
        public int ID { get; set; }

        public DateTime FechaEmision { get; set; } = DateTime.Now;

        // Una cotización suele tener vigencia (ej. 15 días)
        public DateTime FechaVencimiento { get; set; }

        // El cliente es opcional en venta rápida, pero RECOMENDADO en cotización.
        // Usamos int? para permitir nulos si es necesario.
        public int? ClienteId { get; set; }
        [ForeignKey("ClienteId")]
        public virtual Cliente Cliente { get; set; }

        public string Origen { get; set; } = "Local";

        // Totales financieros
        public decimal Subtotal { get; set; }
        public decimal IVA { get; set; }
        public decimal Total { get; set; }

        // Relación con los productos cotizados
        public virtual List<CotizacionDetalle> Detalles { get; set; } = new List<CotizacionDetalle>();
    }
}