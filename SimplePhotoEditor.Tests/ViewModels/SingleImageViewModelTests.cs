using System.Windows;
using Moq;
using Prism.Regions;
using Prism.Services.Dialogs;
using SimplePhotoEditor.Constants;
using SimplePhotoEditor.Contracts.Services;
using SimplePhotoEditor.Tests.Infrastructure;
using SimplePhotoEditor.ViewModels;
using Xunit;

namespace SimplePhotoEditor.Tests.ViewModels
{
    [Collection(WpfApplicationCollection.Name)]
    public class SingleImageViewModelTests
    {
        public SingleImageViewModelTests(WpfApplicationFixture fixture)
        {
            fixture.ResetProperties();
        }

        private static SingleImageViewModel CreateSut(
            Mock<IRegionManager> regionManager = null,
            Mock<IDialogService> dialogService = null,
            Mock<ISessionService> sessionService = null)
        {
            var rm = (regionManager ?? CreateRegionManagerWithEmptyRegions()).Object;
            var ds = (dialogService ?? new Mock<IDialogService>()).Object;
            var ss = (sessionService ?? new Mock<ISessionService>()).Object;
            var meta = new MetadataViewModel(rm, ds, ss, new Mock<IToastService>().Object);
            return new SingleImageViewModel(rm, ds, ss, meta);
        }

        private static Mock<IRegionManager> CreateRegionManagerWithEmptyRegions()
        {
            var regionCollection = new Mock<IRegionCollection>();
            regionCollection.Setup(c => c[It.IsAny<string>()]).Returns((IRegion)null);
            var regionManager = new Mock<IRegionManager>();
            regionManager.SetupGet(r => r.Regions).Returns(regionCollection.Object);
            return regionManager;
        }

        [Fact]
        public void IsNavigationTarget_ReturnsTrue()
        {
            var sut = CreateSut();

            Assert.True(sut.IsNavigationTarget(null));
        }

        [Fact]
        public void OnNavigatedFrom_RecordsViewKeyOnSession()
        {
            var session = new Mock<ISessionService>();
            var sut = CreateSut(sessionService: session);

            sut.OnNavigatedFrom(null);

            session.VerifySet(s => s.PeviousView = PageKeys.SingleImage);
        }

        [Fact]
        public void DefaultsAreSet()
        {
            var sut = CreateSut();

            Assert.True(sut.NextImageEnabled);
            Assert.True(sut.PreviousImageEnabled);
            Assert.False(sut.IsInSkewMode);
            Assert.False(sut.SkewLineVisible);
            Assert.Equal(Visibility.Hidden, sut.ApplyCancelVisibility);
            Assert.Equal(Visibility.Collapsed, sut.SkewInstructionsVisibility);
            Assert.Equal(Visibility.Collapsed, sut.SkewCancelVisibility);
            Assert.Null(sut.CurrentImageBytes);
        }

        [Fact]
        public void IsInSkewMode_TrueShowsSkewCancel()
        {
            var sut = CreateSut();

            sut.IsInSkewMode = true;

            Assert.True(sut.IsInSkewMode);
            Assert.Equal(Visibility.Visible, sut.SkewCancelVisibility);
        }

        [Fact]
        public void IsInSkewMode_FalseHidesSkewCancel()
        {
            var sut = CreateSut();
            sut.IsInSkewMode = true;

            sut.IsInSkewMode = false;

            Assert.False(sut.IsInSkewMode);
            Assert.Equal(Visibility.Collapsed, sut.SkewCancelVisibility);
        }

        [Fact]
        public void CancelActiveEdits_NoOpWhenNotInSkewMode()
        {
            var sut = CreateSut();

            sut.CancelActiveEdits();

            Assert.False(sut.IsInSkewMode);
        }

        [Fact]
        public void SelectNextImage_NoOpWhenNoThumbnailViewModelAvailable()
        {
            var sut = CreateSut();

            var ex = Record.Exception(() => sut.SelectNextImage());

            Assert.Null(ex);
        }

        [Fact]
        public void SelectPreviousImage_NoOpWhenNoThumbnailViewModelAvailable()
        {
            var sut = CreateSut();

            var ex = Record.Exception(() => sut.SelectPreviousImage());

            Assert.Null(ex);
        }

        [Fact]
        public void SortMode_RoundTrips()
        {
            var sut = CreateSut();
            string changedProperty = null;
            sut.PropertyChanged += (_, e) => changedProperty = e.PropertyName;

            sut.SortMode = "FileName";

            Assert.Equal("FileName", sut.SortMode);
            Assert.Equal(nameof(SingleImageViewModel.SortMode), changedProperty);
        }

        [Fact]
        public void SortDirection_RoundTrips()
        {
            var sut = CreateSut();

            sut.SortDirection = "Asc";

            Assert.Equal("Asc", sut.SortDirection);
        }
    }
}
