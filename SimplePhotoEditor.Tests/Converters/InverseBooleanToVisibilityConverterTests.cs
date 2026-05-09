using System;
using System.Globalization;
using System.Windows;
using SimplePhotoEditor.Converters;
using Xunit;

namespace SimplePhotoEditor.Tests.Converters
{
    public class InverseBooleanToVisibilityConverterTests
    {
        private readonly InverseBooleanToVisibilityConverter _converter = new InverseBooleanToVisibilityConverter();

        [Fact]
        public void Convert_FalseProducesVisible()
        {
            var result = _converter.Convert(false, typeof(Visibility), null, CultureInfo.InvariantCulture);

            Assert.Equal(Visibility.Visible, result);
        }

        [Fact]
        public void Convert_TrueProducesCollapsed()
        {
            var result = _converter.Convert(true, typeof(Visibility), null, CultureInfo.InvariantCulture);

            Assert.Equal(Visibility.Collapsed, result);
        }

        [Fact]
        public void Convert_TrueWithIsEnabledParameterReturnsFalse()
        {
            var result = _converter.Convert(true, typeof(bool), "IsEnabled", CultureInfo.InvariantCulture);

            Assert.Equal(false, result);
        }

        [Fact]
        public void Convert_FalseWithIsEnabledParameterReturnsTrue()
        {
            var result = _converter.Convert(false, typeof(bool), "IsEnabled", CultureInfo.InvariantCulture);

            Assert.Equal(true, result);
        }

        [Fact]
        public void Convert_NonBoolValueWithIsEnabledParameterReturnsTrue()
        {
            var result = _converter.Convert(null, typeof(bool), "IsEnabled", CultureInfo.InvariantCulture);

            Assert.Equal(true, result);
        }

        [Fact]
        public void Convert_NonBoolValueWithoutIsEnabledParameterReturnsVisible()
        {
            var result = _converter.Convert(null, typeof(Visibility), null, CultureInfo.InvariantCulture);

            Assert.Equal(Visibility.Visible, result);
        }

        [Fact]
        public void ConvertBack_ThrowsNotImplementedException()
        {
            Assert.Throws<NotImplementedException>(() =>
                _converter.ConvertBack(Visibility.Visible, typeof(bool), null, CultureInfo.InvariantCulture));
        }
    }
}
