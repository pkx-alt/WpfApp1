using System.Windows;
using WpfApp1.ViewModels;

namespace WpfApp1.Views.Dialogs
{
    public partial class DetalleCotizacionDialog : Window
    {
        // Propiedad pública para saber si el usuario quiso convertir
        public bool DeseaConvertir { get; private set; } = false;

        public DetalleCotizacionDialog(int cotizacionId)
        {
            InitializeComponent();

            var vm = new DetalleCotizacionViewModel(cotizacionId);

            // Configurar cierre normal
            vm.CloseAction = (bool result) => {
                this.DialogResult = result;
                this.Close();
            };

            // Configurar acción de convertir
            vm.ConvertirAction = () => {
                // Marcamos la bandera y cerramos con True (Éxito)
                this.DeseaConvertir = true;
                this.DialogResult = true;
                this.Close();
            };

            this.DataContext = vm;
        }
    }
}