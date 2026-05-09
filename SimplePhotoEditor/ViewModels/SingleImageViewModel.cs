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
using SimplePhotoEditor.Core.Services;
using System.Diagnostics;

namespace SimplePhotoEditor.ViewModels
{
    public class SingleImageViewModel : BindableBase, INavigationAware
    {
        private const string AutoCropStrengthKey = "AutoCropStrength";
        private ICommand autoCropCommand;
        private ICommand cropCommand;
        private ICommand rotateLeftCommand;
        private ICommand rotateRightCommand;
        private ICommand skewCommand;
        private ICommand cancelSkewCommand;
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
        private Visibility skewCancelVisibility = Visibility.Collapsed;

        public byte[] CurrentImageBytes => currentImageBytes;

        public SingleImageViewModel(IRegionManager regionManager, IDialogService dialogService, ISessionService sessionService, MetadataViewModel metadataViewModel)
        {
            DialogService = dialogService;
            RegionManager = regionManager;
            SessionService = sessionService;
            MetaDataViewModel = metadataViewModel;
        }

        public ICommand AutoCropCommand => autoCropCommand ?? (autoCropCommand = new DelegateCommand(AutoCrop));

        private void PushUndoState()
        {
            if (currentImageBytes == null)
            {
                return;
            }

            var previousBytes = (byte[])currentImageBytes.Clone();
            imageUndoStack.Push(new EditUndoModel(PreviewImage, previousBytes, null, null));
        }

        private MagickFormat GetOutputFormat()
        {
            var extension = System.IO.Path.GetExtension(FilePath)?.ToLowerInvariant();
            return extension switch
            {
                ".png" => MagickFormat.Png,
                ".bmp" => MagickFormat.Bmp,
                ".gif" => MagickFormat.Gif,
                ".tif" => MagickFormat.Tiff,
                ".tiff" => MagickFormat.Tiff,
                _ => MagickFormat.Jpeg
            };
        }

        private void ShowError(string message, Exception ex = null)
        {
            var fullMessage = ex == null ? message : $"{message}: {ex.Message}";
            Debug.WriteLine(fullMessage);
            var dialogParams = new DialogParameters
            {
                { "message", fullMessage }
            };
            DialogService.ShowDialog("ErrorDialog", dialogParams, null);
        }

        private void AutoCrop()
        {
            try
            {
                PushUndoState();
                var sourceBytes = currentImageBytes ?? File.ReadAllBytes(FilePath);
                currentImageBytes = MagickImageTransforms.AutoTrim(
                    sourceBytes,
                    GetAutoCropFuzzPercent(),
                    GetOutputFormat());

                RefreshPreviewImageFromBytes(currentImageBytes);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in auto-crop: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                ShowError("Auto-crop failed", ex);
            }
        }

        private double GetAutoCropFuzzPercent()
        {
            var strength = App.Current.Properties.Contains(AutoCropStrengthKey)
                ? App.Current.Properties[AutoCropStrengthKey]?.ToString()
                : "Medium";

            return AutoCropFuzzResolver.GetFuzzPercentFromStrengthLabel(strength);
        }

        public ICommand CropCommand => cropCommand ?? (cropCommand = new DelegateCommand<FrameworkElement>(StartCrop));
        public ICommand RotateLeftCommand => rotateLeftCommand ?? (rotateLeftCommand = new DelegateCommand(RotateLeft));

		private void RotateLeft()
		{
			try
			{
                PushUndoState();
                var sourceBytes = currentImageBytes ?? File.ReadAllBytes(tempFilePath ?? FilePath);
                currentImageBytes = MagickImageTransforms.Rotate(sourceBytes, -90, GetOutputFormat());

                RefreshPreviewImageFromBytes(currentImageBytes);
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error rotating left: {ex.Message}");
                ShowError("Rotate left failed", ex);
			}
		}

		public ICommand RotateRightCommand => rotateRightCommand ?? (rotateRightCommand = new DelegateCommand(RotateRight));

		private void RotateRight()
		{
			try
			{
                PushUndoState();
                var sourceBytes = currentImageBytes ?? File.ReadAllBytes(tempFilePath ?? FilePath);
                currentImageBytes = MagickImageTransforms.Rotate(sourceBytes, 90, GetOutputFormat());

                RefreshPreviewImageFromBytes(currentImageBytes);
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error rotating right: {ex.Message}");
                ShowError("Rotate right failed", ex);
			}
		}

		public ICommand SkewCommand => skewCommand ?? (skewCommand = new DelegateCommand(StartSkew));
        public ICommand CancelSkewCommand => cancelSkewCommand ?? (cancelSkewCommand = new DelegateCommand(CancelSkew));
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
            var thumbnailView = RegionManager?.Regions[Regions.Main]?.GetView(PageKeys.Thumbnail) as ThumbnailPage;
            ThumbnailViewModel = thumbnailView?.DataContext as ThumbnailViewModel;
        }

