using System.Windows.Controls;
using WpfApp1.ViewModels;

namespace WpfApp1.Views
{
    public partial class EmpleadosPage : Page
    {
        public EmpleadosPage()
        {
            InitializeComponent();
            // Conectamos el ViewModel
            this.DataContext = new CuentasPorCobrarViewModel();
        }
    }
}