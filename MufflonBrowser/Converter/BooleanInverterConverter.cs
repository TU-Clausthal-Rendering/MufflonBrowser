using System;
using System.Globalization;
using System.Windows.Data;

namespace MufflonBrowser.Converter
{
    class BooleanInverterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value != null && value.GetType() == typeof(bool))
                return !(bool)value;
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && value.GetType() == typeof(bool))
                return !(bool)value;
            return null;
        }
    }
}
