using System;
using System.ComponentModel.DataAnnotations;

namespace WpfApp1.Models
{
    public class CorteCaja
    {
        [Key]
        public int Id { get; set; }

        public DateTime FechaApertura { get; set; }
        public DateTime? FechaCierre { get; set; } // Puede ser nulo si la caja sigue abierta

        public decimal MontoInicial { get; set; }
        public decimal MontoFinal { get; set; } // Lo que contaste al cerrar

        public decimal Diferencia { get; set; } // Si sobró o faltó
        public string Notas { get; set; }

        public bool Abierta { get; set; } // Para saber cuál es el turno actual
    }
}