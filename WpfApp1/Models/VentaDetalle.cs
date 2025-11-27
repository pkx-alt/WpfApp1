using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrySiPOS.Models
{
    public class VentaDetalle
    {
        [Key]
        public int VentaDetalleId { get; set; }

        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; } // Precio de venta (Ya lo tenías, ¡bien!)

        // --- ¡NUEVOS CAMPOS (SNAPSHOT)! ---
        // Guardamos esto para que el historial nunca cambie
        public string Descripcion { get; set; }
        public decimal Costo { get; set; }
        // ----------------------------------

        public int ProductoId { get; set; }
        [ForeignKey("ProductoId")]
        public virtual Producto Producto { get; set; }

        public int VentaId { get; set; }
        [ForeignKey("VentaId")]
        public virtual Venta Venta { get; set; }
    }
}