using System.ComponentModel.DataAnnotations;

namespace OrySiPOS.Models
{
    public class SatUnidad
    {
        [Key]
        [MaxLength(3)]
        public string Clave { get; set; }

        public string Descripcion { get; set; }

        public string Display => $"{Clave} - {Descripcion}";
    }
}