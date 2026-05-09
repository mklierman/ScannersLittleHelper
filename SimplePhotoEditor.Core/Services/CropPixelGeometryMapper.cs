using System;
using ImageMagick;

namespace SimplePhotoEditor.Core.Services
{
    /// <summary>
    /// Maps a crop rectangle in control coordinates to pixel coordinates on the source bitmap,
    /// assuming uniform scaling and letterboxing (same layout as WPF Image with uniform stretch).
    /// </summary>
    public static class CropPixelGeometryMapper
    {
        public static bool TryMapUiCropRectangleToPixelGeometry(
            int rectX,
            int rectY,
            int rectWidth,
            int rectHeight,
            double controlWidth,
            double controlHeight,
            int imagePixelWidth,
            int imagePixelHeight,
            out MagickGeometry geometry)
        {
            geometry = null;

            if (rectWidth <= 0 || rectHeight <= 0)
            {
                return false;
            }

            if (controlWidth <= 0 || controlHeight <= 0 || imagePixelWidth <= 0 || imagePixelHeight <= 0)
            {
                return false;
            }

            var uniformScale = Math.Min(controlWidth / imagePixelWidth, controlHeight / imagePixelHeight);
            var displayedWidth = imagePixelWidth * uniformScale;
            var displayedHeight = imagePixelHeight * uniformScale;
            var offsetX = (controlWidth - displayedWidth) / 2.0;
            var offsetY = (controlHeight - displayedHeight) / 2.0;

            var cropX1 = Math.Max(rectX, (int)offsetX);
            var cropY1 = Math.Max(rectY, (int)offsetY);
            var cropX2 = Math.Min(rectX + rectWidth, (int)(offsetX + displayedWidth));
            var cropY2 = Math.Min(rectY + rectHeight, (int)(offsetY + displayedHeight));

            var normalizedWidth = cropX2 - cropX1;
            var normalizedHeight = cropY2 - cropY1;
            if (normalizedWidth <= 0 || normalizedHeight <= 0)
            {
                return false;
            }

            var pixelX = (int)Math.Round((cropX1 - offsetX) / uniformScale);
            var pixelY = (int)Math.Round((cropY1 - offsetY) / uniformScale);
            var pixelWidth = (int)Math.Round(normalizedWidth / uniformScale);
            var pixelHeight = (int)Math.Round(normalizedHeight / uniformScale);

            if (pixelWidth <= 0 || pixelHeight <= 0)
            {
                return false;
            }

            geometry = new MagickGeometry(pixelX, pixelY, (uint)pixelWidth, (uint)pixelHeight);
            return true;
        }
    }
}
