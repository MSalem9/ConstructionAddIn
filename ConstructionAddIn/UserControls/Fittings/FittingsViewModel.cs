using ArcGIS.Desktop.Framework.Contracts;
using ConstructionAddIn.Services;
using System;
using System.Windows.Input;

namespace ConstructionAddIn.UserControls.Fittings
{
    internal class FittingsViewModel : PropertyChangedBase
    {
        public ICommand FlipSelectedCommand { get; }

        public FittingsViewModel()
        {
            FlipSelectedCommand = new RelayCommand(FlipSelected);
        }

        private async void FlipSelected()
        {
            await PointFeatureFlipService.StartAsync();
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) =>
            _canExecute == null || _canExecute();

        public void Execute(object parameter) => _execute();

        public event EventHandler CanExecuteChanged;
    }
}