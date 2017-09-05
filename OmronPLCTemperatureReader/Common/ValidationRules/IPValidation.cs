using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace OmronPLCTemperatureReader.Common.ValidationRules
{
    public class IPValidation : ValidationRule
    {
        public string ErrorMessage { get; set; }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value.ToString().Equals(""))
                return new ValidationResult(false, "To pole nie może być puste");
            else
            if (!Regex.IsMatch(value.ToString(), @"^0*(25[0-5]|2[0-4]\d|1?\d\d?)(\.0*(25[05]|2[0-4]\d|1?\d\d?)){3}$"))
                return new ValidationResult(false, ErrorMessage);
                //"Niepoprawny format adresu IP: 4 liczby z zakresu 0-255 oddzielone kropkami np. 192.168.0.1";
            else
                return ValidationResult.ValidResult;           
        }
    }
}
