using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ConflictScanner.Converters
{
    public class BooleanNegationConverter : IValueConverter
    {
        public static readonly BooleanNegationConverter Instance = new();

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool b ? !b : value ?? false;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool b ? !b : value ?? false;
    }
}
