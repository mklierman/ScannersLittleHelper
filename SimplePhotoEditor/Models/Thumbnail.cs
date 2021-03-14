using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows.Media.Imaging;

namespace SimplePhotoEditor.Models
{
    public class Thumbnail : BindableBase
    {
        private string fileName;
        public string FileName { get => fileName; set => SetProperty(ref fileName, value); }

        private DateTime createdDate;
        public DateTime CreatedDate { get => createdDate; set => SetProperty(ref createdDate, value); }

        private DateTime modifiedDate;
        public DateTime ModifiedDate { get => modifiedDate; set => SetProperty(ref modifiedDate, value); }

        private bool metaDataModified;
        public bool MetaDataModified { get => metaDataModified; set => SetProperty(ref metaDataModified, value); }

        private BitmapSource image;
        public BitmapSource Image { get => image; set => SetProperty(ref image, value); }

        private string filePath;
        public string FilePath { get => filePath; set => SetProperty(ref filePath, value); }

    }
}
