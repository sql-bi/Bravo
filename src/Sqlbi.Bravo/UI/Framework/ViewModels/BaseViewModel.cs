using Sqlbi.Bravo.Core.Helpers;
using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.UI.Framework.ViewModels
{
    internal class BaseViewModel : IBaseViewModel, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

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
