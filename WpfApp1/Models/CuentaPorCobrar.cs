using System;

namespace OrySiPOS.Models
{
    public class CuentaPorCobrar
    {
        public int ClienteId { get; set; }
        public string NombreCompleto { get; set; }
        public decimal TotalDeuda { get; set; }
        public int NumeroDeVentasPendientes { get; set; }
        // Usamos la fecha del ticket más viejo pendiente para saber desde cuándo debe
        public DateTime FechaMasAntigua { get; set; } 
    }
}