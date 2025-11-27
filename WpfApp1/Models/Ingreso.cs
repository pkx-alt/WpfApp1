using System;
using System.ComponentModel.DataAnnotations;

namespace OrySiPOS.Models
{
    public class Ingreso
    {
        [Key]
        public int Id { get; set; }

        public DateTime Fecha { get; set; }
        public string Categoria { get; set; } // Ej: "Aporte Dueño", "Cambio", "Otros"
        public string Concepto { get; set; }
        public string Usuario { get; set; }
        public decimal Monto { get; set; }
    }
}