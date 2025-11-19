using System.Windows.Controls;
using WpfApp1.ViewModels;

namespace WpfApp1.Views
{
    public partial class MovimientosPage : Page
    {
        public MovimientosPage()
        {
            InitializeComponent();
            // Conectamos con el Cerebro
            this.DataContext = new MovimientosViewModel();
        }

        // Constructor opcional: Para cuando quieras ver el historial de UN SOLO producto
        public MovimientosPage(int productoId)
        {
            InitializeComponent();
            var vm = new MovimientosViewModel();
            vm.CargarMovimientos(productoId); // ¡Cargamos filtrado!
            this.DataContext = vm;
        }
    }
}