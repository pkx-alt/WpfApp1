// DepartamentosPage.xaml.cs
using System.Windows.Controls;
using OrySiPOS.ViewModels; // ¡Importante! Traer nuestro ViewModel

namespace OrySiPOS.Views
{
    public partial class DepartamentosPage : Page
    {
        public DepartamentosPage()
        {
            InitializeComponent();

            // --- ¡LA CONEXIÓN! ---
            // Le decimos a la Vista (la Página) que su "Contexto de Datos"
            // (su "cerebro") es una nueva instancia de nuestro ViewModel.
            this.DataContext = new DepartamentosViewModel();
        }
    }
}