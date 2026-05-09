using SimplePhotoEditor.Services;
using Xunit;

namespace SimplePhotoEditor.Tests.Services
{
    public class SessionServiceTests
    {
        [Fact]
        public void DefaultsAreNull()
        {
            var sut = new SessionService();

            Assert.Null(sut.CurrentImagePath);
            Assert.Null(sut.CurrentFolder);
            Assert.Null(sut.CurrentTempFilePath);
            Assert.Null(sut.PeviousView);
        }

        [Fact]
        public void Properties_RoundTripValues()
        {
            var sut = new SessionService
            {
                CurrentImagePath = @"C:\images\one.jpg",
                CurrentFolder = @"C:\images",
                CurrentTempFilePath = @"C:\temp\one.tmp",
                PeviousView = "Thumbnail"
            };

            Assert.Equal(@"C:\images\one.jpg", sut.CurrentImagePath);
            Assert.Equal(@"C:\images", sut.CurrentFolder);
            Assert.Equal(@"C:\temp\one.tmp", sut.CurrentTempFilePath);
            Assert.Equal("Thumbnail", sut.PeviousView);
        }

        [Fact]
        public void Properties_CanBeReassigned()
        {
            var sut = new SessionService
            {
                CurrentImagePath = "first"
            };

            sut.CurrentImagePath = "second";

            Assert.Equal("second", sut.CurrentImagePath);
        }
    }
}
