using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Waveform.Common
{
    public abstract class BindableBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetProperty<T>(ref T storage, T value, Action<T, T> action = null, [CallerMemberName] String propertyName = null)
        {
            if (Equals(storage, value))
            {
                return false;
            }

            var oldValue = storage;
            storage = value;
            OnPropertyChanged(propertyName);
            action?.Invoke(oldValue, value);

            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
