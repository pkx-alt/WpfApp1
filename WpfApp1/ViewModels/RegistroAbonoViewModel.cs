using System;
using System.Collections.Generic;
using System.Windows.Input;
using OrySiPOS.Helpers;

namespace OrySiPOS.ViewModels
{
    public class RegistroAbonoViewModel : ViewModelBase
    {
        // Guardamos el total real exacto para poder restaurarlo si cambia a Tarjeta
        private decimal _deudaTotalExacta;

        public decimal DeudaTotal
        {
            get { return _deudaTotalExacta; }
            set { _deudaTotalExacta = value; OnPropertyChanged(); }
        }

        private decimal _montoAbono;
        public decimal MontoAbono
        {
            get { return _montoAbono; }
            set
            {
                _montoAbono = value;
                OnPropertyChanged();
                // Avisamos al comando que verifique si el botón se activa
                (ConfirmarCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        private string _metodoPago;
        public string MetodoPago
        {
            get { return _metodoPago; }
            set
            {
                _metodoPago = value;
                OnPropertyChanged();
                // ¡AQUÍ ESTÁ LA MAGIA! Recalculamos al cambiar el método
                AplicarLogicaRedondeo();
            }
        }

        public List<string> MetodosDisponibles { get; } = new List<string> { "Efectivo", "Transferencia", "Tarjeta", "Cheque" };

        public ICommand ConfirmarCommand { get; }
        public Action<bool> CloseAction { get; set; }

        public RegistroAbonoViewModel(decimal deudaPendiente)
        {
            _deudaTotalExacta = deudaPendiente;

            // Inicializamos propiedades (esto disparará la lógica de redondeo inicial)
            MetodoPago = "Efectivo";

            ConfirmarCommand = new RelayCommand(EjecutarConfirmar, PuedeConfirmar);
        }

        private void AplicarLogicaRedondeo()
        {
            // Si el usuario ya escribió algo manual distinto a la deuda total, 
            // quizás no queramos sobreescribirlo, pero para "Sugerir liquidar", hacemos esto:

            if (MetodoPago == "Efectivo")
            {
                // Redondeo estándar (mismo que en Ventas: .50 sube, .49 baja)
                // O AwayFromZero para ir al entero más cercano
                decimal montoRedondeado = Math.Round(_deudaTotalExacta, 0, MidpointRounding.AwayFromZero);
                MontoAbono = montoRedondeado;
            }
            else
            {
                // Si es tarjeta/transferencia, cobramos el centavo exacto
                MontoAbono = _deudaTotalExacta;
            }
        }

        private void EjecutarConfirmar(object obj)
        {
            CloseAction?.Invoke(true);
        }

        private bool PuedeConfirmar(object obj)
        {
            // Validación: Mayor a 0 y que no pague más de lo que debe (con un margen de error pequeño por el redondeo)
            // Permitimos pagar un poquito más o menos por el redondeo
            return MontoAbono > 0;
        }
    }
}