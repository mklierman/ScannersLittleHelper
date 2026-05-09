using Moq;
using Prism.Regions;
using Prism.Services.Dialogs;
using SimplePhotoEditor.Constants;
using SimplePhotoEditor.Contracts.Services;
using SimplePhotoEditor.Models;
using SimplePhotoEditor.Tests.Infrastructure;
using SimplePhotoEditor.ViewModels;
using Xunit;

namespace SimplePhotoEditor.Tests.ViewModels
{
    [Collection(WpfApplicationCollection.Name)]
    public class ThumbnailViewModelTests
    {
        private readonly WpfApplicationFixture _fixture;

        public ThumbnailViewModelTests(WpfApplicationFixture fixture)
        {
            _fixture = fixture;
            _fixture.ResetProperties();
        }

        private static ThumbnailViewModel CreateSut(
            Mock<IRegionManager> regionManager = null,
            Mock<IDialogService> dialogService = null,
            Mock<ISessionService> sessionService = null)
        {
            var rm = (regionManager ?? CreateRegionManagerWithEmptyRegions()).Object;
            var ds = (dialogService ?? new Mock<IDialogService>()).Object;
            var ss = (sessionService ?? new Mock<ISessionService>()).Object;

            var metaSut = new MetadataViewModel(rm, ds, ss, new Mock<IToastService>().Object);
            return new ThumbnailViewModel(rm, ds, ss, metaSut);
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
        public void Constructor_InitializesEmptyImagesCollection()
        {
            var sut = CreateSut();

            Assert.NotNull(sut.Images);
            Assert.Empty(sut.Images);
            Assert.NotNull(sut.MetaDataViewModel);
        }

        [Fact]
        public void DefaultSortOptions_AreFromConstants()
        {
            var sut = CreateSut();

            Assert.Equal(SortOptions.SortByKeyValuePair, sut.SortByOptions);
            Assert.Equal(SortOptions.SortAscDescKeyValuePair, sut.SortAscDescOptions);
        }

        [Fact]
        public void IsNavigationTarget_ReturnsTrue()
        {
            var sut = CreateSut();

            Assert.True(sut.IsNavigationTarget(null));
        }

        [Fact]
        public void OnNavigatedFrom_RecordsViewAndSelectionInSession()
        {
            var session = new Mock<ISessionService>();
            var sut = CreateSut(sessionService: session);
            var thumb = new Thumbnail { FileName = "img.jpg", FilePath = @"C:\img.jpg" };
            sut.Images.Add(thumb);
            sut.SelectedImage = thumb;
            sut.CurrentFolder = @"C:\images";

            sut.OnNavigatedFrom(null);

            session.VerifySet(s => s.PeviousView = PageKeys.Thumbnail);
            session.VerifySet(s => s.CurrentImagePath = @"C:\img.jpg");
            session.VerifySet(s => s.CurrentFolder = @"C:\images");
        }

        [Fact]
        public void SelectNextImage_NoOpWhenIndexInvalid()
        {
            var sut = CreateSut();
            sut.Images.Add(new Thumbnail { FileName = "first.jpg", FilePath = @"C:\first.jpg" });

            sut.SelectNextImage(-1);
            sut.SelectNextImage(99);

            Assert.Null(sut.SelectedImage);
        }

        [Fact]
        public void SelectNextImage_SelectsThumbnailAndUpdatesSession()
        {
            var session = new Mock<ISessionService>();
            var sut = CreateSut(sessionService: session);
            var second = new Thumbnail { FileName = "second.jpg", FilePath = @"C:\second.jpg" };
            sut.Images.Add(new Thumbnail { FileName = "first.jpg", FilePath = @"C:\first.jpg" });
            sut.Images.Add(second);

            sut.SelectNextImage(1);

            Assert.Same(second, sut.SelectedImage);
            session.VerifySet(s => s.CurrentImagePath = @"C:\second.jpg", Times.AtLeastOnce);
        }

        [Fact]
        public void RemoveIfMoved_RemovesEntryWhenSavedToDifferentDirectory()
        {
            var sut = CreateSut();
            sut.CurrentFolder = @"C:\source";
            sut.Images.Add(new Thumbnail { FileName = "moved.jpg", FilePath = @"C:\source\moved.jpg" });
            sut.Images.Add(new Thumbnail { FileName = "stay.jpg", FilePath = @"C:\source\stay.jpg" });

            sut.RemoveIfMoved("moved.jpg", @"C:\dest");

            Assert.Single(sut.Images);
            Assert.Equal("stay.jpg", sut.Images[0].FileName);
        }

        [Fact]
        public void RemoveIfMoved_LeavesCollectionUntouchedWhenSavedToSameDirectory()
        {
            var sut = CreateSut();
            sut.CurrentFolder = @"C:\source";
            sut.Images.Add(new Thumbnail { FileName = "stay.jpg", FilePath = @"C:\source\stay.jpg" });

            sut.RemoveIfMoved("stay.jpg", @"C:\source");

            Assert.Single(sut.Images);
        }

        [Fact]
        public void RemoveIfMoved_NoOpWhenFileNameMissing()
        {
            var sut = CreateSut();
            sut.CurrentFolder = @"C:\source";
            sut.Images.Add(new Thumbnail { FileName = "stay.jpg", FilePath = @"C:\source\stay.jpg" });

            sut.RemoveIfMoved("missing.jpg", @"C:\dest");

            Assert.Single(sut.Images);
        }

        [Fact]
        public void SelectedImage_PropagatesPathToMetadataViewModel()
        {
            var session = new Mock<ISessionService>();
            var sut = CreateSut(sessionService: session);
            var thumb = new Thumbnail { FileName = "x.jpg", FilePath = @"C:\x.jpg" };

            sut.SelectedImage = thumb;

            Assert.Same(thumb, sut.SelectedImage);
            Assert.Equal(PageKeys.Thumbnail, sut.MetaDataViewModel.CallingPage);
            session.VerifySet(s => s.CurrentImagePath = @"C:\x.jpg", Times.AtLeastOnce);
        }
    }
}
