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
    public class IPAddressConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IPAddress ipAddress)
            {
                return ipAddress.ToString();
            }

            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text && IPAddress.TryParse(text, out IPAddress ipAddress))
            {
                return ipAddress;
            }

            return DependencyProperty.UnsetValue;
        }
    }
}
