// ViewModels/FormaPagoViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Windows; // Para MessageBox
using System.Windows.Input;
using WpfApp1.Data;
using WpfApp1.Enums;
using WpfApp1.Models;
namespace WpfApp1.ViewModels
{
    // Usamos nuestra clase base para poder notificar cambios
    public class FormaPagoViewModel : ViewModelBase
    {
        // --- 1. Propiedades para el Estado ---
        // --- 1. Propiedades para el Estado ---

        private decimal _totalAPagar;
        public decimal TotalAPagar
        {
            get { return _totalAPagar; }
            set
            {
                _totalAPagar = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Cambio));
                // Dispara la re-evaluación del pago (auto-llenado)
                PagoRecibido = _totalAPagar;
                ActualizarLogicaDePago();
            }
        }

        private decimal _pagoRecibido;
        public decimal PagoRecibido
        {
            get { return _pagoRecibido; }
            set
            {
                _pagoRecibido = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Cambio));
                // ¡Avisa que los códigos SAT también pueden cambiar!
                OnPropertyChanged(nameof(MetodoPagoSAT));
                (FinalizarVentaCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        private bool _isPagoRecibidoEnabled;
        public bool IsPagoRecibidoEnabled
        {
            get { return _isPagoRecibidoEnabled; }
            set
            {
                _isPagoRecibidoEnabled = value;
                OnPropertyChanged();
            }
        }

        private MetodoPago _metodoPagoSeleccionado;
        public MetodoPago MetodoPagoSeleccionado
        {
            get { return _metodoPagoSeleccionado; }
            set
            {
                _metodoPagoSeleccionado = value;
                OnPropertyChanged();
                ActualizarLogicaDePago();
                // Notifica que la forma de pago SAT cambió
                OnPropertyChanged(nameof(FormaPagoSAT));
            }
        }

        // Esta es la lista que se mostrará en el ComboBox
        public ObservableCollection<Cliente> ListaClientes { get; set; }

        private Cliente _clienteSeleccionado;
        public Cliente ClienteSeleccionado
        {
            get { return _clienteSeleccionado; }
            set
            {
                _clienteSeleccionado = value;
                OnPropertyChanged();
                // Notifica que el método SAT puede cambiar si se vuelve a "Público General"
                OnPropertyChanged(nameof(MetodoPagoSAT));
                (FinalizarVentaCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        private Cliente _clientePublicoGeneral;

        public decimal Cambio => PagoRecibido - TotalAPagar;
        public bool ImprimirTicket { get; set; } = true;
        public Action<bool> CloseAction { get; set; }

        // --- PROPIEDADES CFDI CALCULADAS ---

        // FormaPagoSAT (Se calcula según el enum seleccionado)
        public string FormaPagoSAT
        {
            get
            {
                // Mapeo simple a códigos SAT
                switch (MetodoPagoSeleccionado)
                {
                    case MetodoPago.Efectivo: return "01";
                    case MetodoPago.Tarjeta: return "04"; // 04: Tarjeta de Crédito, 28: Débito. Usamos el genérico.
                    case MetodoPago.Transferencia: return "03";
                    case MetodoPago.Mixto: return "99";
                    default: return "99"; // Otros
                }
            }
        }

        // MetodoPagoSAT (PUE o PPD)
        public string MetodoPagoSAT
        {
            get
            {
                // Si falta dinero (Cambio < 0) Y NO es Público General, es PPD (Crédito)
                if (Cambio < 0 && ClienteSeleccionado != null && ClienteSeleccionado.ID != 0)
                    return "PPD"; // Pago en Parcialidades o Diferido (Crédito)

                return "PUE"; // Pago en Una Sola Exhibición (Contado)
            }
        }
        // ------------------------------------


        // --- 2. Comandos ---
        public ICommand FinalizarVentaCommand { get; }
        public ICommand CerrarVentanaCommand { get; }

        // --- 3. Constructor ---
        public FormaPagoViewModel()
        {
            // "Enchufamos" los comandos a sus métodos
            FinalizarVentaCommand = new RelayCommand(EjecutarFinalizarVenta, PuedeFinalizarVenta);
            CerrarVentanaCommand = new RelayCommand(EjecutarCerrarVentana);

            // --- ¡NUEVO CÓDIGO AQUÍ! ---

            // 1. Inicializamos la lista
            ListaClientes = new ObservableCollection<Cliente>();

            // 2. Creamos nuestro cliente "Público en general" en memoria
            //    (Le ponemos un ID 0 o -1 para saber que no es uno real de la BD)
            _clientePublicoGeneral = new Cliente { ID = 0, RazonSocial = "Público en general" };

            // 3. Cargamos los clientes desde la BD
            CargarClientesParaCombo();

            // 4. Establecemos "Público en general" como la selección por defecto
            ClienteSeleccionado = _clientePublicoGeneral;

            // 5. Establecer el estado inicial del método de pago
            MetodoPagoSeleccionado = MetodoPago.Efectivo;
        }

        // --- 4. Lógica de Métodos (Acciones) ---

        // En ViewModels/FormaPagoViewModel.cs

        private void EjecutarFinalizarVenta(object parameter)
        {
            // Si es Público General y falta dinero -> ERROR (Esto ya lo validaba el botón, pero por seguridad)
            if ((ClienteSeleccionado == null || ClienteSeleccionado.ID == 0) && PagoRecibido < TotalAPagar)
            {
                MessageBox.Show("Público en General debe liquidar el total de la venta.", "Pago Insuficiente", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Si es Cliente y falta dinero -> AVISO DE CRÉDITO
            if (MetodoPagoSAT == "PPD") // Usamos la nueva propiedad calculada
            {
                decimal deuda = TotalAPagar - PagoRecibido;
                var resultado = MessageBox.Show(
                    $"El pago es menor al total.\n\n" +
                    $"Se registrará una deuda de {deuda:C} al cliente '{ClienteSeleccionado.RazonSocial}'.\n\n" +
                    "¿Desea continuar?",
                    "Venta a Crédito (PPD)",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (resultado == MessageBoxResult.No)
                {
                    return;
                }
            }

            // ¡Todo bien! Cerramos y devolvemos true
            CloseAction?.Invoke(true);
        }

        // En ViewModels/FormaPagoViewModel.cs

        private bool PuedeFinalizarVenta(object parameter)
        {
            if (PagoRecibido < 0) return false;

            // Si es Público en General, DEBE pagar PUE (completo)
            if (ClienteSeleccionado == null || ClienteSeleccionado.ID == 0)
            {
                return PagoRecibido >= TotalAPagar;
            }

            // Si es un Cliente registrado, siempre puede continuar (puede ser a crédito/PPD)
            return true;
        }

        private void EjecutarCerrarVentana(object parameter)
        {
            // Le decimos a la Vista que cierre y devuelva "false"
            CloseAction?.Invoke(false);
        }

        private void ActualizarLogicaDePago()
        {
            switch (MetodoPagoSeleccionado)
            {
                case MetodoPago.Efectivo:
                    // 1. Habilitamos el TextBox
                    IsPagoRecibidoEnabled = true;
                    break;

                case MetodoPago.Tarjeta:
                case MetodoPago.Transferencia:
                    // 1. Deshabilitamos el TextBox
                    IsPagoRecibidoEnabled = false;
                    // 2. Auto-llenamos el pago con el total
                    PagoRecibido = TotalAPagar;
                    break;

                case MetodoPago.Mixto:
                    // 1. Deshabilitamos el TextBox (por ahora)
                    IsPagoRecibidoEnabled = false;
                    MessageBox.Show("El método de pago 'Mixto' es una función avanzada y no está implementado aún.", "Función no disponible", MessageBoxButton.OK, MessageBoxImage.Information);
                    // 2. (Opcional) Regresarlo a Efectivo
                    // MetodoPagoSeleccionado = MetodoPago.Efectivo; 
                    break;
            }

            // Forzamos la re-evaluación del método SAT (MetodoPagoSAT)
            OnPropertyChanged(nameof(MetodoPagoSAT));
        }
        private void CargarClientesParaCombo()
        {
            // Limpiamos la lista (por si acaso)
            ListaClientes.Clear();

            // 1. Añadimos a "Público en general" PRIMERO
            ListaClientes.Add(_clientePublicoGeneral);

            // 2. Abrimos la conexión a la BD
            using (var db = new InventarioDbContext())
            {
                // 3. Consultamos SÓLO los clientes activos
                //    y los ordenamos por Razón Social (alfabéticamente)
                var clientesDB = db.Clientes
                                   .Where(c => c.Activo) // ¡Solo activos!
                                   .OrderBy(c => c.RazonSocial) // ¡Alfabético!
                                   .ToList();

                // 4. Añadimos los clientes de la BD a nuestra lista
                foreach (var cliente in clientesDB)
                {
                    ListaClientes.Add(cliente);
                }
            }
        }

        // Bandera recibida desde VentaViewModel
        private bool _esModoCotizacion;
        public bool EsModoCotizacion
        {
            get { return _esModoCotizacion; }
            set
            {
                _esModoCotizacion = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TituloVentana));
                OnPropertyChanged(nameof(TextoBotonAccion));
                OnPropertyChanged(nameof(VisibilidadPago));
            }
        }

        // Propiedades dinámicas
        public string TituloVentana => EsModoCotizacion ? "Guardar Cotización" : "Cobrar Venta";
        public string TextoBotonAccion => EsModoCotizacion ? "Guardar" : "Finalizar Venta";
        public Visibility VisibilidadPago => EsModoCotizacion ? Visibility.Collapsed : Visibility.Visible;

        // Validación modificada
        private bool PuedeFinalizar(object param)
        {
            // Si es cotización, siempre true (o validar si requiere cliente)
            if (EsModoCotizacion) return true;

            // Si es venta, validar dinero
            return PagoRecibido >= TotalAPagar; // O >= 0 según tu regla
        }

        private void EjecutarFinalizar(object param)
        {
            // Validar pago solo si NO es cotización
            if (!EsModoCotizacion && PagoRecibido < TotalAPagar)
            {
                MessageBox.Show("Pago insuficiente");
                return;
            }

            CloseAction?.Invoke(true);
        }
    }


}