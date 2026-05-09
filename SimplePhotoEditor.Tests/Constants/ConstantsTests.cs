using System.Linq;
using SimplePhotoEditor.Constants;
using Xunit;

namespace SimplePhotoEditor.Tests.Constants
{
    public class ConstantsTests
    {
        [Theory]
        [InlineData(".tif")]
        [InlineData(".tiff")]
        [InlineData(".jpg")]
        [InlineData(".jpeg")]
        [InlineData(".png")]
        [InlineData(".gif")]
        [InlineData(".bmp")]
        public void Extensions_Images_ContainsCommonImageFormats(string extension)
        {
            Assert.Contains(extension, Extensions.Images);
        }

        [Fact]
        public void Extensions_Images_HasExpectedCount()
        {
            Assert.Equal(7, Extensions.Images.Length);
        }

        [Fact]
        public void Extensions_Images_AllStartWithDot()
        {
            Assert.All(Extensions.Images, ext => Assert.StartsWith(".", ext));
        }

        [Fact]
        public void Extensions_Images_AllLowercase()
        {
            Assert.All(Extensions.Images, ext => Assert.Equal(ext, ext.ToLowerInvariant()));
        }

        [Fact]
        public void PageKeys_HasExpectedValues()
        {
            Assert.Equal("SingleImage", PageKeys.SingleImage);
            Assert.Equal("Thumbnail", PageKeys.Thumbnail);
            Assert.Equal("Scan", PageKeys.Scan);
            Assert.Equal("Settings", PageKeys.Settings);
        }

        [Fact]
        public void Regions_MainHasExpectedValue()
        {
            Assert.Equal("MainRegion", Regions.Main);
        }

        [Fact]
        public void SortOptions_SortByKeyValuePair_ContainsCreatedDate()
        {
            Assert.Contains(SortOptions.SortByKeyValuePair,
                kv => kv.Key == "CreatedDate" && kv.Value == "Created Date");
        }

        [Fact]
        public void SortOptions_SortByKeyValuePair_ContainsModifiedDate()
        {
            Assert.Contains(SortOptions.SortByKeyValuePair,
                kv => kv.Key == "ModifiedDate" && kv.Value == "Modified Date");
        }

        [Fact]
        public void SortOptions_SortByKeyValuePair_ContainsFileName()
        {
            Assert.Contains(SortOptions.SortByKeyValuePair,
                kv => kv.Key == "FileName" && kv.Value == "File Name");
        }

        [Fact]
        public void SortOptions_SortAscDesc_ContainsAscendingAndDescending()
        {
            Assert.Contains(SortOptions.SortAscDescKeyValuePair,
                kv => kv.Key == "Asc" && kv.Value == "Ascending");
            Assert.Contains(SortOptions.SortAscDescKeyValuePair,
                kv => kv.Key == "Desc" && kv.Value == "Descending");
        }

        [Fact]
        public void SortOptions_HaveExpectedCounts()
        {
            Assert.Equal(3, SortOptions.SortByKeyValuePair.Length);
            Assert.Equal(2, SortOptions.SortAscDescKeyValuePair.Length);
        }

        [Fact]
        public void SortOptions_SortByKeys_AreUnique()
        {
            var keys = SortOptions.SortByKeyValuePair.Select(kv => kv.Key).ToArray();
            Assert.Equal(keys.Length, keys.Distinct().Count());
        }
    }
}
