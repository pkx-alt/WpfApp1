using System;

namespace WpfApp1.Models
{
    public class DetalleDeuda
    {
        public int VentaId { get; set; } // Necesitamos saber qué venta es para abonarle
        public string Folio { get; set; } // "Ticket #105"
        public DateTime Fecha { get; set; }
        public string Concepto { get; set; } // "Venta de 5 artículos"
        public decimal MontoOriginal { get; set; }
        public decimal PagadoHastaAhora { get; set; }

        // Propiedad calculada: Lo que falta por pagar
        public decimal SaldoPendiente => MontoOriginal - PagadoHastaAhora;
    }
}