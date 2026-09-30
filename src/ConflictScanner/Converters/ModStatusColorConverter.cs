using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using ConflictScanner;

namespace ConflictScanner.Converters
{
    public class ModStatusColorConverter : IValueConverter
    {
        private static readonly IBrush GreenBrush = new SolidColorBrush(Color.Parse("#2E7D32"));
        private static readonly IBrush OrangeBrush = new SolidColorBrush(Color.Parse("#E65100"));
        private static readonly IBrush RedBrush = new SolidColorBrush(Color.Parse("#C62828"));
        private static readonly IBrush GrayBrush = new SolidColorBrush(Color.Parse("#757575"));

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is ModHealthStatus status)
            {
                return status switch
                {
                    ModHealthStatus.Clean => GreenBrush,
                    ModHealthStatus.Warning => OrangeBrush,
                    ModHealthStatus.Error => RedBrush,
                    _ => GrayBrush
                };
            }

            return GrayBrush;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
