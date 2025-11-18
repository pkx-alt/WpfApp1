using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1.Models
{
    public class Venta
    {
        [Key]
        public int VentaId { get; set; } // Clave primaria de la venta

        public DateTime Fecha { get; set; }

        public decimal Subtotal { get; set; }
        public decimal IVA { get; set; }
        public decimal Total { get; set; }


        // --- ¡AÑADE ESTAS 3 LÍNEAS! ---

        // 1. La Clave Foránea (FK). Es "nullable" (int?)
        //    para que si es 'null', sepamos que fue "Público en general".
        public int? ClienteId { get; set; }

        // 2. La "Propiedad de Navegación" (El enlace)
        [ForeignKey("ClienteId")]
        public virtual Cliente Cliente { get; set; }

        // --- FIN DE LÍNEAS NUEVAS ---
        // --- ¡AÑADE ESTAS DOS LÍNEAS! ---
        public decimal PagoRecibido { get; set; }
        public decimal Cambio { get; set; }
        // --- FIN DE LÍNEAS NUEVAS ---

        // Propiedad de navegación: Una Venta tiene MUCHOS Detalles
        public virtual ICollection<VentaDetalle> Detalles { get; set; }

        public Venta()
        {
            Detalles = new List<VentaDetalle>();
        }
    }
}
