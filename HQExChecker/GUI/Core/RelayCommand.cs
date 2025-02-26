using System.Windows.Input;

namespace HQExChecker.GUI.Core
{
    public class RelayCommand(Action<object?> execute, Predicate<object?> canExecute) : ICommand
    {
        private readonly Action<object?> _execute = execute;
        private readonly Predicate<object?> _canExecute = canExecute;

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute(parameter);
        }

        public void Execute(object? parameter)
        {
            _execute(parameter);
        }
    }
}
