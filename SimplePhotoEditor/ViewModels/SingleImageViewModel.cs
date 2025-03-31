using ImageProcessor;
using ImageProcessor.Imaging;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Services.Dialogs;
using SimplePhotoEditor.Constants;
using SimplePhotoEditor.Managers;
using SimplePhotoEditor.Models;
using SimplePhotoEditor.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Controls;
using ImageMagick;
using System.Threading.Tasks;
using SimplePhotoEditor.Contracts.Services;
using System.Diagnostics;

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
        private ICommand applyCommand;
        private ICommand cancelCommand;
        private IDialogService DialogService;
        private ISessionService SessionService;
        private ImageFactory selectedImage;
        private ImageSource previewImage;
        private IRegionManager RegionManager;
        private MetadataViewModel metadataViewModel;
        private ThumbnailViewModel ThumbnailViewModel;
        private string tempFilePath;
        private string fileName;
        private string filePath;
        private string applyButtonText;
        private string cancelButtonText;
        private Stack<EditUndoModel> imageUndoStack = new Stack<EditUndoModel>();
        private byte[] currentImageBytes;
        private bool isDrawingSkewLine = false;
        private Point skewStartPoint;
        private Line skewLine;
        private bool skewLineVisible = false;
        private bool isInSkewMode = false;
        private string skewInstructions = "Draw a line along what should be horizontal";
        private Visibility skewInstructionsVisibility = Visibility.Collapsed;

        public byte[] CurrentImageBytes => currentImageBytes;

        public SingleImageViewModel(IRegionManager regionManager, IDialogService dialogService, ISessionService sessionService, MetadataViewModel metadataViewModel)
        {
            DialogService = dialogService;
            RegionManager = regionManager;
            SessionService = sessionService;
            MetaDataViewModel = metadataViewModel;
        }

        public ICommand AutoCropCommand => autoCropCommand ?? (autoCropCommand = new DelegateCommand(AutoCrop));

        private void AutoCrop()
        {
            try
            {
                using (var image = new MagickImage(currentImageBytes ?? File.ReadAllBytes(FilePath)))
                {
                    // Trim the image (removes edges that are the same color as the corner pixels)
                    image.Trim();
                    image.RePage();
                    
                    // Store the result
                    currentImageBytes = image.ToByteArray(MagickFormat.Jpeg);
                    
                    // Add to undo stack
                    var editModel = new EditUndoModel(PreviewImage, null, null);
                    imageUndoStack.Push(editModel);
                }
                
                RefreshPreviewImageFromBytes(currentImageBytes);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in auto-crop: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public ICommand CropCommand => cropCommand ?? (cropCommand = new DelegateCommand<FrameworkElement>(StartCrop));
        public ICommand RotateLeftCommand => rotateLeftCommand ?? (rotateLeftCommand = new DelegateCommand(RotateLeft));

		private void RotateLeft()
		{
			try
			{
				using (var image = new MagickImage(currentImageBytes ?? File.ReadAllBytes(tempFilePath ?? FilePath)))
				{
					image.Rotate(-90);
					currentImageBytes = image.ToByteArray(MagickFormat.Jpeg);
				}
				
				var editModel = new EditUndoModel(PreviewImage, null, null);
				imageUndoStack.Push(editModel);
				RefreshPreviewImageFromBytes(currentImageBytes);
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error rotating left: {ex.Message}");
				throw;
			}
		}

		public ICommand RotateRightCommand => rotateRightCommand ?? (rotateRightCommand = new DelegateCommand(RotateRight));

		private void RotateRight()
		{
			try
			{
				using (var image = new MagickImage(currentImageBytes ?? File.ReadAllBytes(tempFilePath ?? FilePath)))
				{
					image.Rotate(90);
					currentImageBytes = image.ToByteArray(MagickFormat.Jpeg);
				}
				
				var editModel = new EditUndoModel(PreviewImage, null, null);
				imageUndoStack.Push(editModel);
				RefreshPreviewImageFromBytes(currentImageBytes);
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error rotating right: {ex.Message}");
				throw;
			}
		}

		public ICommand SkewCommand => skewCommand ?? (skewCommand = new DelegateCommand(StartSkew));
        public ICommand UndoCommand => undoCommand ?? (undoCommand = new DelegateCommand(UndoEdit));
        public ICommand NextImageCommand => nextImageCommand ?? (nextImageCommand = new DelegateCommand(SelectNextImage));
        public ICommand PreviousImageCommand => previousImageCommand ?? (previousImageCommand = new DelegateCommand(SelectPreviousImage));
        public ICommand ApplyCommand => applyCommand ?? (applyCommand = new DelegateCommand(ApplyCrop));
        public ICommand CancelCommand => cancelCommand ?? (cancelCommand = new DelegateCommand(CancelCrop));
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
                if (SetProperty(ref filePath, value))
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        GetImagePreview();
                        if (MetaDataViewModel != null)
                        {
                            MetaDataViewModel.CallingPage = PageKeys.SingleImage;
                            MetaDataViewModel.FilePath = value;
                        }
                    }
                }
            }
        }

        public string ApplyButtonText { get => applyButtonText; set => SetProperty(ref applyButtonText, value); }
        public string CancelButtonText { get => cancelButtonText; set => SetProperty(ref cancelButtonText, value); }

        public MetadataViewModel MetaDataViewModel { get => metadataViewModel; set => SetProperty(ref metadataViewModel, value); }
        public ImageSource PreviewImage
        {
            get => previewImage;
            set => SetProperty(ref previewImage, value);
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            SessionService.PeviousView = PageKeys.SingleImage;
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            FilePath = SessionService.CurrentImagePath;
            //var parameter = navigationContext.Parameters["FilePath"]?.ToString();
            //if (parameter != null)
            //{
            //    FilePath = parameter;
            //}
            //else
            //{
            //    GetThumbnailViewModel();
            //    FilePath = ThumbnailViewModel.SelectedImage?.FilePath;
            //}
            CheckThumbnailListPosition();
        }

        private void CheckThumbnailListPosition()
        {
            GetThumbnailViewModel();
            FilePath = ThumbnailViewModel.SelectedImage?.FilePath;
            NextImageEnabled = ThumbnailViewModel.Images?.IndexOf(ThumbnailViewModel.SelectedImage) < ThumbnailViewModel.Images?.Count - 1;
            PreviousImageEnabled = ThumbnailViewModel.Images?.IndexOf(ThumbnailViewModel.SelectedImage) > 0;
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
                SessionService.CurrentImagePath = FilePath;
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
                SessionService.CurrentImagePath = FilePath;
            }
            CheckThumbnailListPosition();
        }

        private void GetImagePreview()
        {
            try
            {
                currentImageBytes = File.ReadAllBytes(FilePath);
                RefreshPreviewImageFromBytes(currentImageBytes);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting image preview: {ex.Message}");
                throw;
            }
        }

        private void RefreshPreviewImageFromBytes(byte[] imageBytes)
        {
            try
            {
                // Dispose of the previous image if it exists
                if (PreviewImage is BitmapImage previousImage)
                {
                    previousImage.StreamSource?.Dispose();
                    previousImage.CacheOption = BitmapCacheOption.None;
                    previousImage.UriSource = null;
                }

                var bmi = new BitmapImage();
                using (var stream = new MemoryStream(imageBytes))
                {
                    bmi.BeginInit();
                    bmi.CacheOption = BitmapCacheOption.OnLoad;
                    bmi.StreamSource = stream;
                    bmi.EndInit();
                }
                PreviewImage = bmi;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error refreshing preview image: {ex.Message}");
                throw;
            }
        }

        private void ImageSelected()
        {
            //if (MetaDataViewModel == null)
            //{
            //    MetaDataViewModel = new MetadataViewModel(RegionManager, DialogService, PageKeys.SingleImage);
            //}
            //metadataViewModel.FilePath = FilePath;
            SessionService.CurrentImagePath = FilePath;
        }

        private Visibility applyCancelVisibility = Visibility.Hidden;
        public Visibility ApplyCancelVisibility
        {
            get => applyCancelVisibility;
            set => SetProperty(ref applyCancelVisibility, value);
        }

        private bool cropSelected;
        public bool CropSelected
        {
            get => cropSelected;
            set => SetProperty(ref cropSelected, value);
        }

        private CropManager cropper = new CropManager();
        private void StartCrop(FrameworkElement frameworkElement)
        {
            cropper.AddCropToElement(frameworkElement);
            ApplyCancelVisibility = Visibility.Visible;
            ApplyButtonText = "Apply Crop";
            CancelButtonText = "Cancel Crop";
            CropSelected = true;
        }

        private void ApplyCrop()
        {
            try
            {
                // Remove the crop UI first
                cropper.RemoveCropFromCur();
                ApplyCancelVisibility = Visibility.Hidden;
                
                using (var image = new MagickImage(currentImageBytes ?? File.ReadAllBytes(FilePath)))
                {
                    var rect = cropper.GetCropRect();
                    Debug.WriteLine($"Crop rectangle: X={rect.X}, Y={rect.Y}, Width={rect.Width}, Height={rect.Height}");
                    Debug.WriteLine($"Image dimensions: Width={image.Width}, Height={image.Height}");
                    
                    if (rect.Width <= 0 || rect.Height <= 0)
                    {
                        Debug.WriteLine("Invalid crop dimensions detected");
                        return;
                    }

                    // Get the actual image dimensions from the preview
                    var previewImage = PreviewImage as BitmapImage;
                    if (previewImage == null) return;

                    // Calculate the scale factors between preview and actual image
                    double scaleX = image.Width / previewImage.PixelWidth;
                    double scaleY = image.Height / previewImage.PixelHeight;

                    // Scale the crop rectangle to match the actual image dimensions
                    int scaledX = (int)(rect.X * scaleX);
                    int scaledY = (int)(rect.Y * scaleY);
                    int scaledWidth = (int)(rect.Width * scaleX);
                    int scaledHeight = (int)(rect.Height * scaleY);

                    Debug.WriteLine($"Scaled crop rectangle: X={scaledX}, Y={scaledY}, Width={scaledWidth}, Height={scaledHeight}");

                    IMagickGeometry magickGeometry = new MagickGeometry(scaledX, scaledY, scaledWidth, scaledHeight);
                    image.Crop(magickGeometry);
                    image.RePage();
                    currentImageBytes = image.ToByteArray(MagickFormat.Jpeg);
                    
                    var editModel = new EditUndoModel(PreviewImage, null, null);
                    imageUndoStack.Push(editModel);
                }
                CropSelected = false;
                RefreshPreviewImageFromBytes(currentImageBytes);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error applying crop: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private void CancelCrop()
        {
            cropper.RemoveCropFromCur();
            ApplyCancelVisibility = Visibility.Hidden;
            CropSelected = false;
        }

        private void UndoEdit()
        {
            if (imageUndoStack.Count > 0)
            {
                var previousState = imageUndoStack.Pop();
                if (previousState.ImageSource is BitmapImage previousImage)
                {
                    // Restore the original image bytes
                    currentImageBytes = File.ReadAllBytes(FilePath);
                    PreviewImage = previousImage;
                }
            }
        }

        public bool SkewLineVisible
        {
            get => skewLineVisible;
            set => SetProperty(ref skewLineVisible, value);
        }

        public bool IsInSkewMode
        {
            get => isInSkewMode;
            set => SetProperty(ref isInSkewMode, value);
        }

        public string SkewInstructions
        {
            get => skewInstructions;
            set => SetProperty(ref skewInstructions, value);
        }

        public Visibility SkewInstructionsVisibility
        {
            get => skewInstructionsVisibility;
            set => SetProperty(ref skewInstructionsVisibility, value);
        }

        private void StartSkew()
        {
            try
            {
                if (CurrentImageBytes == null) return;

                // Create a new line for skew correction
                skewLine = new Line
                {
                    Stroke = Brushes.Red,
                    StrokeThickness = 2,
                    Visibility = Visibility.Visible
                };

                // Add the line to the image container
                var imageContainer = GetImageContainer();
                if (imageContainer != null)
                {
                    imageContainer.Children.Add(skewLine);
                    SkewLineVisible = true;
                    IsInSkewMode = true;
                    SkewInstructionsVisibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error starting skew: {ex.Message}");
                var dialogParams = new DialogParameters
                {
                    { "message", $"Failed to start skew correction: {ex.Message}" }
                };
                DialogService.ShowDialog("ErrorDialog", dialogParams, null);
            }
        }

        private Panel GetImageContainer()
        {
            var singleImageView = RegionManager.Regions[Regions.Main].GetView(PageKeys.SingleImage) as SingleImagePage;
            return singleImageView?.FindName("ImageContainer") as Panel;
        }

        public void HandleSkewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!SkewLineVisible) return;

            isDrawingSkewLine = true;
            skewStartPoint = e.GetPosition(sender as IInputElement);
            skewLine.X1 = skewStartPoint.X;
            skewLine.Y1 = skewStartPoint.Y;
            skewLine.X2 = skewStartPoint.X;
            skewLine.Y2 = skewStartPoint.Y;
        }

        public void HandleSkewMouseMove(object sender, MouseEventArgs e)
        {
            if (!isDrawingSkewLine) return;

            var currentPoint = e.GetPosition(sender as IInputElement);
            skewLine.X2 = currentPoint.X;
            skewLine.Y2 = currentPoint.Y;
        }

        public void HandleSkewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!isDrawingSkewLine) return;

            isDrawingSkewLine = false;
            var endPoint = e.GetPosition(sender as IInputElement);
            
            // Calculate the angle between the line and the horizontal axis
            double deltaX = endPoint.X - skewStartPoint.X;
            double deltaY = endPoint.Y - skewStartPoint.Y;
            
            // Calculate the angle needed to make the line horizontal
            // atan2 gives us the angle from the horizontal axis (-180 to 180)
            double angle = -Math.Atan2(deltaY, deltaX) * 180 / Math.PI;

            // Apply the skew correction
            ApplySkewCorrection(angle);

            // Remove the line and reset skew mode
            var imageContainer = GetImageContainer();
            if (imageContainer != null)
            {
                imageContainer.Children.Remove(skewLine);
                SkewLineVisible = false;
                IsInSkewMode = false;
                SkewInstructionsVisibility = Visibility.Collapsed;
            }
        }

        private void ApplySkewCorrection(double angle)
        {
            try
            {
                using (var image = new MagickImage(currentImageBytes ?? File.ReadAllBytes(FilePath)))
                {
                    // Rotate the image to correct the skew
                    image.Rotate(angle);
                    currentImageBytes = image.ToByteArray(MagickFormat.Jpeg);
                    
                    var editModel = new EditUndoModel(PreviewImage, null, null);
                    imageUndoStack.Push(editModel);
                    RefreshPreviewImageFromBytes(currentImageBytes);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error applying skew correction: {ex.Message}");
                var dialogParams = new DialogParameters
                {
                    { "message", $"Failed to apply skew correction: {ex.Message}" }
                };
                DialogService.ShowDialog("ErrorDialog", dialogParams, null);
            }
        }
    }
}