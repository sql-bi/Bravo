using Sqlbi.Bravo.Core.Helpers;
using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.UI.Framework.ViewModels
{
    internal class BaseViewModel : IBaseViewModel, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        protected async Task ExecuteCommandAsync(Expression<Func<bool>> updating, Func<Task> action)
        {
            if (updating.GetPropertyValue())
                return;

            updating.SetPropertyValue(true);
            try
            {
                await action?.Invoke();
            }
            finally
            {
                updating.SetPropertyValue(false);
            }
        }
    }
}
