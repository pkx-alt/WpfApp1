using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrySiPOS.Models
{
    public class VentaDetalle
    {
        [Key]
        public int VentaDetalleId { get; set; } // Clave primaria del detalle

        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; } // Precio al momento de la venta

        // --- Claves Foráneas (Foreign Keys) ---

        // 1. Enlace al Producto
        public int ProductoId { get; set; }
        [ForeignKey("ProductoId")]
        public virtual Producto Producto { get; set; }

        // 2. Enlace a la Venta (el "ticket" padre)
        public int VentaId { get; set; }
        [ForeignKey("VentaId")]
        public virtual Venta Venta { get; set; }
    }
}