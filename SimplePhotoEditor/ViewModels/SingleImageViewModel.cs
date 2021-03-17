using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ImageProcessor;
using Prism.Mvvm;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using System.IO;
using Prism.Commands;
using System.Windows.Input;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Forms;
using ControlzEx.Theming;
using Prism.Regions;
using Prism.Services.Dialogs;
using SimplePhotoEditor.Constants;

namespace SimplePhotoEditor.ViewModels
{
    public class SingleImageViewModel : BindableBase, INavigationAware
    {
        private string filePath;
        private ImageFactory selectedImage;
        private ImageSource previewImage;


        private string fileName;
        private string title;
        private string subject;
        private string comments;
        private MetadataViewModel metadataViewModel;
        private IRegionManager RegionManager;
        private IDialogService DialogService;
        private DateTime dateTaken;
        private ObservableCollection<string> tags;
        private string tag;
        private ICommand saveCommand;
        private ICommand saveNextCommand;
        private ICommand cancelCommand;
        private ICommand cropCommand;
        private ICommand autoCropCommand;
        private ICommand rotateLeftCommand;
        private ICommand rotateRightCommand;
        private ICommand skewCommand;
        private ICommand undoCommand;


        public SingleImageViewModel(IRegionManager regionManager, IDialogService dialogService)
        {
            DialogService = dialogService;
            RegionManager = regionManager;
        }
        public ICommand CropCommand => cropCommand ?? (cropCommand = new DelegateCommand(GetImagePreview));
        public ICommand AutoCropCommand => autoCropCommand ?? (autoCropCommand = new DelegateCommand(GetImagePreview));
        public ICommand RotateLeftCommand => rotateLeftCommand ?? (rotateLeftCommand = new DelegateCommand(GetImagePreview));
        public ICommand RotateRightCommand => rotateRightCommand ?? (rotateRightCommand = new DelegateCommand(GetImagePreview));
        public ICommand SkewCommand => skewCommand ?? (skewCommand = new DelegateCommand(GetImagePreview));
        public ICommand UndoCommand => undoCommand ?? (undoCommand = new DelegateCommand(GetImagePreview));

        public MetadataViewModel MetaDataViewModel { get => metadataViewModel; set => SetProperty(ref metadataViewModel, value); }
        private void ImageSelected()
        {
            if (MetaDataViewModel == null)
            {
                MetaDataViewModel = new MetadataViewModel(RegionManager, DialogService, PageKeys.SingleImage);
            }
            metadataViewModel.FilePath = FilePath;
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
                    }
                }
            }
        }

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

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            FilePath = navigationContext.Parameters["FilePath"]?.ToString();
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {

        }

        public void SelectNextImage()
        {
            //Get list of images in current directory
            //Sort by user preference
            //Set FilePath to image after current one
            throw new NotImplementedException();
        }

        public void SelectPreviousImage()
        {

        }
        //        internal void DrawCropRectangle(object sender, MouseButtonEventArgs e)
        //        {
        //            if (e.LeftButton == MouseButtonState.Pressed)
        //            {
        //                System.Windows.Point point = e.GetPosition((System.Windows.IInputElement)sender);

        //                cropX = point.X;
        //                cropY = point.Y;
        //                cropPen = new System.Drawing.Pen(System.Drawing.Color.Black, 1);
        //                cropPen.DashStyle = System.Drawing.Drawing2D.DashStyle.DashDot;
        //                System.Windows.Controls.Image imageControl = (System.Windows.Controls.Image)sender;
        //                imageControl.UpdateLayout();
        //            }

        //        }


        //        internal void MoveCropRectangle(object sender, MouseEventArgs e)
        //{
        //            cropWidth = e.X - cropX;
        //            cropHeight = e.Y - cropY;
        //            System.Windows.Controls.Image imageControl = (System.Windows.Controls.Image)sender;
        //            imageControl.CreateGraphics().DrawRectangle(cropPen, cropX, cropY, cropWidth, cropHeight);
        //            imageControl.
        //        }
    }
}
