// ViewModels/RelayCommand.cs
using System;
using System.Windows.Input;

namespace WpfApp1.ViewModels
{
    // Esta es nuestra clase "traductora"
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        // Constructor para comandos que siempre se pueden ejecutar
        public RelayCommand(Action<object> execute) : this(execute, null)
        {
        }

        // Constructor principal
        public RelayCommand(Action<object> execute, Predicate<object> canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        // Esta función le dice al botón si está HABILITADO o NO
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        // Esta es la ACCIÓN que se ejecuta cuando le dan clic
        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        // Un método útil para forzar la re-evaluación de CanExecute
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}