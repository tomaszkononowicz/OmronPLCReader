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
    public class TextValidation : ValidationRule
    {
        public int? MinLength { get; set; }
        public int? MaxLength { get; set; }
        public string ErrorMessage { get; set; }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value.ToString().Equals(""))
                return new ValidationResult(false, "To pole nie może być puste");
            else
            {
                int i = value.ToString().Length;
                if (i < (MinLength ?? i) || i > (MaxLength ?? i))
                    return new ValidationResult(false, ErrorMessage);
                else
                    return ValidationResult.ValidResult;
            }
        }
    }
}
