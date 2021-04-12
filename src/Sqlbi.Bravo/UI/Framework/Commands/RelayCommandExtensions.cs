using System.ComponentModel;

namespace Sqlbi.Bravo.UI.Framework.Commands
{
    internal static class RelayCommandExtensions
    {
        public static RelayCommand ObserveProperty(this RelayCommand command, INotifyPropertyChanged source, string propertyName)
        {
            source.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName.Equals(propertyName))
                    command.RaiseCanExecuteChanged();
            };

            return command;
        }

        public static RelayCommand ObserveProperties(this RelayCommand command, INotifyPropertyChanged source, params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
                command = command.ObserveProperty(source, propertyName);

            return command;
        }
    }
}
