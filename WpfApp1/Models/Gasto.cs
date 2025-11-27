using System;
using System.ComponentModel.DataAnnotations; // Necesario para [Key]

namespace OrySiPOS.Models
{
    public class Gasto
    {
        [Key] // Esto marca la propiedad como clave primaria
        public int Id { get; set; }

        public DateTime Fecha { get; set; }
        public string Categoria { get; set; }
        public string Concepto { get; set; }
        public string Usuario { get; set; } // Podría ser una relación con un modelo de Usuario
        public string MetodoPago { get; set; }
        public decimal Monto { get; set; }
    }
}