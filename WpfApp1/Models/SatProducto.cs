using System.ComponentModel.DataAnnotations;

namespace OrySiPOS.Models
{
    public class SatProducto
    {
        [Key]
        [MaxLength(8)]
        public string Clave { get; set; }

        public string Descripcion { get; set; }

        public string Display => $"{Clave} - {Descripcion}";
    }
}