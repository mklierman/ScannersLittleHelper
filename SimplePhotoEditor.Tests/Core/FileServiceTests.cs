using System;
using System.IO;
using SimplePhotoEditor.Core.Services;
using Xunit;

namespace SimplePhotoEditor.Tests.Core
{
    public class FileServiceTests : IDisposable
    {
        private readonly string _tempFolder;
        private readonly FileService _service;

        public FileServiceTests()
        {
            _tempFolder = Path.Combine(Path.GetTempPath(), "FileServiceTests_" + Guid.NewGuid().ToString("N"));
            _service = new FileService();
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempFolder))
            {
                Directory.Delete(_tempFolder, recursive: true);
            }
        }

        [Fact]
        public void Save_CreatesFolderIfMissing_AndWritesJsonFile()
        {
            var fileName = "data.json";
            var content = new SamplePayload { Name = "alpha", Value = 42 };

            _service.Save(_tempFolder, fileName, content);

            var fullPath = Path.Combine(_tempFolder, fileName);
            Assert.True(File.Exists(fullPath));

            var raw = File.ReadAllText(fullPath);
            Assert.Contains("alpha", raw);
            Assert.Contains("42", raw);
        }

        [Fact]
        public void Read_RoundTripsSavedContent()
        {
            var fileName = "data.json";
            var original = new SamplePayload { Name = "beta", Value = 7 };

            _service.Save(_tempFolder, fileName, original);
            var actual = _service.Read<SamplePayload>(_tempFolder, fileName);

            Assert.NotNull(actual);
            Assert.Equal(original.Name, actual.Name);
            Assert.Equal(original.Value, actual.Value);
        }

        [Fact]
        public void Read_ReturnsDefaultWhenFileMissing()
        {
            var actual = _service.Read<SamplePayload>(_tempFolder, "missing.json");
            Assert.Null(actual);
        }

        [Fact]
        public void Read_ReturnsDefaultForValueTypeWhenFileMissing()
        {
            var actual = _service.Read<int>(_tempFolder, "missing.json");
            Assert.Equal(0, actual);
        }

        [Fact]
        public void Delete_RemovesExistingFile()
        {
            var fileName = "delete-me.json";
            _service.Save(_tempFolder, fileName, new SamplePayload());

            var fullPath = Path.Combine(_tempFolder, fileName);
            Assert.True(File.Exists(fullPath));

            _service.Delete(_tempFolder, fileName);

            Assert.False(File.Exists(fullPath));
        }

        [Fact]
        public void Delete_DoesNothingWhenFileMissing()
        {
            Directory.CreateDirectory(_tempFolder);
            var ex = Record.Exception(() => _service.Delete(_tempFolder, "not-there.json"));
            Assert.Null(ex);
        }

        [Fact]
        public void Delete_DoesNothingWhenFileNameIsNull()
        {
            Directory.CreateDirectory(_tempFolder);
            var ex = Record.Exception(() => _service.Delete(_tempFolder, null));
            Assert.Null(ex);
        }

        private class SamplePayload
        {
            public string Name { get; set; }
            public int Value { get; set; }
        }
    }
}
