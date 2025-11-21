using System.Windows.Controls;
using WpfApp1.ViewModels;

namespace WpfApp1.Views
{
    public partial class CuentasCobrarPage : Page // <--- CAMBIADO DE EmpleadosPage
    {
        public CuentasCobrarPage() // <--- CAMBIADO EL CONSTRUCTOR
        {
            InitializeComponent();
            this.DataContext = new WpfApp1.ViewModels.CuentasPorCobrarViewModel();
        }
    }
}