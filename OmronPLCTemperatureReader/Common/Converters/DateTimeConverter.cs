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
                return ((DateTime)(dateTime)).ToString("d.MM.yyyy H:m:s");
            }

            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value as string;
            DateTime dateTime;

            if (text != null && DateTime.TryParseExact(value.ToString(), "d.MM.yyyy H:m:s", null, DateTimeStyles.None, out dateTime))
            {
                return dateTime;
            }

            return DependencyProperty.UnsetValue;
        }
    }
}
