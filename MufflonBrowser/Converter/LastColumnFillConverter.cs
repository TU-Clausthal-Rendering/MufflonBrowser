using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace MufflonBrowser.Converter
{
    public class LastColumnFillConverter : IValueConverter
    {
        // Idea taken from here: https://social.msdn.microsoft.com/Forums/en-US/3ee5696c-4f26-4e30-8891-0e2f95d69623/gridview-last-column-to-fill-available-space?forum=wpf
        public object Convert(object o, Type targetType, object parameter, CultureInfo culture)
        {
            // Compute the sum of all columns minus the last one
            // The actual width minus that is then how large the last column should be
            var list = o as ListView;
            var grid = list.View as GridView;
            double totalWidth = 0.0;
            for (int i = 0; i + 1 < grid.Columns.Count; ++i)
                totalWidth += grid.Columns[i].Width;
            return (list.ActualWidth - totalWidth);
        }

        public object ConvertBack(object o, Type type, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
