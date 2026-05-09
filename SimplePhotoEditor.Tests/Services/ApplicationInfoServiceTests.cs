using System;
using SimplePhotoEditor.Services;
using Xunit;

namespace SimplePhotoEditor.Tests.Services
{
    public class ApplicationInfoServiceTests
    {
        [Fact]
        public void GetVersion_ReturnsNonNullVersion()
        {
            var sut = new ApplicationInfoService();

            var version = sut.GetVersion();

            Assert.NotNull(version);
        }

        [Fact]
        public void GetVersion_ReturnsValidVersionComponents()
        {
            var sut = new ApplicationInfoService();

            var version = sut.GetVersion();

            Assert.True(version.Major >= 0);
            Assert.True(version.Minor >= 0);
        }

        [Fact]
        public void GetVersion_IsStableAcrossCalls()
        {
            var sut = new ApplicationInfoService();

            var first = sut.GetVersion();
            var second = sut.GetVersion();

            Assert.Equal(first, second);
        }
    }
}
