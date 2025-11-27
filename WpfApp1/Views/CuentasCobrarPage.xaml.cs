using System.Windows.Controls;
using OrySiPOS.ViewModels;

namespace OrySiPOS.Views
{
    public partial class CuentasCobrarPage : Page // <--- CAMBIADO DE EmpleadosPage
    {
        public CuentasCobrarPage() // <--- CAMBIADO EL CONSTRUCTOR
        {
            InitializeComponent();
            this.DataContext = new OrySiPOS.ViewModels.CuentasPorCobrarViewModel();
        }
    }
}