using System.Linq;
using MahApps.Metro.Controls;
using Moq;
using Prism.Regions;
using SimplePhotoEditor.Constants;
using SimplePhotoEditor.Tests.Infrastructure;
using SimplePhotoEditor.ViewModels;
using Xunit;

namespace SimplePhotoEditor.Tests.ViewModels
{
    [Collection(WpfApplicationCollection.Name)]
    public class ShellViewModelTests
    {
        private readonly WpfApplicationFixture _fixture;

        public ShellViewModelTests(WpfApplicationFixture fixture)
        {
            _fixture = fixture;
            _fixture.ResetProperties();
        }

        private static ShellViewModel CreateSut()
            => new ShellViewModel(new Mock<IRegionManager>().Object);

        [Fact]
        public void MenuItems_AreInitializedWithExpectedTags()
        {
            var sut = CreateSut();

            var tags = sut.MenuItems.Select(i => i.Tag?.ToString()).ToArray();

            Assert.Contains(PageKeys.Scan, tags);
            Assert.Contains(PageKeys.Thumbnail, tags);
            Assert.Contains(PageKeys.SingleImage, tags);
            Assert.Equal(3, sut.MenuItems.Count);
        }

        [Fact]
        public void OptionMenuItems_ContainSettingsTag()
        {
            var sut = CreateSut();

            Assert.Single(sut.OptionMenuItems);
            Assert.Equal(PageKeys.Settings, sut.OptionMenuItems[0].Tag?.ToString());
        }

        [Fact]
        public void IsPaneOpen_DefaultsToTrue()
        {
            var sut = CreateSut();

            Assert.True(sut.IsPaneOpen);
        }

        [Fact]
        public void IsPaneOpen_PropagatesToApplicationProperties()
        {
            var sut = CreateSut();

            sut.IsPaneOpen = false;

            Assert.False(sut.IsPaneOpen);
            Assert.True(_fixture.Properties.Contains("NavigationPaneOpen"));
            Assert.Equal(false, _fixture.Properties["NavigationPaneOpen"]);
        }

        [Fact]
        public void IsPaneOpen_SettingSameValueDoesNotWriteProperty()
        {
            var sut = CreateSut();
            _fixture.ResetProperties();

            sut.IsPaneOpen = true;

            Assert.False(_fixture.Properties.Contains("NavigationPaneOpen"));
        }

        [Fact]
        public void SelectedMenuItem_RaisesPropertyChanged()
        {
            var sut = CreateSut();
            string changedProperty = null;
            sut.PropertyChanged += (_, e) => changedProperty = e.PropertyName;

            sut.SelectedMenuItem = sut.MenuItems[0];

            Assert.Equal(nameof(ShellViewModel.SelectedMenuItem), changedProperty);
            Assert.Same(sut.MenuItems[0], sut.SelectedMenuItem);
        }

        [Fact]
        public void SelectedOptionsMenuItem_RaisesPropertyChanged()
        {
            var sut = CreateSut();
            string changedProperty = null;
            sut.PropertyChanged += (_, e) => changedProperty = e.PropertyName;

            sut.SelectedOptionsMenuItem = sut.OptionMenuItems[0];

            Assert.Equal(nameof(ShellViewModel.SelectedOptionsMenuItem), changedProperty);
            Assert.Same(sut.OptionMenuItems[0], sut.SelectedOptionsMenuItem);
        }

        [Fact]
        public void GoBackCommand_DefaultsToCannotExecute()
        {
            var sut = CreateSut();

            Assert.False(sut.GoBackCommand.CanExecute());
        }

        [Fact]
        public void Commands_AreNotNull()
        {
            var sut = CreateSut();

            Assert.NotNull(sut.LoadedCommand);
            Assert.NotNull(sut.UnloadedCommand);
            Assert.NotNull(sut.MenuItemInvokedCommand);
            Assert.NotNull(sut.OptionsMenuItemInvokedCommand);
            Assert.NotNull(sut.GoBackCommand);
        }
    }
}
