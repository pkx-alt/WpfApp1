using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WpfApp1.Models
{
    public class MovimientoInventario
    {
        [Key]
        public int Id { get; set; }

        public DateTime Fecha { get; set; }

        // Relación con el producto (para saber qué se movió)
        public int ProductoId { get; set; }
        [ForeignKey("ProductoId")]
        public virtual Producto Producto { get; set; }

        // ¿Qué tipo de movimiento fue? (Entrada, Salida, Venta, Ajuste)
        public string TipoMovimiento { get; set; }

        // ¿Cuántas unidades se movieron?
        public int Cantidad { get; set; }

        // Opcional: Stock que había ANTES y DESPUÉS del movimiento
        // (Esto ayuda mucho a auditar errores)
        public int StockAnterior { get; set; }
        public int StockNuevo { get; set; }

        // Motivo detallado (ej: "Producto dañado", "Compra factura #123")
        public string Motivo { get; set; }

        public string Usuario { get; set; }
    }
}