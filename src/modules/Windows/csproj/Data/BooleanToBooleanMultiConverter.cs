﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Fuxion.Windows.Data
{
    public sealed class BooleanToBooleanMultiConverter : IMultiValueConverter
    {
        public MultiBooleanConverterMode Mode { get; set; } = MultiBooleanConverterMode.AllTrue;
        public bool TrueValue { get; set; } = true;
        public bool FalseValue { get; set; } = false;
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (!values.All(v => v is bool)) throw new NotSupportedException($"Not all values are booleans");
            var vals = values.Cast<bool>();
            switch (Mode)
            {
                case MultiBooleanConverterMode.AllTrue:
                    return vals.Any(v => !v) ? FalseValue : TrueValue;
                case MultiBooleanConverterMode.AnyTrue:
                    return vals.Any(v => v) ? TrueValue : FalseValue;
                case MultiBooleanConverterMode.AllFalse:
                    return vals.Any(v => v) ? FalseValue : TrueValue;
                case MultiBooleanConverterMode.AnyFalse:
                    return vals.Any(v => !v) ? TrueValue : FalseValue;
                default:
                    throw new NotSupportedException($"The value of Mode '{Mode}' is not supported");
            }
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException($"Method '{nameof(ConvertBack)}' is not implemented");
        }
    }
}
