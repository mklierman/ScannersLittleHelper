using ImageProcessor;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Services.Dialogs;
using SimplePhotoEditor.Constants;
using SimplePhotoEditor.Managers;
using SimplePhotoEditor.Views;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SimplePhotoEditor.ViewModels
{
    public class SingleImageViewModel : BindableBase, INavigationAware
    {
        private ICommand autoCropCommand;
        private ICommand cropCommand;
        private ICommand rotateLeftCommand;
        private ICommand rotateRightCommand;
        private ICommand skewCommand;
        private ICommand undoCommand;
        private ICommand nextImageCommand;
        private ICommand previousImageCommand;
        private IDialogService DialogService;
        private ImageFactory selectedImage;
        private ImageSource previewImage;
        private IRegionManager RegionManager;
        private MetadataViewModel metadataViewModel;
        private ThumbnailViewModel ThumbnailViewModel;
        private string fileName;
        private string filePath;

        public SingleImageViewModel(IRegionManager regionManager, IDialogService dialogService)
        {
            DialogService = dialogService;
            RegionManager = regionManager;
        }

        public ICommand AutoCropCommand => autoCropCommand ?? (autoCropCommand = new DelegateCommand(GetImagePreview));
        public ICommand CropCommand => cropCommand ?? (cropCommand = new DelegateCommand<FrameworkElement>(StartCrop));
        public ICommand RotateLeftCommand => rotateLeftCommand ?? (rotateLeftCommand = new DelegateCommand(GetImagePreview));
        public ICommand RotateRightCommand => rotateRightCommand ?? (rotateRightCommand = new DelegateCommand(GetImagePreview));
        public ICommand SkewCommand => skewCommand ?? (skewCommand = new DelegateCommand(GetImagePreview));
        public ICommand UndoCommand => undoCommand ?? (undoCommand = new DelegateCommand(GetImagePreview));
        public ICommand NextImageCommand => nextImageCommand ?? (nextImageCommand = new DelegateCommand(SelectNextImage));
        public ICommand PreviousImageCommand => previousImageCommand ?? (previousImageCommand = new DelegateCommand(SelectPreviousImage));
        public string FileName
        {
            get => fileName;
            set
            {
                if (fileName != value)
                {
                    fileName = value;
                    RaisePropertyChanged(nameof(FileName));
                }
            }
        }

        public string FilePath
        {
            get => filePath;
            set
            {
                if (filePath != value)
                {
                    filePath = value;
                    RaisePropertyChanged(nameof(FilePath));
                    if (File.Exists(FilePath))
                    {
                        GetImagePreview();
                        ImageSelected();
                        FileName = Path.GetFileName(FilePath);
                    }
                }
            }
        }

        public MetadataViewModel MetaDataViewModel { get => metadataViewModel; set => SetProperty(ref metadataViewModel, value); }
        public ImageSource PreviewImage
        {
            get => previewImage;
            set
            {
                if (previewImage != value)
                {
                    previewImage = value;
                    RaisePropertyChanged(nameof(PreviewImage));
                }
            }
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            var parameter = navigationContext.Parameters["FilePath"]?.ToString();
            if (parameter != null)
            {
                FilePath = parameter;
            }
            else
            {
                GetThumbnailViewModel();
                FilePath = ThumbnailViewModel.SelectedImage?.FilePath;
            }
            CheckThumbnailListPosition();
        }

        private void CheckThumbnailListPosition()
        {
            GetThumbnailViewModel();
            NextImageEnabled = ThumbnailViewModel.Images.IndexOf(ThumbnailViewModel.SelectedImage) < ThumbnailViewModel.Images.Count - 1;
            PreviousImageEnabled = ThumbnailViewModel.Images.IndexOf(ThumbnailViewModel.SelectedImage) > 0;
        }

        private string sortMode;
        private string sortDirection;

        public string SortMode { get => sortMode; set => SetProperty(ref sortMode, value); }
        public string SortDirection { get => sortDirection; set => SetProperty(ref sortDirection, value); }


        private void GetThumbnailViewModel()
        {
            var thumbnailView = (ThumbnailPage)RegionManager.Regions[Regions.Main].GetView(PageKeys.Thumbnail);
            ThumbnailViewModel = (ThumbnailViewModel)thumbnailView.DataContext;
        }

        private bool previousImageEnabled = true;
        private bool nextImageEnabled = true;

        public bool PreviousImageEnabled { get => previousImageEnabled; set => SetProperty(ref previousImageEnabled, value); }
        public bool NextImageEnabled { get => nextImageEnabled; set => SetProperty(ref nextImageEnabled, value); }
        public void SelectNextImage()
        {
            GetThumbnailViewModel();
            if (ThumbnailViewModel.Images.IndexOf(ThumbnailViewModel.SelectedImage) != ThumbnailViewModel.Images.Count - 1)
            {
                var nextImageIndex = ThumbnailViewModel.Images.IndexOf(ThumbnailViewModel.SelectedImage) + 1;
                FilePath = ThumbnailViewModel.Images[nextImageIndex].FilePath;
                ThumbnailViewModel.SelectedImage = ThumbnailViewModel.Images[nextImageIndex];
            }
            CheckThumbnailListPosition();
        }

        public void SelectPreviousImage()
        {
            GetThumbnailViewModel();
            if (ThumbnailViewModel.Images.IndexOf(ThumbnailViewModel.SelectedImage) != 0)
            {
                var previousImageIndex = ThumbnailViewModel.Images.IndexOf(ThumbnailViewModel.SelectedImage) - 1;
                FilePath = ThumbnailViewModel.Images[previousImageIndex].FilePath;
                ThumbnailViewModel.SelectedImage = ThumbnailViewModel.Images[previousImageIndex];
            }
            CheckThumbnailListPosition();
        }

        private void GetImagePreview()
        {
            var bmi = new BitmapImage();
            bmi.BeginInit();
            bmi.CacheOption = BitmapCacheOption.OnLoad;
            bmi.UriSource = new Uri(FilePath);
            bmi.EndInit();
            PreviewImage = bmi;
        }

        private void ImageSelected()
        {
            if (MetaDataViewModel == null)
            {
                MetaDataViewModel = new MetadataViewModel(RegionManager, DialogService, PageKeys.SingleImage);
            }
            metadataViewModel.FilePath = FilePath;
        }

        private CropManager cropper = new CropManager();
        private void StartCrop(FrameworkElement frameworkElement)
        {
            cropper.AddCropToElement(frameworkElement);
        }

    }
}