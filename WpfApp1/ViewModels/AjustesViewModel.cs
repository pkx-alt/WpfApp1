using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using OrySiPOS.Helpers; // Para el RelayCommand
using OrySiPOS.Properties; // ¡Importante! Para acceder a Settings
using System.Diagnostics;
using System.Printing; // <-- ¡Nuevo! Para hablar con el servicio de impresión de Windows

namespace OrySiPOS.ViewModels
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
        public ICommand AbrirPropiedadesImpresoraCommand { get; }

        // --- Constructor ---
        // En ViewModels/AjustesViewModel.cs

        public AjustesViewModel()
        {
            // 1. Inicializamos la lista vacía
            ListaImpresoras = new ObservableCollection<string>();

            try
            {
                // Creamos un servidor de impresión local (tu PC)
                var server = new LocalPrintServer();

                // Obtenemos todas las colas de impresión (impresoras instaladas)
                var colas = server.GetPrintQueues();

                foreach (var cola in colas)
                {
                    // Agregamos el nombre real de cada impresora a nuestra lista
                    ListaImpresoras.Add(cola.Name); // o cola.FullName
                }
            }
            catch (Exception ex)
            {
                // Si algo falla (ej. servicio de impresión detenido), cargamos una lista de emergencia
                // para que el programa no truene.
                ListaImpresoras.Add("Microsoft Print to PDF");
                System.Diagnostics.Debug.WriteLine("Error al listar impresoras: " + ex.Message);
            }

            // Si por alguna razón no encontró nada, avisamos
            if (ListaImpresoras.Count == 0)
            {
                ListaImpresoras.Add("(No se encontraron impresoras)");
            }

            // 2. Cargamos los datos guardados (tu configuración previa)
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

        private void AbrirPropiedades(object obj)
        {
            if (string.IsNullOrEmpty(ImpresoraSeleccionada))
            {
                MessageBox.Show("Primero selecciona una impresora de la lista.");
                return;
            }

            try
            {
                // Este comando mágico de Windows abre las propiedades de una impresora específica
                // printui.dll es la herramienta nativa de Windows para esto.
                string argumentos = $"/p /n \"{ImpresoraSeleccionada}\"";

                Process.Start(new ProcessStartInfo
                {
                    FileName = "rundll32",
                    Arguments = $"printui.dll,PrintUIEntry {argumentos}",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo abrir la configuración de Windows: " + ex.Message);
            }
        }
    }
}