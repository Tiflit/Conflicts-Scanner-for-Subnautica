using System;
using System.Globalization;
using Avalonia.Media;
using ConflictScanner;
using ConflictScanner.Converters;
using Xunit;

namespace ConflictScanner.Tests
{
    public class ImpactColorConverterTests
    {
        [Theory]
        [InlineData(Impact.Critical)]
        [InlineData(Impact.High)]
        [InlineData(Impact.Medium)]
        [InlineData(Impact.Low)]
        [InlineData(Impact.Info)]
        public void Convert_AllImpactValues_ReturnSolidColorBrush(Impact impact)
        {
            var converter = new ImpactColorConverter();
            var result = converter.Convert(impact, typeof(IBrush), null, CultureInfo.InvariantCulture);

            Assert.NotNull(result);
            Assert.IsAssignableFrom<IBrush>(result);
        }

        [Fact]
        public void Convert_InvalidOrNullValue_ReturnsFallbackBrush()
        {
            var converter = new ImpactColorConverter();
            var result = converter.Convert(null, typeof(IBrush), null, CultureInfo.InvariantCulture);

            Assert.NotNull(result);
            Assert.IsAssignableFrom<IBrush>(result);
        }

        [Fact]
        public void ConvertBack_ThrowsNotSupportedException()
        {
            var converter = new ImpactColorConverter();
            Assert.Throws<NotSupportedException>(() =>
                converter.ConvertBack(null, typeof(Impact), null, CultureInfo.InvariantCulture));
        }
    }
}
