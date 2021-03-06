﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace OmronPLCTemperatureReader.Common.ValidationRules
{
    public class IntegerValidation : ValidationRule
    {
        public int? MinValue { get; set; }
        public int? MaxValue { get; set; }
        public string ErrorMessage { get; set; }
        public bool CanBeEmpty { get; set; } = false;
        public bool IsEnabled { get; set; } = true;


        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (IsEnabled)
            {
                if (!(CanBeEmpty) && value.ToString().Equals(""))
                    return new ValidationResult(false, "To pole nie może być puste");
                else if ((CanBeEmpty) && value.ToString().Equals(""))
                    return ValidationResult.ValidResult;
                else if (!int.TryParse(value.ToString(), out int i))
                    return new ValidationResult(false, "Podana wartość nie jest liczbą!");
                else if (i < (MinValue ?? i) || i > (MaxValue ?? i))
                    return new ValidationResult(false, ErrorMessage);
                else
                    return ValidationResult.ValidResult;
            }
            else return ValidationResult.ValidResult;
        }
    }
}
