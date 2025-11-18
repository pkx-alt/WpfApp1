using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WpfApp1.Models
{
    [Table("CotizacionDetalles")]
    public class CotizacionDetalle
    {
        [Key]
        public int ID { get; set; }

        // Relación con la Cotización padre
        public int CotizacionId { get; set; }
        [ForeignKey("CotizacionId")]
        public virtual Cotizacion Cotizacion { get; set; }

        // Relación con el Producto
        public int ProductoId { get; set; }
        [ForeignKey("ProductoId")]
        public virtual Producto Producto { get; set; }

        // Guardamos estos datos por si el precio cambia en el futuro,
        // la cotización antigua respete el precio original.
        public string Descripcion { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal ImporteTotal => Cantidad * PrecioUnitario;
    }
}