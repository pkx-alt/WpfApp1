using System.Windows.Controls;

namespace WpfApp1.Views
{
    public partial class FacturacionPage : Page
    {
        public FacturacionPage()
        {
            InitializeComponent();
        }
    }

    // --- CLASES SIMPLES SOLO PARA QUE SE VEA EL DISEÑO (BORRAR LUEGO O MOVER A MODELS) ---
    public class FacturaEjemplo
    {
        public string Folio { get; set; }
        public string Fecha { get; set; }
        public string Cliente { get; set; }
        public string RFC { get; set; }
        public double Total { get; set; }
    }

    public class FacturaHistorialEjemplo
    {
        public string UUID { get; set; }
        public string SerieFolio { get; set; }
        public string Cliente { get; set; }
        public string Total { get; set; }
    }
}