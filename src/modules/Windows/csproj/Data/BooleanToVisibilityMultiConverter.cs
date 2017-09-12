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
    public enum MultiBooleanConverterMode
    {
        AllTrue, AnyTrue, AllFalse, AnyFalse
    }
    public sealed class BooleanToVisibilityMultiConverter : IMultiValueConverter
    {
        public MultiBooleanConverterMode Mode { get; set; } = MultiBooleanConverterMode.AllTrue;
        public Visibility TrueValue { get; set; } = Visibility.Visible;
        public Visibility FalseValue { get; set; } = Visibility.Collapsed;
        public bool AllowNullValues { get; set; }
        public bool NullValue { get; set; }
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (!values.All(v => v is bool) && !AllowNullValues) throw new NotSupportedException($"Not all values are booleans");
            var vals = values.Select(o => o == null || o == DependencyProperty.UnsetValue ? NullValue : (bool)o);
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