        private bool previousImageEnabled = true;
        private bool nextImageEnabled = true;

        public bool PreviousImageEnabled { get => previousImageEnabled; set => SetProperty(ref previousImageEnabled, value); }
        public bool NextImageEnabled { get => nextImageEnabled; set => SetProperty(ref nextImageEnabled, value); }
        public void SelectNextImage()
        {
            GetThumbnailViewModel();
            if (ThumbnailViewModel?.Images == null || ThumbnailViewModel.SelectedImage == null)
            {
                return;
            }

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
            if (ThumbnailViewModel?.Images == null || ThumbnailViewModel.SelectedImage == null)
            {
                return;
            }

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
                ShowError("Unable to load image preview", ex);
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
                ShowError("Unable to refresh image preview", ex);
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
        private FrameworkElement cropTargetElement;
        private void StartCrop(FrameworkElement frameworkElement)
        {
            if (CurrentImageBytes == null || frameworkElement == null)
            {
                return;
            }

            cropTargetElement = frameworkElement;
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
                PushUndoState();
                var rect = cropper.GetCropRect();

                if (rect.Width <= 0 || rect.Height <= 0)
                {
                    Debug.WriteLine("Invalid crop dimensions detected");
                    return;
                }

                if (cropTargetElement == null || PreviewImage == null ||
                    cropTargetElement.ActualWidth <= 0 || cropTargetElement.ActualHeight <= 0)
                {
                    return;
                }

                var previewBitmap = PreviewImage as BitmapImage;
                if (previewBitmap == null || previewBitmap.PixelWidth <= 0 || previewBitmap.PixelHeight <= 0)
                {
                    return;
                }

                var sourceBytes = currentImageBytes ?? File.ReadAllBytes(FilePath);
                if (!CropPixelGeometryMapper.TryMapUiCropRectangleToPixelGeometry(
                        rect.X,
                        rect.Y,
                        rect.Width,
                        rect.Height,
                        cropTargetElement.ActualWidth,
                        cropTargetElement.ActualHeight,
                        previewBitmap.PixelWidth,
                        previewBitmap.PixelHeight,
                        out var magickGeometry))
                {
                    return;
                }

                Debug.WriteLine(
                    $"Scaled crop rectangle: X={magickGeometry.X}, Y={magickGeometry.Y}, Width={magickGeometry.Width}, Height={magickGeometry.Height}");

                currentImageBytes = MagickImageTransforms.Crop(sourceBytes, magickGeometry, GetOutputFormat());

                // Now remove the crop UI after we've read the rectangle
                cropper.RemoveCropFromCur();
                ApplyCancelVisibility = Visibility.Hidden;
                cropTargetElement = null;
                CropSelected = false;
                RefreshPreviewImageFromBytes(currentImageBytes);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error applying crop: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                ShowError("Apply crop failed", ex);
            }
        }

        private void CancelCrop()
        {
            cropper.RemoveCropFromCur();
            ApplyCancelVisibility = Visibility.Hidden;
            CropSelected = false;
        }

        public void CancelActiveEdits()
        {
            if (IsInSkewMode)
            {
                CancelSkew();
            }
        }

        public void CancelCropOrSkew()
        {
            if (IsInSkewMode)
            {
                CancelSkew();
            }

            if (CropSelected || ApplyCancelVisibility == Visibility.Visible)
            {
                CancelCrop();
            }
        }

        private void UndoEdit()
        {
            if (imageUndoStack.Count > 0)
            {
                var previousState = imageUndoStack.Pop();
                if (previousState?.ImageBytes != null)
                {
                    currentImageBytes = previousState.ImageBytes;
                    RefreshPreviewImageFromBytes(currentImageBytes);
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
            set
            {
                if (SetProperty(ref isInSkewMode, value))
                {
                    SkewCancelVisibility = value ? Visibility.Visible : Visibility.Collapsed;
                }
            }
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

        public Visibility SkewCancelVisibility
        {
            get => skewCancelVisibility;
            set => SetProperty(ref skewCancelVisibility, value);
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

            ResetSkewMode();
        }

        private void CancelSkew()
        {
            isDrawingSkewLine = false;
            ResetSkewMode();
        }

        private void ResetSkewMode()
        {
            var imageContainer = GetImageContainer();
            if (imageContainer != null && skewLine != null)
            {
                imageContainer.Children.Remove(skewLine);
            }

            skewLine = null;
            SkewLineVisible = false;
            IsInSkewMode = false;
            SkewInstructionsVisibility = Visibility.Collapsed;
        }

        private void ApplySkewCorrection(double angle)
        {
            try
            {
                PushUndoState();
                var sourceBytes = currentImageBytes ?? File.ReadAllBytes(FilePath);
                currentImageBytes = MagickImageTransforms.Rotate(sourceBytes, angle, GetOutputFormat());
                RefreshPreviewImageFromBytes(currentImageBytes);
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