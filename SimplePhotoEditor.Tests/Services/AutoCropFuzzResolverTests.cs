using SimplePhotoEditor.Core.Services;
using Xunit;

namespace SimplePhotoEditor.Tests.Services
{
    public class AutoCropFuzzResolverTests
    {
        [Theory]
        [InlineData("Low", 2.0)]
        [InlineData("High", 10.0)]
        [InlineData("Medium", 5.0)]
        [InlineData(null, 5.0)]
        [InlineData("", 5.0)]
        public void GetFuzzPercentFromStrengthLabel_MatchesExpected(string label, double expected)
        {
            Assert.Equal(expected, AutoCropFuzzResolver.GetFuzzPercentFromStrengthLabel(label));
        }
    }
}
