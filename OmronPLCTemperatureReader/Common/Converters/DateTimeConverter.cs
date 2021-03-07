using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace OmronPLCTemperatureReader.Common.Converters
{
    public class DateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var dateTime = value as DateTime?;

            if (dateTime != null)
            {
                return ((DateTime)(dateTime)).ToString("d.MM.yyyy H:mm:ss");
            }

            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && DateTime.TryParseExact(value.ToString(), "d.MM.yyyy H:mm:ss", null, DateTimeStyles.None, out DateTime dateTime))
            {
                return dateTime;
            }

            return DependencyProperty.UnsetValue;
        }
    }
}
