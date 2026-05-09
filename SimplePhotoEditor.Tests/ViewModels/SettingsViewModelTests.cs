using System;
using System.Collections.ObjectModel;
using Moq;
using SimplePhotoEditor.Contracts.Services;
using SimplePhotoEditor.Models;
using SimplePhotoEditor.Tests.Infrastructure;
using SimplePhotoEditor.ViewModels;
using Xunit;

namespace SimplePhotoEditor.Tests.ViewModels
{
    [Collection(WpfApplicationCollection.Name)]
    public class SettingsViewModelTests
    {
        public SettingsViewModelTests(WpfApplicationFixture fixture)
        {
            fixture.ResetProperties();
        }

        private static SettingsViewModel CreateSut(
            AppConfig appConfig = null,
            Mock<IThemeSelectorService> themeSelectorService = null,
            Mock<ISystemService> systemService = null,
            Mock<IApplicationInfoService> applicationInfoService = null)
        {
            return new SettingsViewModel(
                appConfig ?? new AppConfig { PrivacyStatement = "https://example.com" },
                (themeSelectorService ?? new Mock<IThemeSelectorService>()).Object,
                (systemService ?? new Mock<ISystemService>()).Object,
                (applicationInfoService ?? new Mock<IApplicationInfoService>()).Object);
        }

        [Fact]
        public void IsNavigationTarget_ReturnsTrue()
        {
            var sut = CreateSut();

            Assert.True(sut.IsNavigationTarget(null));
        }

        [Fact]
        public void Defaults_AreSet()
        {
            var sut = CreateSut();

            Assert.NotNull(sut.ScannerList);
            Assert.Empty(sut.ScannerList);
            Assert.NotNull(sut.DPIList);
            Assert.Empty(sut.DPIList);
            Assert.Equal("Medium", sut.SelectedAutoCropStrength);
            Assert.Contains("Low", sut.AutoCropStrengthOptions);
            Assert.Contains("Medium", sut.AutoCropStrengthOptions);
            Assert.Contains("High", sut.AutoCropStrengthOptions);
            Assert.Equal(0, sut.SelectedDPI);
            Assert.Null(sut.SelectedScanner);
            Assert.Null(sut.SelectedScannerName);
            Assert.Null(sut.VersionDescription);
        }

        [Fact]
        public void Theme_RaisesPropertyChanged()
        {
            var sut = CreateSut();
            string changedProperty = null;
            sut.PropertyChanged += (_, e) => changedProperty = e.PropertyName;

            sut.Theme = AppTheme.Dark;

            Assert.Equal(nameof(SettingsViewModel.Theme), changedProperty);
            Assert.Equal(AppTheme.Dark, sut.Theme);
        }

        [Fact]
        public void VersionDescription_RaisesPropertyChanged()
        {
            var sut = CreateSut();

            sut.VersionDescription = "v1.0.0";

            Assert.Equal("v1.0.0", sut.VersionDescription);
        }

        [Fact]
        public void Commands_AreNotNull()
        {
            var sut = CreateSut();

            Assert.NotNull(sut.SetThemeCommand);
            Assert.NotNull(sut.PrivacyStatementCommand);
            Assert.NotNull(sut.RefreshScannersCommand);
        }

        [Fact]
        public void PrivacyStatementCommand_OpensConfiguredUrl()
        {
            var systemService = new Mock<ISystemService>();
            var sut = CreateSut(
                appConfig: new AppConfig { PrivacyStatement = "https://privacy.example.com" },
                systemService: systemService);

            sut.PrivacyStatementCommand.Execute(null);

            systemService.Verify(s => s.OpenInWebBrowser("https://privacy.example.com"), Times.Once);
        }

        [Fact]
        public void SetThemeCommand_DelegatesToThemeService()
        {
            var themeService = new Mock<IThemeSelectorService>();
            var sut = CreateSut(themeSelectorService: themeService);

            sut.SetThemeCommand.Execute("Dark");

            themeService.Verify(t => t.SetTheme(AppTheme.Dark), Times.Once);
        }

        [Fact]
        public void SetThemeCommand_ParsesLightTheme()
        {
            var themeService = new Mock<IThemeSelectorService>();
            var sut = CreateSut(themeSelectorService: themeService);

            sut.SetThemeCommand.Execute("Light");

            themeService.Verify(t => t.SetTheme(AppTheme.Light), Times.Once);
        }

        [Fact]
        public void SelectedAutoCropStrength_RoundTrips()
        {
            var sut = CreateSut();

            sut.SelectedAutoCropStrength = "High";

            Assert.Equal("High", sut.SelectedAutoCropStrength);
        }

        [Fact]
        public void DPIList_CanBeReplaced()
        {
            var sut = CreateSut();
            var newList = new ObservableCollection<int> { 100, 200, 300 };

            sut.DPIList = newList;

            Assert.Same(newList, sut.DPIList);
        }
    }
}
