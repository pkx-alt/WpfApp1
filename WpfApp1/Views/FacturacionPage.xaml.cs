using System.Windows.Controls;
using WpfApp1.ViewModels;

namespace WpfApp1.Views
{
    public partial class FacturacionPage : Page
    {
        public FacturacionPage()
        {
            InitializeComponent();
            // Conectamos el ViewModel
            this.DataContext = new FacturacionViewModel();
        }
    }
}