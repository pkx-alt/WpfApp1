using System;
using System.Collections.Generic;
using System.Windows.Input;
using WpfApp1.Helpers; // Para RelayCommand

namespace WpfApp1.ViewModels
{
    public class RegistroAbonoViewModel : ViewModelBase
    {
        // Datos que recibimos
        public decimal DeudaTotal { get; set; }

        // Datos que el usuario llena
        public decimal MontoAbono { get; set; }
        public string MetodoPago { get; set; } = "Efectivo"; // Por defecto

        // Listas para la vista
        public List<string> MetodosDisponibles { get; } = new List<string> { "Efectivo", "Transferencia", "Tarjeta", "Cheque" };

        // Comandos
        public ICommand ConfirmarCommand { get; }
        public Action<bool> CloseAction { get; set; } // Para cerrar la ventana

        public RegistroAbonoViewModel(decimal deudaPendiente)
        {
            DeudaTotal = deudaPendiente;
            MontoAbono = deudaPendiente; // Sugerimos pagar todo de una vez

            ConfirmarCommand = new RelayCommand(EjecutarConfirmar, PuedeConfirmar);
        }

        private void EjecutarConfirmar(object obj)
        {
            // Simplemente cerramos con éxito
            CloseAction?.Invoke(true);
        }

        private bool PuedeConfirmar(object obj)
        {
            // Validamos: Debe ser mayor a 0 Y no puede pagar más de lo que debe
            return MontoAbono > 0 && MontoAbono <= DeudaTotal;
        }
    }
}