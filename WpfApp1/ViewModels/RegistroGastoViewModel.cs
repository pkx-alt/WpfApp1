using System;
using System.Collections.Generic;
using System.Windows.Input;
using OrySiPOS.Models;
using OrySiPOS.Helpers; // Usaremos esta clase para los comandos

namespace OrySiPOS.ViewModels
{
    public class RegistroGastoViewModel : ViewModelBase // Asume que tienes una clase ViewModelBase
    {
        // El objeto Gasto que vamos a crear
        public Gasto NuevoGasto { get; set; }

        // Listas para los ComboBox
        public List<string> CategoriasDisponibles { get; set; } = new List<string> { "Proveedores", "Servicios", "Mantenimiento", "Alquiler", "Sueldos", "Otros" };
        public List<string> MetodosDisponibles { get; set; } = new List<string> { "Efectivo", "Transferencia", "Tarjeta", "Cheque" };

        // Comando para guardar
        public ICommand GuardarGastoCommand { get; set; }

        // El delegado para notificar a la ventana principal que cierre el modal
        public Action CerrarVentana { get; set; }

        public RegistroGastoViewModel()
        {
            // Inicializamos el nuevo gasto con valores por defecto
            NuevoGasto = new Gasto
            {
                Fecha = DateTime.Today,
                Usuario = "Admin", // Valor por defecto, se puede cambiar
                Monto = 0.00m
            };

            // Inicializamos el comando de guardar
            GuardarGastoCommand = new RelayCommand(GuardarGastoEjecutar, GuardarGastoPuedeEjecutar);
        }

        // Lógica de validación
        private bool GuardarGastoPuedeEjecutar(object parameter)
        {
            // Simple validación: el Concepto no puede estar vacío y el Monto debe ser mayor a cero
            return !string.IsNullOrWhiteSpace(NuevoGasto.Concepto) && NuevoGasto.Monto > 0;
        }

        // Lógica de guardado
        private void GuardarGastoEjecutar(object parameter)
        {
            // Aquí no guardamos aún, solo notificamos al code-behind de la ventana de que el proceso fue exitoso.
            // La ventana principal (GastosPage) hará el guardado en la DB.
            CerrarVentana?.Invoke();
        }
    }
}