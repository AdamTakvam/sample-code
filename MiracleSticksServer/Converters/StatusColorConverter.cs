using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;

namespace MiracleSticksServer.Converters
{
    [ValueConversion(typeof(bool), typeof(SolidColorBrush))]
    public class StatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isError = (bool) value;
            return isError ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Green);
        }
 
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
