using System;
using System.Globalization;
using SimplePhotoEditor.Converters;
using SimplePhotoEditor.Models;
using Xunit;

namespace SimplePhotoEditor.Tests.Converters
{
    public class EnumToBooleanConverterTests
    {
        private readonly EnumToBooleanConverter _converter = new EnumToBooleanConverter
        {
            EnumType = typeof(AppTheme)
        };

        [Fact]
        public void Convert_ReturnsTrueWhenValueMatchesParameter()
        {
            var result = _converter.Convert(AppTheme.Dark, typeof(bool), "Dark", CultureInfo.InvariantCulture);

            Assert.True((bool)result);
        }

        [Fact]
        public void Convert_ReturnsFalseWhenValueDiffersFromParameter()
        {
            var result = _converter.Convert(AppTheme.Light, typeof(bool), "Dark", CultureInfo.InvariantCulture);

            Assert.False((bool)result);
        }

        [Fact]
        public void Convert_ReturnsFalseWhenParameterIsNotString()
        {
            var result = _converter.Convert(AppTheme.Dark, typeof(bool), 42, CultureInfo.InvariantCulture);

            Assert.False((bool)result);
        }

        [Fact]
        public void Convert_ReturnsFalseWhenValueIsNotInEnum()
        {
            var result = _converter.Convert("not-an-enum", typeof(bool), "Dark", CultureInfo.InvariantCulture);

            Assert.False((bool)result);
        }

        [Fact]
        public void ConvertBack_ReturnsParsedEnumValue()
        {
            var result = _converter.ConvertBack(true, typeof(AppTheme), "Light", CultureInfo.InvariantCulture);

            Assert.Equal(AppTheme.Light, result);
        }

        [Fact]
        public void ConvertBack_ReturnsNullWhenParameterIsNotString()
        {
            var result = _converter.ConvertBack(true, typeof(AppTheme), 42, CultureInfo.InvariantCulture);

            Assert.Null(result);
        }

        [Fact]
        public void ConvertBack_ThrowsForUnknownEnumName()
        {
            Assert.Throws<ArgumentException>(() =>
                _converter.ConvertBack(true, typeof(AppTheme), "Bogus", CultureInfo.InvariantCulture));
        }
    }
}
