namespace SimplePhotoEditor.Core.Services
{
    /// <summary>
    /// Maps user-facing auto-crop strength labels to Magick.NET ColorFuzz percentages.
    /// </summary>
    public static class AutoCropFuzzResolver
    {
        public static double GetFuzzPercentFromStrengthLabel(string strengthLabel)
        {
            if (strengthLabel == "Low")
            {
                return 2.0;
            }

            if (strengthLabel == "High")
            {
                return 10.0;
            }

            return 5.0;
        }
    }
}
