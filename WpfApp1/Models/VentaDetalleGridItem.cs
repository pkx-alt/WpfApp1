// --- Models/VentaDetalleGridItem.cs ---
namespace WpfApp1.Models
{
    public class VentaDetalleGridItem
    {
        public string ID { get; set; } // ID/Código del Producto
        public string Descripcion { get; set; }
        public int UD { get; set; } // Unidades
        public decimal Precio { get; set; }
        public decimal Subtotal { get; set; }
    }
}