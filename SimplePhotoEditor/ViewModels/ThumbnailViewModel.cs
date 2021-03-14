using System;

using Prism.Mvvm;
using System.Windows.Input;
using Prism.Commands;
using System.IO;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using SimplePhotoEditor.Models;
using System.Threading.Tasks;
using System.Windows.Data;
using System.ComponentModel;
using System.Windows.Threading;
using SimplePhotoEditor.Views;
using System.Windows;
using System.Linq;
using Microsoft.WindowsAPICodePack.Shell;
using System.Drawing;
using System.Windows.Interop;

namespace SimplePhotoEditor.ViewModels
{
    public class ThumbnailViewModel : BindableBase
    {
        private string currentFolder;
        private string filePath;
        private ICommand folderBrowseCommand;
        private AsyncObservableCollection<Thumbnail> images = new AsyncObservableCollection<Thumbnail>();
        private MetadataViewModel metadataViewModel = new MetadataViewModel();
        private Thumbnail selectedImage;

        public ThumbnailViewModel()
        {
            if (App.Current.Properties.Contains("LastThumbnailFolder"))
            {
                CurrentFolder = App.Current.Properties["LastThumbnailFolder"].ToString();
                if (!string.IsNullOrEmpty(CurrentFolder))
                {
                    Task.Run(() => CreateThumbnails());
                }
            }
        }

        public string CurrentFolder { get => currentFolder; set => SetProperty(ref currentFolder, value); }
        public string FilePath { get => filePath; set => SetProperty(ref filePath, value); }
        public ICommand FolderBrowseCommand => folderBrowseCommand ??= new DelegateCommand(FolderBrowse);
        public AsyncObservableCollection<Thumbnail> Images { get => images; set => SetProperty(ref images, value); }
        public MetadataViewModel MetaDataViewModel { get => metadataViewModel; set => SetProperty(ref metadataViewModel, value); }

        public Thumbnail SelectedImage
        {
            get => selectedImage;
            set
            {
                SetProperty(ref selectedImage, value);
                MetaDataViewModel.FilePath = value.FilePath;
            }
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        private BitmapSource Bitmap2BitmapImage(Bitmap bitmap)
        {
            IntPtr hBitmap = bitmap.GetHbitmap();
            BitmapSource retval;

            try
            {
                retval = (BitmapSource)Imaging.CreateBitmapSourceFromHBitmap(
                             hBitmap,
                             IntPtr.Zero,
                             Int32Rect.Empty,
                             BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(hBitmap);
            }

            return retval;
        }

        private void CreateThumbnails()
        {
            DirectoryInfo folder = new DirectoryInfo(CurrentFolder);
            foreach (FileInfo img in folder.GetFiles("*.tif*").OrderByDescending(x => x.Name).ToArray())
            {
                ShellFile shellFile = ShellFile.FromFilePath(img.FullName);

                var thumb = new Thumbnail();
                thumb.Image = Bitmap2BitmapImage(shellFile.Thumbnail.Bitmap);
                thumb.FileName = img.Name;
                thumb.CreatedDate = img.CreationTime;
                thumb.ModifiedDate = img.LastWriteTime;
                thumb.FilePath = img.FullName;
                thumb.MetaDataModified =
                    !string.IsNullOrEmpty(shellFile.Properties.System.Title.Value) ||
                    !string.IsNullOrEmpty(shellFile.Properties.System.Subject.Value) ||
                    !string.IsNullOrEmpty(shellFile.Properties.System.Comment.Value) ||
                    shellFile.Properties.System.Photo.DateTaken.Value != null ||
                    shellFile.Properties.System.Photo.TagViewAggregate.Value?.Length > 0;
                thumb.Image.Freeze();
                Images.Add(thumb);
            }
        }

        private void FolderBrowse()
        {

            var folderBrowser = new System.Windows.Forms.FolderBrowserDialog
            {
                ShowNewFolderButton = false,
                RootFolder = Environment.SpecialFolder.MyComputer
            };
            if (folderBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                CurrentFolder = folderBrowser.SelectedPath;
                App.Current.Properties["LastThumbnailFolder"] = folderBrowser.SelectedPath;
                Images.Clear();
                Task.Run(() => CreateThumbnails());
            }
        }

        private void ImageSelected()
        {
            metadataViewModel.FilePath = FilePath;
        }
    }
}
