using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Moq;
using SimplePhotoEditor.Core.Contracts.Services;
using SimplePhotoEditor.Models;
using SimplePhotoEditor.Services;
using SimplePhotoEditor.Tests.Infrastructure;
using Xunit;

namespace SimplePhotoEditor.Tests.Services
{
    [Collection(WpfApplicationCollection.Name)]
    public class PersistAndRestoreServiceTests
    {
        private readonly WpfApplicationFixture _fixture;
        private readonly AppConfig _appConfig;
        private readonly string _expectedFolderRoot;

        public PersistAndRestoreServiceTests(WpfApplicationFixture fixture)
        {
            _fixture = fixture;
            _fixture.ResetProperties();
            _appConfig = new AppConfig
            {
                ConfigurationsFolder = "TestConfigurations",
                AppPropertiesFileName = "TestAppProperties.json"
            };
            _expectedFolderRoot = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }

        [Fact]
        public void PersistData_SavesApplicationPropertiesUsingFileService()
        {
            _fixture.Properties["alpha"] = "beta";
            var fileService = new Mock<IFileService>();
            var sut = new PersistAndRestoreService(fileService.Object, _appConfig);

            sut.PersistData();

            fileService.Verify(s => s.Save(
                Path.Combine(_expectedFolderRoot, _appConfig.ConfigurationsFolder),
                _appConfig.AppPropertiesFileName,
                _fixture.Properties), Times.Once);
        }

        [Fact]
        public void RestoreData_LoadsAndAppliesProperties()
        {
            var stored = new Dictionary<object, object>
            {
                { "saved-key", "saved-value" },
                { "count", 5 }
            };
            var fileService = new Mock<IFileService>();
            fileService
                .Setup(s => s.Read<IDictionary>(
                    Path.Combine(_expectedFolderRoot, _appConfig.ConfigurationsFolder),
                    _appConfig.AppPropertiesFileName))
                .Returns((IDictionary)stored);
            var sut = new PersistAndRestoreService(fileService.Object, _appConfig);

            sut.RestoreData();

            Assert.Equal("saved-value", _fixture.Properties["saved-key"]);
            Assert.Equal(5, _fixture.Properties["count"]);
        }

        [Fact]
        public void RestoreData_NoOpWhenFileServiceReturnsNull()
        {
            var fileService = new Mock<IFileService>();
            fileService
                .Setup(s => s.Read<IDictionary>(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((IDictionary)null);
            var sut = new PersistAndRestoreService(fileService.Object, _appConfig);

            var ex = Record.Exception(() => sut.RestoreData());

            Assert.Null(ex);
            Assert.Empty(_fixture.Properties);
        }
    }
}
