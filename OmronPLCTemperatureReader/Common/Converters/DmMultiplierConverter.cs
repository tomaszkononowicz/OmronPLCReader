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
    public class DmMultiplierConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var intValue = values[0] as int?;
            var multiplier = values[1] as double?;

            if (intValue != null)
            {
                if (multiplier != null)
                {
                    return (intValue * multiplier).ToString();
                }
                return intValue.ToString();
            }

            return DependencyProperty.UnsetValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text && double.TryParse(text, out double doubleValue))
            {
                var multiplier = parameter as double?;
                if (multiplier != null)
                {
                    return doubleValue / multiplier;
                }
            }

            return DependencyProperty.UnsetValue;
        }
    }
}
