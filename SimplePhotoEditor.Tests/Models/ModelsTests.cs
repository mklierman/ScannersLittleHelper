using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using ImageProcessor.Imaging;
using SimplePhotoEditor.Models;
using Xunit;

namespace SimplePhotoEditor.Tests.Models
{
    public class ModelsTests
    {
        [Fact]
        public void EditUndoModel_DefaultConstructor_InitializesImageEdits()
        {
            var sut = new EditUndoModel();

            Assert.NotNull(sut.ImageEdits);
            Assert.Null(sut.ImageSource);
            Assert.Null(sut.ImageBytes);
            Assert.Null(sut.TempFilePath);
        }

        [Fact]
        public void EditUndoModel_ParameterizedConstructor_AssignsAllFields()
        {
            var bytes = new byte[] { 1, 2, 3 };
            var crop = new CropLayer(0f, 0f, 10f, 10f, CropMode.Pixels);

            var sut = new EditUndoModel(null, bytes, "temp.tmp", crop);

            Assert.NotNull(sut.ImageEdits);
            Assert.Same(bytes, sut.ImageBytes);
            Assert.Equal("temp.tmp", sut.TempFilePath);
            Assert.Equal(crop, sut.ImageEdits.cropLayer);
        }

        [Fact]
        public void ImageEdits_DefaultsToNullCropAndZeroRotation()
        {
            var sut = new ImageEdits();

            Assert.Null(sut.cropLayer);
            Assert.Equal(0d, sut.rotateAmount);
        }

        [Fact]
        public void ImageEdits_RoundTripsRotateAmount()
        {
            var sut = new ImageEdits { rotateAmount = 90 };

            Assert.Equal(90d, sut.rotateAmount);
        }

        [Fact]
        public void AppConfig_StoresAllProperties()
        {
            var sut = new AppConfig
            {
                ConfigurationsFolder = "Configurations",
                AppPropertiesFileName = "AppProperties.json",
                PrivacyStatement = "https://example.com/privacy"
            };

            Assert.Equal("Configurations", sut.ConfigurationsFolder);
            Assert.Equal("AppProperties.json", sut.AppPropertiesFileName);
            Assert.Equal("https://example.com/privacy", sut.PrivacyStatement);
        }

        [Fact]
        public void Thumbnail_RaisesPropertyChangedForFileName()
        {
            var sut = new Thumbnail();
            string changedProperty = null;
            ((INotifyPropertyChanged)sut).PropertyChanged += (_, e) => changedProperty = e.PropertyName;

            sut.FileName = "image.jpg";

            Assert.Equal(nameof(Thumbnail.FileName), changedProperty);
            Assert.Equal("image.jpg", sut.FileName);
        }

        [Fact]
        public void Thumbnail_DoesNotRaiseWhenSameValueAssigned()
        {
            var sut = new Thumbnail { FileName = "image.jpg" };
            int callCount = 0;
            ((INotifyPropertyChanged)sut).PropertyChanged += (_, _) => callCount++;

            sut.FileName = "image.jpg";

            Assert.Equal(0, callCount);
        }

        [Fact]
        public void Thumbnail_RoundTripsAllProperties()
        {
            var created = new DateTime(2023, 1, 2, 3, 4, 5);
            var modified = new DateTime(2024, 6, 7, 8, 9, 10);

            var sut = new Thumbnail
            {
                FileName = "image.jpg",
                CreatedDate = created,
                ModifiedDate = modified,
                MetaDataModified = true,
                FilePath = @"C:\images\image.jpg"
            };

            Assert.Equal("image.jpg", sut.FileName);
            Assert.Equal(created, sut.CreatedDate);
            Assert.Equal(modified, sut.ModifiedDate);
            Assert.True(sut.MetaDataModified);
            Assert.Equal(@"C:\images\image.jpg", sut.FilePath);
        }

        [Fact]
        public void AsyncObservableCollection_AddRaisesCollectionChanged()
        {
            var collection = new AsyncObservableCollection<int>();
            NotifyCollectionChangedAction? action = null;
            collection.CollectionChanged += (_, e) => action = e.Action;

            collection.Add(1);

            Assert.Equal(NotifyCollectionChangedAction.Add, action);
            Assert.Single(collection);
        }

        [Fact]
        public void AsyncObservableCollection_FromInitialList_HasItems()
        {
            var collection = new AsyncObservableCollection<int>(new[] { 1, 2, 3 });

            Assert.Equal(3, collection.Count);
            Assert.Equal(new[] { 1, 2, 3 }, collection);
        }

        [Fact]
        public void ImageInfo_StoresMutableFields()
        {
            var info = new ImageInfo
            {
                FileName = "image.jpg",
                FilePath = @"C:\images\image.jpg",
                NewFilePath = @"C:\images\image-edit.jpg",
                Title = "Title",
                Comment = "Comment",
                Subject = "Subject",
                SelectedSaveToFolder = @"C:\out",
                DateTaken = new DateTime(2024, 1, 1)
            };

            Assert.Equal("image.jpg", info.FileName);
            Assert.Equal(@"C:\images\image.jpg", info.FilePath);
            Assert.Equal(@"C:\images\image-edit.jpg", info.NewFilePath);
            Assert.Equal("Title", info.Title);
            Assert.Equal("Comment", info.Comment);
            Assert.Equal("Subject", info.Subject);
            Assert.Equal(@"C:\out", info.SelectedSaveToFolder);
            Assert.Equal(new DateTime(2024, 1, 1), info.DateTaken);
        }
    }
}
