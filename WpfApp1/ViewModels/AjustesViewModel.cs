using OrySiPOS.Helpers; // Para el RelayCommand
using OrySiPOS.Properties; // ¡Importante! Para acceder a Settings
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Printing; // <-- ¡Nuevo! Para hablar con el servicio de impresión de Windows
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32; // Para SaveFileDialog
using System.IO;       // Para File.Copy

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

        // --- NUEVAS PROPIEDADES ---

        private int _nivelBajoStock;
        public int NivelBajoStock
        {
            get => _nivelBajoStock;
            set { _nivelBajoStock = value; OnPropertyChanged(); }
        }

        private decimal _porcentajeIVA;
        public decimal PorcentajeIVA
        {
            get => _porcentajeIVA;
            set { _porcentajeIVA = value; OnPropertyChanged(); }
        }

        private string _emailInventario;
        public string EmailInventario
        {
            get => _emailInventario;
            set { _emailInventario = value; OnPropertyChanged(); }
        }

        private string _passEmailInventario;
        public string PassEmailInventario
        {
            get => _passEmailInventario;
            set { _passEmailInventario = value; OnPropertyChanged(); }
        }
        private string _emailKeyword;
        public string EmailKeyword
        {
            get => _emailKeyword;
            set { _emailKeyword = value; OnPropertyChanged(); }
        }
        // --- Comandos ---
        public ICommand GuardarCommand { get; }
        public ICommand AbrirPropiedadesImpresoraCommand { get; }
        // 1. Declarar el comando
        public ICommand ExportarBaseDatosCommand { get; }
        // 1. Declarar el nuevo comando
        public ICommand ImportarBaseDatosCommand { get; }

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
            ExportarBaseDatosCommand = new RelayCommand(EjecutarExportacion);
            ImportarBaseDatosCommand = new RelayCommand(EjecutarImportacion);
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
            // Nuevas cargas
            NivelBajoStock = Settings.Default.NivelBajoStock;
            PorcentajeIVA = Settings.Default.PorcentajeIVA;
            EmailInventario = Settings.Default.EmailInventario;
            PassEmailInventario = Settings.Default.PassEmailInventario;
            EmailKeyword = Settings.Default.EmailKeyword;
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
                // Nuevos guardados
                Settings.Default.NivelBajoStock = NivelBajoStock;
                Settings.Default.PorcentajeIVA = PorcentajeIVA;
                Settings.Default.EmailInventario = EmailInventario;
                Settings.Default.PassEmailInventario = PassEmailInventario;
                Settings.Default.EmailKeyword = EmailKeyword;

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

        // 3. La lógica del respaldo
        private void EjecutarExportacion(object parameter)
        {
            try
            {
                // Nombre de tu base de datos (según tu DbContext)
                string nombreArchivoDb = "BaseDeDatos.db";

                // Buscamos la ruta real donde está corriendo la app
                string rutaOrigen = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, nombreArchivoDb);

                if (!File.Exists(rutaOrigen))
                {
                    MessageBox.Show("No se encuentra el archivo de base de datos original.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Configurar el diálogo para guardar
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Base de Datos SQLite (*.db)|*.db";
                saveFileDialog.FileName = $"Respaldo_OrySiPOS_{DateTime.Now:yyyy_MM_dd}.db";
                saveFileDialog.Title = "Guardar copia de seguridad";

                // Mostrar diálogo
                if (saveFileDialog.ShowDialog() == true)
                {
                    string rutaDestino = saveFileDialog.FileName;

                    // ¡COPIAR EL ARCHIVO!
                    File.Copy(rutaOrigen, rutaDestino, true);

                    MessageBox.Show($"Respaldo creado con éxito en:\n{rutaDestino}", "Copia de Seguridad", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al exportar: {ex.Message}\n\nAsegúrate de que nadie más esté usando el archivo.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        // 3. La Lógica de Restauración
        private void EjecutarImportacion(object parameter)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Base de Datos SQLite (*.db)|*.db",
                Title = "Seleccionar archivo de respaldo"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string rutaRespaldo = openFileDialog.FileName;
                string rutaDestino = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "inventario.db");

                // PREGUNTA DE SEGURIDAD
                var confirmacion = MessageBox.Show(
                    "⚠️ ¡ATENCIÓN! ⚠️\n\n" +
                    "Al restaurar este respaldo, SE BORRARÁN todos los datos actuales y serán reemplazados por los del archivo seleccionado.\n\n" +
                    "Esta acción no se puede deshacer.\n\n" +
                    "¿Estás 100% seguro de continuar?",
                    "Confirmar Restauración",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirmacion == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Intentamos forzar la liberación de archivos (truco para SQLite)
                        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                        GC.Collect();
                        GC.WaitForPendingFinalizers();

                        // Reemplazamos el archivo
                        File.Copy(rutaRespaldo, rutaDestino, true);

                        MessageBox.Show(
                            "La base de datos se restauró correctamente.\n\n" +
                            "La aplicación se cerrará para aplicar los cambios. Por favor, ábrela de nuevo.",
                            "Restauración Exitosa",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        // REINICIAR LA APLICACIÓN
                        // Opción A: Cerrar y que el usuario abra
                        Application.Current.Shutdown();

                        // Opción B: Intentar reiniciar automáticamente (opcional)
                        // Process.Start(Environment.ProcessPath);
                        // Application.Current.Shutdown();
                    }
                    catch (IOException)
                    {
                        MessageBox.Show(
                            "No se pudo reemplazar el archivo porque está en uso.\n\n" +
                            "Intenta cerrar y abrir la aplicación nuevamente, y realiza la restauración inmediatamente antes de hacer cualquier venta.",
                            "Archivo Bloqueado",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error al restaurar: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}