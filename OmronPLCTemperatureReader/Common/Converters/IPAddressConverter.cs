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
            var ipAddress = value as IPAddress;

            if (ipAddress != null)
            {
                return ipAddress.ToString();
            }

            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value as string;
            IPAddress ipAddress;

            if (text != null && IPAddress.TryParse(text, out ipAddress))
            {
                return ipAddress;
            }

            return DependencyProperty.UnsetValue;
        }
    }
}
