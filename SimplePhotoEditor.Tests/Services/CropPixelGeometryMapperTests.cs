using ImageMagick;
using SimplePhotoEditor.Core.Services;
using Xunit;

namespace SimplePhotoEditor.Tests.Services
{
    public class CropPixelGeometryMapperTests
    {
        [Fact]
        public void TryMap_FullControlOverCenteredImage_ReturnsFullPixelRect()
        {
            const int imgW = 1000;
            const int imgH = 800;
            const double ctlW = 500;
            const double ctlH = 400;

            var ok = CropPixelGeometryMapper.TryMapUiCropRectangleToPixelGeometry(
                0,
                0,
                (int)ctlW,
                (int)ctlH,
                ctlW,
                ctlH,
                imgW,
                imgH,
                out MagickGeometry g);

            Assert.True(ok);
            Assert.Equal(0, g.X);
            Assert.Equal(0, g.Y);
            Assert.Equal((uint)imgW, g.Width);
            Assert.Equal((uint)imgH, g.Height);
        }

        [Fact]
        public void TryMap_InvalidRect_ReturnsFalse()
        {
            var ok = CropPixelGeometryMapper.TryMapUiCropRectangleToPixelGeometry(
                0,
                0,
                0,
                10,
                100,
                100,
                500,
                500,
                out _);

            Assert.False(ok);
        }
    }
}
