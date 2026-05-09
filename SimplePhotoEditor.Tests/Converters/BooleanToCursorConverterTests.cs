using System;
using System.Globalization;
using System.Windows.Input;
using SimplePhotoEditor.Converters;
using Xunit;

namespace SimplePhotoEditor.Tests.Converters
{
    public class BooleanToCursorConverterTests
    {
        private readonly BooleanToCursorConverter _converter = new BooleanToCursorConverter();

        [Fact]
        public void Convert_ReturnsCrossCursorWhenEnabledWithCrossParameter()
        {
            var result = _converter.Convert(true, typeof(Cursor), "Cross", CultureInfo.InvariantCulture);

            Assert.Equal(Cursors.Cross, result);
        }

        [Fact]
        public void Convert_IsCaseInsensitiveForParameter()
        {
            var result = _converter.Convert(true, typeof(Cursor), "cRoSs", CultureInfo.InvariantCulture);

            Assert.Equal(Cursors.Cross, result);
        }

        [Fact]
        public void Convert_ReturnsArrowCursorForUnknownParameter()
        {
            var result = _converter.Convert(true, typeof(Cursor), "Other", CultureInfo.InvariantCulture);

            Assert.Equal(Cursors.Arrow, result);
        }

        [Fact]
        public void Convert_ReturnsArrowCursorWhenDisabled()
        {
            var result = _converter.Convert(false, typeof(Cursor), "Cross", CultureInfo.InvariantCulture);

            Assert.Equal(Cursors.Arrow, result);
        }

        [Fact]
        public void Convert_ReturnsArrowCursorForNonBoolValue()
        {
            var result = _converter.Convert("not-a-bool", typeof(Cursor), "Cross", CultureInfo.InvariantCulture);

            Assert.Equal(Cursors.Arrow, result);
        }

        [Fact]
        public void Convert_ReturnsArrowWhenParameterIsNull()
        {
            var result = _converter.Convert(true, typeof(Cursor), null, CultureInfo.InvariantCulture);

            Assert.Equal(Cursors.Arrow, result);
        }

        [Fact]
        public void ConvertBack_ThrowsNotImplementedException()
        {
            Assert.Throws<NotImplementedException>(() =>
                _converter.ConvertBack(Cursors.Arrow, typeof(bool), null, CultureInfo.InvariantCulture));
        }
    }
}
