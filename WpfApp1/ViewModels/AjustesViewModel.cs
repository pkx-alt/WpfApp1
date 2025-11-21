using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using WpfApp1.Helpers; // Para el RelayCommand
using WpfApp1.Properties; // ¡Importante! Para acceder a Settings

namespace WpfApp1.ViewModels
{
    public class AjustesViewModel : ViewModelBase
    {
        // --- Propiedades que se ven en la pantalla ---
        // Usamos propiedades completas para que, si cambian, la pantalla se entere.

        private string _nombreTienda;
        public string NombreTienda
        {
            get => _nombreTienda;
            set { _nombreTienda = value; OnPropertyChanged(); }
        }

        private string _direccionTienda;
        public string DireccionTienda
        {
            get => _direccionTienda;
            set { _direccionTienda = value; OnPropertyChanged(); }
        }

        private string _telefonoTienda;
        public string TelefonoTienda
        {
            get => _telefonoTienda;
            set { _telefonoTienda = value; OnPropertyChanged(); }
        }

        private string _rfcTienda;
        public string RFCTienda
        {
            get => _rfcTienda;
            set { _rfcTienda = value; OnPropertyChanged(); }
        }

        private string _mensajeTicket;
        public string MensajeTicket
        {
            get => _mensajeTicket;
            set { _mensajeTicket = value; OnPropertyChanged(); }
        }

        // Para los ComboBox
        public ObservableCollection<string> ListaImpresoras { get; set; }

        private string _impresoraSeleccionada;
        public string ImpresoraSeleccionada
        {
            get => _impresoraSeleccionada;
            set { _impresoraSeleccionada = value; OnPropertyChanged(); }
        }

        private bool _imprimirTicketDefault;
        public bool ImprimirTicketDefault
        {
            get => _imprimirTicketDefault;
            set { _imprimirTicketDefault = value; OnPropertyChanged(); }
        }

        // --- Comandos ---
        public ICommand GuardarCommand { get; }

        // --- Constructor ---
        public AjustesViewModel()
        {
            // 1. Llenamos la lista de impresoras (Simulada por ahora)
            // Un reto para después: Usar System.Drawing.Printing para listar las reales de Windows.
            ListaImpresoras = new ObservableCollection<string>
            {
                "Microsoft Print to PDF",
                "EPSON TM-T20II",
                "POS-58",
                "Generic Text Only"
            };

            // 2. Cargamos los datos guardados
            CargarConfiguracion();

            // 3. Configurar el botón
            GuardarCommand = new RelayCommand(GuardarCambios);
        }

        private void CargarConfiguracion()
        {
            // Leemos de la "memoria" (Settings) y lo ponemos en la RAM (Propiedades)
            NombreTienda = Settings.Default.NombreTienda;
            DireccionTienda = Settings.Default.DireccionTienda;
            TelefonoTienda = Settings.Default.TelefonoTienda;
            RFCTienda = Settings.Default.RFCTienda;
            MensajeTicket = Settings.Default.MensajeTicket;
            ImpresoraSeleccionada = Settings.Default.Impresora;
            ImprimirTicketDefault = Settings.Default.ImprimirTicketDefault;
        }

        private void GuardarCambios(object obj)
        {
            try
            {
                // Pasamos los datos de la RAM (Propiedades) a la "memoria" permanente (Settings)
                Settings.Default.NombreTienda = NombreTienda;
                Settings.Default.DireccionTienda = DireccionTienda;
                Settings.Default.TelefonoTienda = TelefonoTienda;
                Settings.Default.RFCTienda = RFCTienda;
                Settings.Default.MensajeTicket = MensajeTicket;
                Settings.Default.Impresora = ImpresoraSeleccionada;
                Settings.Default.ImprimirTicketDefault = ImprimirTicketDefault;

                // ¡EL PASO MÁS IMPORTANTE! Si no haces Save(), se pierde al cerrar la app.
                Settings.Default.Save();

                MessageBox.Show("¡Configuración guardada correctamente!", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Error al guardar: " + ex.Message);
            }
        }
    }
}