using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ConflictScanner.Converters
{
    public class ImpactColorConverter : IValueConverter
    {
        private static readonly IBrush CriticalBrush = new SolidColorBrush(Color.Parse("#E53935")); // Crimson Red
        private static readonly IBrush HighBrush     = new SolidColorBrush(Color.Parse("#EF6C00")); // Deep Orange
        private static readonly IBrush MediumBrush   = new SolidColorBrush(Color.Parse("#FB8C00")); // Amber
        private static readonly IBrush LowBrush      = new SolidColorBrush(Color.Parse("#0288D1")); // Light Blue
        private static readonly IBrush InfoBrush     = new SolidColorBrush(Color.Parse("#546E7A")); // Slate Gray

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Impact impact)
            {
                return impact switch
                {
                    Impact.Critical => CriticalBrush,
                    Impact.High => HighBrush,
                    Impact.Medium => MediumBrush,
                    Impact.Low => LowBrush,
                    Impact.Info => InfoBrush,
                    _ => InfoBrush
                };
            }
            return InfoBrush;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
