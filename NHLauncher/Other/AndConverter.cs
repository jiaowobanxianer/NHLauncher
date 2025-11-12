using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace NHLauncher.Other
{
    public class AndConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            foreach (var value in values)
            {
                if (value is bool booleanValue && !booleanValue)
                {
                    return false;
                }
                if (value is Enum enumValue && System.Convert.ToInt32(enumValue) == 0)
                {
                    return false;
                }
            }
            return true;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("AndConverter does not support ConvertBack.");
        }
    }
}