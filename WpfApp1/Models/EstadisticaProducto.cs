namespace OrySiPOS.Models
{
    public class EstadisticaProducto
    {
        public string Nombre { get; set; }
        public int Cantidad { get; set; }
        public decimal Monto { get; set; } // Por si quieres mostrar dinero en vez de unidades
    }
}