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



        private decimal _totalAPagar;
        public decimal TotalAPagar
        {
            get { return _totalAPagar; }
            set
            {
                _totalAPagar = value;
                OnPropertyChanged();

                // ¡REACCIÓN! Si el total cambia, recalculamos el cambio
                OnPropertyChanged(nameof(Cambio));

                // Ponemos el total como sugerencia de pago
                PagoRecibido = _totalAPagar;
                ActualizarLogicaDePago(); // Para que el pago se auto-llene si es Tarjeta/Transfer
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

                // ¡REACCIÓN! Si el pago cambia, recalculamos el cambio
                OnPropertyChanged(nameof(Cambio));

                // También le avisamos al botón de Finalizar que re-evalúe
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

                // ¡¡AQUÍ ESTÁ LA LÓGICA!!
                // Cada vez que el método de pago cambia,
                // ejecutamos nuestra lógica de actualización.
                ActualizarLogicaDePago();
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
            }
        }

        // Un campo privado para guardar nuestro cliente "dummy"
        private Cliente _clientePublicoGeneral;

        // ¡Propiedad Calculada! Esta es la nueva forma de hacerlo.
        // No guarda un valor, solo lo calcula cuando se lo piden.
        public decimal Cambio => PagoRecibido - TotalAPagar;

        public bool ImprimirTicket { get; set; } = true; // Valor por defecto

        // Esta propiedad "especial" la usaremos para cerrar la ventana
        // Es un "truco" para que el VM le ordene al View que se cierre.
        public Action<bool> CloseAction { get; set; }


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
            if (ClienteSeleccionado != null && ClienteSeleccionado.ID != 0 && PagoRecibido < TotalAPagar)
            {
                decimal deuda = TotalAPagar - PagoRecibido;
                var resultado = MessageBox.Show(
                    $"El pago es menor al total.\n\n" +
                    $"Se registrará una deuda de {deuda:C} al cliente '{ClienteSeleccionado.RazonSocial}'.\n\n" +
                    "¿Desea continuar?",
                    "Venta a Crédito",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (resultado == MessageBoxResult.No)
                {
                    return; // Cancelar si el usuario se arrepintió
                }
            }

            // ¡Todo bien! Cerramos
            CloseAction?.Invoke(true);
        }

        // En ViewModels/FormaPagoViewModel.cs

        private bool PuedeFinalizarVenta(object parameter)
        {
            // Regla 1: El pago no puede ser negativo (obvio)
            if (PagoRecibido < 0) return false;

            // Regla 2: Si es "Público en General" (ID = 0), DEBE pagar completo.
            // (No le fiamos a desconocidos)
            if (ClienteSeleccionado == null || ClienteSeleccionado.ID == 0)
            {
                return PagoRecibido >= TotalAPagar;
            }

            // Regla 3: Si es un Cliente registrado, ¡puede pagar lo que sea! (Incluso 0)
            // El sistema registrará la deuda automáticamente.
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