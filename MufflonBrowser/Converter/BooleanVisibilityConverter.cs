using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MufflonBrowser.Converter
{
    class BooleanVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && value.GetType() == typeof(bool))
                return (bool)value ? Visibility.Visible : Visibility.Collapsed;
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && value.GetType() == typeof(Visibility))
                return (Visibility)value == Visibility.Visible;
            return null;
        }
    }
}
