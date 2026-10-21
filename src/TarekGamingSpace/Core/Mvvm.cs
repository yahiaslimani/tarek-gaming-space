using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace TarekGamingSpace.Core
{
    public abstract class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void Raise([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        protected bool Set<T>(ref T field, T value, [CallerMemberName] string name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            Raise(name);
            return true;
        }
    }

    public sealed class RelayCommand : ICommand
    {
        readonly Action<object> _run;
        readonly Func<object, bool> _can;

        public RelayCommand(Action run, Func<bool> can = null)
            : this(_ => run(), can == null ? (Func<object, bool>)null : _ => can()) { }

        public RelayCommand(Action<object> run, Func<object, bool> can = null)
        {
            _run = run;
            _can = can;
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter) => _can == null || _can(parameter);
        public void Execute(object parameter) => _run(parameter);
    }

    /// <summary>Two-way bridge between a string setting and a radio button: IsChecked = (value == parameter).</summary>
    public sealed class EqualsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.Ordinal);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is bool b && b ? parameter : Binding.DoNothing;
    }

    public sealed class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is bool b && b ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
