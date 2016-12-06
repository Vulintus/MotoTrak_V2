using System;
using System.Windows.Input;

namespace MotoTrakBoothLauncher
{
    /// <summary>
    /// A simple command class
    /// </summary>
    public class SimpleCommand : ICommand
    {
        private Action _action;
        private bool _canExecute;

        public SimpleCommand(Action action, bool canExecute)
        {
            _action = action;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            _action();
        }
    }
}