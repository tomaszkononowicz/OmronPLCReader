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
        public string Pattern { get; set; }
        public string PatternErrorMessage { get; set; }
        public bool CanBeEmpty { get; set; } = false;
        public bool IsEnabled { get; set; } = true;

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (IsEnabled)
            {
                int i = 0;
                if (value != null)
                    i = value.ToString().Length;
                if (!(CanBeEmpty) && i <= 0)
                    return new ValidationResult(false, "To pole nie może być puste");
                else if (i < (MinLength ?? i))
                    return new ValidationResult(false, "Wprowadzony tekst jest zbyt krótki, minimalna liczba znaków: " + MinLength);
                else if (i > (MaxLength ?? i))
                    return new ValidationResult(false, "Wprowadzony tekst jest zbyt długi, maksymalna liczba znaków: " + MaxLength);
                else if (Pattern != null && !Regex.IsMatch(value.ToString(), Pattern))
                    return new ValidationResult(false, "Wprowadzony tekst nie pasuje do wzorca: " + PatternErrorMessage);
                else
                    return ValidationResult.ValidResult;
            }
            else return ValidationResult.ValidResult;
        }
    }
}

