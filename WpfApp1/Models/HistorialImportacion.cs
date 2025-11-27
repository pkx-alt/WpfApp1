using System;
using System.ComponentModel.DataAnnotations;

namespace OrySiPOS.Models
{
    public class HistorialImportacion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UUID { get; set; } // El folio fiscal que no debe repetirse

        public DateTime FechaProcesado { get; set; }
        public string Archivo { get; set; } // Nombre del archivo XML
    }
}