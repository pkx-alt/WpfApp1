using System;
using System.Collections.Generic;
using System.Windows.Input;
using OrySiPOS.Models;  // Asegúrate de tener tu modelo Ingreso aquí
using OrySiPOS.Helpers; // O donde tengas tu RelayCommand

namespace OrySiPOS.ViewModels
{
    public class RegistroIngresoViewModel : ViewModelBase
    {
        // Objeto específico para Ingresos
        public Ingreso NuevoIngreso { get; set; }

        // Categorías típicas de entradas de dinero NO por ventas
        public List<string> CategoriasDisponibles { get; set; } = new List<string>
        {
            "Aporte de Capital",
            "Fondo de Caja (Cambio)",
            "Devolución de Préstamo",
            "Ingreso Varios",
            "Ajuste de Inventario"
        };

        public List<string> MetodosDisponibles { get; set; } = new List<string> { "Efectivo", "Transferencia", "Cheque", "Depósito" };

        // Comandos y Acciones
        public ICommand GuardarIngresoCommand { get; set; }
        public Action CerrarVentana { get; set; }

        public RegistroIngresoViewModel()
        {
            // Inicializamos
            NuevoIngreso = new Ingreso
            {
                Fecha = DateTime.Today,
                Usuario = "Admin",
                Monto = 0.00m
            };

            GuardarIngresoCommand = new RelayCommand(GuardarIngresoEjecutar, GuardarIngresoPuedeEjecutar);
        }

        // Validación: Concepto obligatorio y monto positivo
        private bool GuardarIngresoPuedeEjecutar(object parameter)
        {
            return !string.IsNullOrWhiteSpace(NuevoIngreso.Concepto) && NuevoIngreso.Monto > 0;
        }

        private void GuardarIngresoEjecutar(object parameter)
        {
            // Avisamos a la vista que cierre con éxito
            CerrarVentana?.Invoke();
        }
    }
}