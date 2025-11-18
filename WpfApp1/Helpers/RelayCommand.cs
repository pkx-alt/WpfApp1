// Helpers/RelayCommand.cs
using System;
using System.Windows.Input;

namespace WpfApp1.Helpers
{
    // Esta es nuestra clase "cable" reutilizable
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // ¿Puede ejecutarse el comando?
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        // El código que se ejecuta cuando se "jala la palanca"
        public void Execute(object parameter)
        {
            _execute(parameter);
        }
    }
}