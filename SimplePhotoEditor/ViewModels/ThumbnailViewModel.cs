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
using Prism.Regions;
using SimplePhotoEditor.Constants;
using System.Collections.Generic;
using System.Windows.Controls;
using SimplePhotoEditor.Helpers;
using Prism.Services.Dialogs;

namespace SimplePhotoEditor.ViewModels
{
    public class ThumbnailViewModel : BindableBase, INavigationAware
    {
        private string currentFolder;
        private string filePath;
        private KeyValuePair<string, string>[] sortByOptions = SortOptions.SortByKeyValuePair;
        private string selectedSortBy = "FileName";
        private KeyValuePair<string, string>[] sortAscDescOptions = SortOptions.SortAscDescKeyValuePair;
        private string selectedSortAscDesc = "Asc";
        private ICommand refreshCommand;
        private IRegionManager RegionManager;
        private IDialogService DialogService;
        private ICommand folderBrowseCommand;
        private AsyncObservableCollection<Thumbnail> images = new AsyncObservableCollection<Thumbnail>();
        private MetadataViewModel metadataViewModel;
        private Thumbnail selectedImage;

        public KeyValuePair<string, string>[] SortByOptions { get => sortByOptions; set => SetProperty(ref sortByOptions, value); }
        public string SelectedSortBy { get => selectedSortBy; set { SetProperty(ref selectedSortBy, value); OrderImageList(); } }
        public KeyValuePair<string, string>[] SortAscDescOptions { get => sortAscDescOptions; set => SetProperty(ref sortAscDescOptions, value); }
        public string SelectedSortAscDesc
        {
            get => selectedSortAscDesc;
            set
            {
                SetProperty(ref selectedSortAscDesc, value);
                OrderImageList();
            }
        }
        public ICommand RefreshCommand => refreshCommand ??= new DelegateCommand(RefreshImageList);

        private void RefreshImageList()
        {
            Images.Clear();
            Task.Run(() => CreateThumbnails());
        }

        private void WatchForNewFiles()
        {
            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = CurrentFolder;
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.IncludeSubdirectories = false;
            foreach (var filter in Extensions.Images)
            {
                watcher.Filters.Add(filter);
            }
            watcher.Changed += FolderContentsChanged;
            watcher.EnableRaisingEvents = true;
        }

        private void FolderContentsChanged(object sender, FileSystemEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void OrderImageList()
        {
            var sortedList = new AsyncObservableCollection<Thumbnail>();
            switch (SelectedSortBy, SelectedSortAscDesc)
            {
                case ("CreatedDate", "Asc"):
                    Images.Sort(o => o.CreatedDate);
                    break;
                case ("ModifiedDate", "Asc"):
                    Images.Sort(o => o.ModifiedDate);
                    break;
                case ("FileName", "Asc"):
                    Images.Sort(o => o.FileName);
                    break;
                case ("CreatedDate", "Desc"):
                    Images.Sort(o => o.CreatedDate, true);
                    break;
                case ("ModifiedDate", "Desc"):
                    Images.Sort(o => o.CreatedDate, true);
                    break;
                case ("FileName", "Desc"):
                    Images.Sort(o => o.CreatedDate, true);
                    break;
            }

        }

        internal void OpenSingleImage()
        {

            var navParams = new NavigationParameters();
            navParams.Add("FilePath", SelectedImage.FilePath);
            RegionManager.RequestNavigate(Regions.Main, PageKeys.SingleImage, navParams);
        }


        public ThumbnailViewModel()
        {
            if (App.Current.Properties.Contains("LastThumbnailFolder"))
            {
                if (Directory.Exists(App.Current.Properties["LastThumbnailFolder"].ToString()))
                {
                    CurrentFolder = App.Current.Properties["LastThumbnailFolder"].ToString();
                    if (!string.IsNullOrEmpty(CurrentFolder))
                    {
                        Task.Run(() => CreateThumbnails());
                    }
                }
            }
        }

        public ThumbnailViewModel(IRegionManager regionManager, IDialogService dialogService)
        {
            DialogService = dialogService;
            RegionManager = regionManager;
            object lockObj = new object();
            BindingOperations.EnableCollectionSynchronization(Images, lockObj);
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
                if (MetaDataViewModel == null)
                {
                    MetaDataViewModel = new MetadataViewModel(RegionManager, DialogService, PageKeys.Thumbnail);
                }
                MetaDataViewModel.CallingPage = PageKeys.Thumbnail;
                MetaDataViewModel.FilePath = value?.FilePath;
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

        public void SelectNextImage(int nextImageIndex)
        {
            if (nextImageIndex < Images.Count)
            {
                SelectedImage = Images[nextImageIndex];
            }
        }

        private void CreateThumbnails()
        {
            DirectoryInfo folder = new DirectoryInfo(CurrentFolder);

            FileInfo[] files = folder?.GetFiles("*.*")
                .Where(f => Extensions.Images.Contains(f.Extension.ToLower())).ToArray();

            foreach (FileInfo img in files?.OrderBy(x => x.Name).ToArray())
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

        public  void RemoveIfMoved(string fileName, string saveDirectory)
        {
            if (CurrentFolder != saveDirectory && Images.Where(i => i.FileName == fileName).Any())
            {
                Images.Remove(Images.Where(i => i.FileName == fileName).FirstOrDefault());
            }
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            if (string.IsNullOrEmpty(CurrentFolder))
            {
                if (App.Current.Properties.Contains("LastThumbnailFolder") && Directory.Exists(App.Current.Properties["LastThumbnailFolder"].ToString()))
                {
                    CurrentFolder = App.Current.Properties["LastThumbnailFolder"].ToString();
                    if (!string.IsNullOrEmpty(CurrentFolder))
                    {
                        Task.Run(() => CreateThumbnails());
                    }
                }
            }
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            //throw new NotImplementedException();
        }
    }
}
