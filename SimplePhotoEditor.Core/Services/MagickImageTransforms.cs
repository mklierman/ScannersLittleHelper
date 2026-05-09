using ImageMagick;

namespace SimplePhotoEditor.Core.Services
{
    /// <summary>
    /// Stateless Magick.NET operations on in-memory image bytes.
    /// </summary>
    public static class MagickImageTransforms
    {
        public static byte[] AutoTrim(byte[] imageBytes, double fuzzPercent, MagickFormat outputFormat)
        {
            using (var image = new MagickImage(imageBytes))
            {
                image.ColorFuzz = new Percentage(fuzzPercent);
                image.Trim();
                image.ResetPage();
                return image.ToByteArray(outputFormat);
            }
        }

        public static byte[] Rotate(byte[] imageBytes, double degrees, MagickFormat outputFormat)
        {
            using (var image = new MagickImage(imageBytes))
            {
                image.Rotate(degrees);
                return image.ToByteArray(outputFormat);
            }
        }

        public static byte[] Crop(byte[] imageBytes, IMagickGeometry geometry, MagickFormat outputFormat)
        {
            using (var image = new MagickImage(imageBytes))
            {
                image.Crop(geometry);
                image.ResetPage();
                return image.ToByteArray(outputFormat);
            }
        }
    }
}
