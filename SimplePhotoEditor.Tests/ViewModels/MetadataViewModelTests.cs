using System;
using System.Collections.ObjectModel;
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
    public class MetadataViewModelTests
    {
        private readonly WpfApplicationFixture _fixture;

        public MetadataViewModelTests(WpfApplicationFixture fixture)
        {
            _fixture = fixture;
            _fixture.ResetProperties();
        }

        private static MetadataViewModel CreateSut(
            Mock<IRegionManager> regionManager = null,
            Mock<IDialogService> dialogService = null,
            Mock<ISessionService> sessionService = null,
            Mock<IToastService> toastService = null)
        {
            return new MetadataViewModel(
                (regionManager ?? new Mock<IRegionManager>()).Object,
                (dialogService ?? new Mock<IDialogService>()).Object,
                (sessionService ?? new Mock<ISessionService>()).Object,
                (toastService ?? new Mock<IToastService>()).Object);
        }

        [Fact]
        public void Constructor_InitializesEmptyCollections()
        {
            var sut = CreateSut();

            Assert.NotNull(sut.Tags);
            Assert.Empty(sut.Tags);
            Assert.NotNull(sut.SaveToFolderOptions);
            Assert.Empty(sut.SaveToFolderOptions);
        }

        [Fact]
        public void AddTag_AppendsTagToCollectionAndClearsInput()
        {
            var sut = CreateSut();
            sut.Tag = "vacation";

            sut.AddTag();

            Assert.Single(sut.Tags);
            Assert.Equal("vacation", sut.Tags[0]);
            Assert.Equal(string.Empty, sut.Tag);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void AddTag_IgnoresEmptyOrWhitespace(string value)
        {
            var sut = CreateSut();
            sut.Tag = value;

            sut.AddTag();

            Assert.Empty(sut.Tags);
        }

        [Fact]
        public void AddTag_SkipsDuplicateValues()
        {
            var sut = CreateSut();
            sut.Tag = "summer";
            sut.AddTag();
            sut.Tag = "summer";

            sut.AddTag();

            Assert.Single(sut.Tags);
            Assert.Equal("summer", sut.Tag);
        }

        [Fact]
        public void AddTag_ReinitializesCollectionWhenNull()
        {
            var sut = CreateSut();
            sut.Tags = null;
            sut.Tag = "first";

            sut.AddTag();

            Assert.NotNull(sut.Tags);
            Assert.Single(sut.Tags);
            Assert.Equal("first", sut.Tags[0]);
        }

        [Fact]
        public void RemoveTag_RemovesSelectedTag()
        {
            var sut = CreateSut();
            sut.Tags = new ObservableCollection<string> { "a", "b", "c" };
            sut.SelectedTag = "b";

            sut.RemoveTag();

            Assert.Equal(new[] { "a", "c" }, sut.Tags);
            Assert.Null(sut.SelectedTag);
        }

        [Fact]
        public void RemoveTag_DoesNothingWhenSelectedTagMissing()
        {
            var sut = CreateSut();
            sut.Tags = new ObservableCollection<string> { "a", "b" };
            sut.SelectedTag = null;

            sut.RemoveTag();

            Assert.Equal(2, sut.Tags.Count);
        }

        [Fact]
        public void RemoveTag_IgnoresValuesNotInCollection()
        {
            var sut = CreateSut();
            sut.Tags = new ObservableCollection<string> { "a" };
            sut.SelectedTag = "z";

            sut.RemoveTag();

            Assert.Equal(new[] { "a" }, sut.Tags);
        }

        [Fact]
        public void SelectedTag_MirroredToTagInput()
        {
            var sut = CreateSut();

            sut.SelectedTag = "blue";

            Assert.Equal("blue", sut.Tag);
        }

        [Fact]
        public void ClearMetadataForScan_ResetsEditableProperties()
        {
            var sut = CreateSut();
            sut.FileName = "file.jpg";
            sut.Title = "old";
            sut.Subject = "subject";
            sut.Comment = "comment";
            sut.Tag = "tag";
            sut.SelectedTag = "tag";
            sut.DateTaken = DateTime.Now;
            sut.Tags = new ObservableCollection<string> { "alpha" };

            sut.ClearMetadataForScan();

            Assert.Equal(string.Empty, sut.FileName);
            Assert.Equal(string.Empty, sut.Title);
            Assert.Equal(string.Empty, sut.Subject);
            Assert.Equal(string.Empty, sut.Comment);
            Assert.Null(sut.Tag);
            Assert.Null(sut.SelectedTag);
            Assert.Null(sut.DateTaken);
            Assert.Empty(sut.Tags);
            Assert.False(sut.FocusOnFileName);
        }

        [Theory]
        [InlineData(0, 230)]
        [InlineData(-5, 230)]
        [InlineData(231, 230)]
        [InlineData(50, 50)]
        [InlineData(230, 230)]
        public void MaxFileNameLength_IsClampedToValidRange(int input, int expected)
        {
            var sut = CreateSut();

            sut.MaxFileNameLength = input;

            Assert.Equal(expected, sut.MaxFileNameLength);
        }

        [Fact]
        public void Title_RaisesPropertyChangedOnce()
        {
            var sut = CreateSut();
            int callCount = 0;
            sut.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(MetadataViewModel.Title))
                {
                    callCount++;
                }
            };

            sut.Title = "first";
            sut.Title = "first";

            Assert.Equal(1, callCount);
        }

        [Fact]
        public void Subject_RaisesPropertyChangedOnce()
        {
            var sut = CreateSut();
            int callCount = 0;
            sut.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(MetadataViewModel.Subject))
                {
                    callCount++;
                }
            };

            sut.Subject = "first";
            sut.Subject = "first";

            Assert.Equal(1, callCount);
        }

        [Fact]
        public void Comment_RaisesPropertyChangedOnce()
        {
            var sut = CreateSut();
            int callCount = 0;
            sut.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(MetadataViewModel.Comment))
                {
                    callCount++;
                }
            };

            sut.Comment = "first";
            sut.Comment = "first";

            Assert.Equal(1, callCount);
        }

        [Fact]
        public void CallingPage_RaisesPropertyChanged()
        {
            var sut = CreateSut();
            string changedProperty = null;
            sut.PropertyChanged += (_, e) => changedProperty = e.PropertyName;

            sut.CallingPage = PageKeys.SingleImage;

            Assert.Equal(nameof(MetadataViewModel.CallingPage), changedProperty);
            Assert.Equal(PageKeys.SingleImage, sut.CallingPage);
        }
    }
}
