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
using System.Windows.Controls;
using DNTScanner.Core;
using System.Runtime.InteropServices;
using System.Linq;
using SimplePhotoEditor.Contracts.Services;
using ImageMagick;
using System.Threading;
using System.Threading.Tasks;

namespace SimplePhotoEditor.ViewModels
{
    public class ScanViewModel : BindableBase, INavigationAware
    {
        private const string LastSelectedScannerNameKey = "LastSelectedScannerName";
        private const string LastSelectedDpiKeyPrefix = "LastSelectedDpi::";
        private IRegionManager RegionManager;
        private IDialogService DialogService;
        private ISessionService SessionService;
        private string scanWarningMessage;
        private string fileName;
        private BitmapImage previewImage;
        private byte[] currentImageBytes;
        private Stack<byte[]> imageUndoStack = new Stack<byte[]>();
        private Visibility scanWarningVisibility = Visibility.Collapsed;
        private Visibility scanningOverlayVisibility = Visibility.Collapsed;
        private bool isScanning;
        private Visibility applyCancelVisibility = Visibility.Collapsed;
        private bool cropSelected;
        private DelegateCommand autoCropCommand;
        private DelegateCommand rotateLeftCommand;
        private DelegateCommand rotateRightCommand;
        private DelegateCommand undoCommand;
        private DelegateCommand<FrameworkElement> cropCommand;
        private DelegateCommand applyCommand;
        private DelegateCommand cancelCommand;
        private DelegateCommand skewCommand;
        private bool isDrawingSkewLine;
        private Point skewStartPoint;
        private System.Windows.Shapes.Line skewLine;
        private bool isInSkewMode;
        private string skewInstructions = "Draw a line along what should be horizontal";
        private Visibility skewInstructionsVisibility = Visibility.Collapsed;
        private FrameworkElement cropTargetElement;
        private readonly CropManager cropper = new CropManager();

        public ScanViewModel(IRegionManager regionManager, IDialogService dialogService, ISessionService sessionService, MetadataViewModel metadataViewModel)
        {
            RegionManager = regionManager;
            DialogService = dialogService;
            SessionService = sessionService;
            MetaDataViewModel = metadataViewModel;
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            SessionService.PeviousView = PageKeys.Scan;
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            var parameter = navigationContext.Parameters["FilePath"]?.ToString();
            if (parameter != null)
            {
                //FilePath = parameter;
            }
            else
            {
                //GetThumbnailViewModel();
                //FilePath = ThumbnailViewModel.SelectedImage?.FilePath;
            }
            if (MetaDataViewModel != null)
            {
                MetaDataViewModel.CallingPage = PageKeys.Scan;
                MetaDataViewModel.ClearMetadataForScan();
            }
            //CheckThumbnailListPosition();
            ValidateScannerSelection();
        }

        public string ScanWarningMessage
        {
            get => scanWarningMessage;
            set => SetProperty(ref scanWarningMessage, value);
        }

        public string FileName
        {
            get => fileName;
            set => SetProperty(ref fileName, value);
        }

        public BitmapImage PreviewImage
        {
            get => previewImage;
            set => SetProperty(ref previewImage, value);
        }

        public Visibility ScanWarningVisibility
        {
            get => scanWarningVisibility;
            set => SetProperty(ref scanWarningVisibility, value);
        }

        public Visibility ScanningOverlayVisibility
        {
            get => scanningOverlayVisibility;
            set => SetProperty(ref scanningOverlayVisibility, value);
        }

        public bool IsScanning
        {
            get => isScanning;
            set => SetProperty(ref isScanning, value);
        }

        public Visibility ApplyCancelVisibility
        {
            get => applyCancelVisibility;
            set => SetProperty(ref applyCancelVisibility, value);
        }

        public bool CropSelected
        {
            get => cropSelected;
            set => SetProperty(ref cropSelected, value);
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

        private MetadataViewModel metadataViewModel;
        public MetadataViewModel MetaDataViewModel { get => metadataViewModel; set => SetProperty(ref metadataViewModel, value); }



        private DelegateCommand scanPreview;
        public ICommand ScanPreview => scanPreview ??= new DelegateCommand(PerformScanPreview);
        public ICommand AutoCropCommand => autoCropCommand ??= new DelegateCommand(AutoCrop);
        public ICommand RotateLeftCommand => rotateLeftCommand ??= new DelegateCommand(RotateLeft);
        public ICommand RotateRightCommand => rotateRightCommand ??= new DelegateCommand(RotateRight);
        public ICommand UndoCommand => undoCommand ??= new DelegateCommand(UndoEdit);
        public ICommand CropCommand => cropCommand ??= new DelegateCommand<FrameworkElement>(StartCrop);
        public ICommand ApplyCommand => applyCommand ??= new DelegateCommand(ApplyCrop);
        public ICommand CancelCommand => cancelCommand ??= new DelegateCommand(CancelCrop);
        public ICommand SkewCommand => skewCommand ??= new DelegateCommand(StartSkew);

        private async void PerformScanPreview()
        {
            await PerformScan(isPreview: true);
        }

        private DelegateCommand scanFull;
        public ICommand ScanFull => scanFull ??= new DelegateCommand(PerformScanFull);

        private async void PerformScanFull()
        {
            await PerformScan(isPreview: false);
        }

        private async Task PerformScan(bool isPreview)
        {
            if (IsScanning)
            {
                return;
            }

            try
            {
                IsScanning = true;
                ScanningOverlayVisibility = Visibility.Visible;
                ScanWarningMessage = string.Empty;
                ScanWarningVisibility = Visibility.Collapsed;

                var scanResult = await RunScanOnStaThreadAsync(isPreview);

                if (!string.IsNullOrWhiteSpace(scanResult) && File.Exists(scanResult))
                {
                    currentImageBytes = File.ReadAllBytes(scanResult);
                    imageUndoStack.Clear();
                    RefreshPreviewImageFromBytes(currentImageBytes);
                    FileName = Path.GetFileName(scanResult);
                    if (!isPreview && MetaDataViewModel != null)
                    {
                        MetaDataViewModel.CallingPage = PageKeys.Scan;
                        MetaDataViewModel.FilePath = scanResult;
                    }
                    ScanWarningMessage = string.Empty;
                    ScanWarningVisibility = Visibility.Collapsed;
                }
            }
            catch (InvalidOperationException ex)
            {
                ShowScanWarning(ex.Message);
            }
            catch (COMException ex)
            {
                var friendlyErrorMessage = ex.GetComErrorMessage(); // How to show a better error message to users
                Console.WriteLine(friendlyErrorMessage);
                Console.WriteLine(ex);
                ShowScanWarning(friendlyErrorMessage);
            }
            finally
            {
                IsScanning = false;
                ScanningOverlayVisibility = Visibility.Collapsed;
            }
        }

        private Task<string> RunScanOnStaThreadAsync(bool isPreview)
        {
            var tcs = new TaskCompletionSource<string>();

            var thread = new Thread(() =>
            {
                try
                {
                    var scanners = SystemDevices.GetScannerDevices();
                    if (scanners == null || scanners.Count == 0)
                    {
                        throw new InvalidOperationException("Please connect your scanner to the system and make sure its driver is installed.");
                    }

                    if (!App.Current.Properties.Contains(LastSelectedScannerNameKey))
                    {
                        throw new InvalidOperationException("No scanner is selected. Choose a scanner and DPI in Settings before scanning.");
                    }

                    var selectedScannerName = App.Current.Properties[LastSelectedScannerNameKey]?.ToString();
                    if (string.IsNullOrWhiteSpace(selectedScannerName))
                    {
                        throw new InvalidOperationException("No scanner is selected. Choose a scanner and DPI in Settings before scanning.");
                    }

                    var selectedScanner = scanners.FirstOrDefault(s =>
                        s.ScannerDeviceSettings.TryGetValue("Name", out var scannerNameObj) &&
                        string.Equals(scannerNameObj?.ToString(), selectedScannerName, StringComparison.Ordinal));

                    if (selectedScanner == null)
                    {
                        throw new InvalidOperationException("The selected scanner is unavailable. Re-select an available scanner in Settings.");
                    }

                    var dpiKey = $"{LastSelectedDpiKeyPrefix}{selectedScannerName}";
                    if (!App.Current.Properties.Contains(dpiKey))
                    {
                        throw new InvalidOperationException("No DPI is selected for the current scanner. Choose a DPI in Settings.");
                    }

                    var selectedDpi = Convert.ToInt32(App.Current.Properties[dpiKey].ToString());
                    if (!selectedScanner.SupportedResolutions.Contains(selectedDpi))
                    {
                        throw new InvalidOperationException("The selected DPI is not supported by the current scanner. Choose another DPI in Settings.");
                    }

                    var requestedDpi = selectedDpi;
                    if (isPreview)
                    {
                        var supported = selectedScanner.SupportedResolutions?.Distinct().OrderBy(r => r).ToList() ?? new List<int>();
                        var targetPreviewDpi = 75;
                        var previewDpi = supported.FirstOrDefault(r => r >= targetPreviewDpi);
                        if (previewDpi <= 0 && supported.Count > 0)
                        {
                            previewDpi = supported.First();
                        }

                        if (previewDpi > 0)
                        {
                            requestedDpi = previewDpi;
                        }
                    }

                    using var scannerDevice = new ScannerDevice(selectedScanner);
                    scannerDevice.ScannerPictureSettings(config =>
                    {
                        config.ColorFormat(DNTScanner.Core.ColorType.Color)
                              .Resolution(requestedDpi)
                              .Brightness(1)
                              .Contrast(1)
                              .StartPosition(left: 0, top: 0);
                    });

                    scannerDevice.ScannerDeviceSettings(config =>
                    {
                    });

                    scannerDevice.PerformScan(WiaImageFormat.Jpeg);

                    scannerDevice.ProcessScannedImages(process =>
                    {
                        process.ScaleByPixels(maximumWidth: 1000, maximumHeight: 1000, preserveAspectRatio: true)
                               .CropByPixels(left: 10, top: 10, right: 10, bottom: 10)
                               .RotateFlip(rotationAngle: 90, flipHorizontal: false, flipVertical: false)
                               .Compress(quality: 90);
                    });

                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    var prefix = isPreview ? "Preview" : "Scan";
                    var fileName = Path.Combine(Directory.GetCurrentDirectory(), $"{prefix}_{timestamp}.jpg");
                    string firstSavedFilePath = null;
                    foreach (var file in scannerDevice.SaveScannedImageFiles(fileName))
                    {
                        if (string.IsNullOrWhiteSpace(firstSavedFilePath))
                        {
                            firstSavedFilePath = file;
                        }
                    }

                    foreach (var fileBytes in scannerDevice.ExtractScannedImageFiles())
                    {
                        File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), $"{prefix}_{timestamp}_raw.jpg"), fileBytes);
                    }

                    tcs.SetResult(firstSavedFilePath);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();

            return tcs.Task;
        }

        private void ValidateScannerSelection()
        {
            ScanWarningMessage = string.Empty;
            ScanWarningVisibility = Visibility.Collapsed;

            if (!App.Current.Properties.Contains(LastSelectedScannerNameKey))
            {
                ShowScanWarning("No scanner is selected. Choose a scanner and DPI in Settings before scanning.");
                return;
            }

            var selectedScannerName = App.Current.Properties[LastSelectedScannerNameKey]?.ToString();
            if (string.IsNullOrWhiteSpace(selectedScannerName))
            {
                ShowScanWarning("No scanner is selected. Choose a scanner and DPI in Settings before scanning.");
                return;
            }

            var scanners = SystemDevices.GetScannerDevices();
            var scanner = scanners.FirstOrDefault(s =>
                s.ScannerDeviceSettings.TryGetValue("Name", out var scannerNameObj) &&
                string.Equals(scannerNameObj?.ToString(), selectedScannerName, StringComparison.Ordinal));

            if (scanner == null)
            {
                ShowScanWarning("The selected scanner is unavailable. Re-select an available scanner in Settings.");
                return;
            }

            var dpiKey = $"{LastSelectedDpiKeyPrefix}{selectedScannerName}";
            if (!App.Current.Properties.Contains(dpiKey))
            {
                ShowScanWarning("No DPI is selected for the current scanner. Choose a DPI in Settings.");
                return;
            }

            var savedDpi = Convert.ToInt32(App.Current.Properties[dpiKey].ToString());
            if (!scanner.SupportedResolutions.Contains(savedDpi))
            {
                ShowScanWarning("The selected DPI is not supported by the current scanner. Choose another DPI in Settings.");
            }
        }

        private void ShowScanWarning(string message)
        {
            ScanWarningMessage = message;
            ScanWarningVisibility = Visibility.Visible;
        }

        private void PushUndoState()
        {
            if (currentImageBytes == null)
            {
                return;
            }

            imageUndoStack.Push((byte[])currentImageBytes.Clone());
        }

        private void RefreshPreviewImageFromBytes(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0)
            {
                return;
            }

            var image = new BitmapImage();
            using (var stream = new MemoryStream(imageBytes))
            {
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.EndInit();
                image.Freeze();
            }

            PreviewImage = image;
        }

        private void AutoCrop()
        {
            if (currentImageBytes == null)
            {
                return;
            }

            PushUndoState();
            using var image = new MagickImage(currentImageBytes);
            image.Trim();
            image.ResetPage();
            currentImageBytes = image.ToByteArray(MagickFormat.Jpeg);
            RefreshPreviewImageFromBytes(currentImageBytes);
        }

        private void RotateLeft()
        {
            if (currentImageBytes == null)
            {
                return;
            }

            PushUndoState();
            using var image = new MagickImage(currentImageBytes);
            image.Rotate(-90);
            currentImageBytes = image.ToByteArray(MagickFormat.Jpeg);
            RefreshPreviewImageFromBytes(currentImageBytes);
        }

        private void RotateRight()
        {
            if (currentImageBytes == null)
            {
                return;
            }

            PushUndoState();
            using var image = new MagickImage(currentImageBytes);
            image.Rotate(90);
            currentImageBytes = image.ToByteArray(MagickFormat.Jpeg);
            RefreshPreviewImageFromBytes(currentImageBytes);
        }

        private void UndoEdit()
        {
            if (imageUndoStack.Count <= 0)
            {
                return;
            }

            currentImageBytes = imageUndoStack.Pop();
            RefreshPreviewImageFromBytes(currentImageBytes);
        }

        private void StartCrop(FrameworkElement frameworkElement)
        {
            if (currentImageBytes == null || frameworkElement == null)
            {
                return;
            }

            cropTargetElement = frameworkElement;
            cropper.AddCropToElement(frameworkElement);
            ApplyCancelVisibility = Visibility.Visible;
            CropSelected = true;
        }

        private void ApplyCrop()
        {
            if (currentImageBytes == null)
            {
                return;
            }

            try
            {
                PushUndoState();
                var rect = cropper.GetCropRect();
                cropper.RemoveCropFromCur();
                ApplyCancelVisibility = Visibility.Collapsed;
                CropSelected = false;

                if (rect.Width <= 0 || rect.Height <= 0)
                {
                    return;
                }

                if (cropTargetElement == null || PreviewImage == null ||
                    cropTargetElement.ActualWidth <= 0 || cropTargetElement.ActualHeight <= 0 ||
                    PreviewImage.PixelWidth <= 0 || PreviewImage.PixelHeight <= 0)
                {
                    return;
                }

                using var image = new MagickImage(currentImageBytes);
                if (rect.Width <= 0 || rect.Height <= 0)
                {
                    return;
                }

                var controlWidth = cropTargetElement.ActualWidth;
                var controlHeight = cropTargetElement.ActualHeight;
                var imageWidth = PreviewImage.PixelWidth;
                var imageHeight = PreviewImage.PixelHeight;

                var uniformScale = Math.Min(controlWidth / imageWidth, controlHeight / imageHeight);
                var displayedWidth = imageWidth * uniformScale;
                var displayedHeight = imageHeight * uniformScale;
                var offsetX = (controlWidth - displayedWidth) / 2.0;
                var offsetY = (controlHeight - displayedHeight) / 2.0;

                var cropX1 = Math.Max(rect.X, (int)offsetX);
                var cropY1 = Math.Max(rect.Y, (int)offsetY);
                var cropX2 = Math.Min(rect.X + rect.Width, (int)(offsetX + displayedWidth));
                var cropY2 = Math.Min(rect.Y + rect.Height, (int)(offsetY + displayedHeight));

                var normalizedWidth = cropX2 - cropX1;
                var normalizedHeight = cropY2 - cropY1;
                if (normalizedWidth <= 0 || normalizedHeight <= 0)
                {
                    return;
                }

                var pixelX = (int)Math.Round((cropX1 - offsetX) / uniformScale);
                var pixelY = (int)Math.Round((cropY1 - offsetY) / uniformScale);
                var pixelWidth = (int)Math.Round(normalizedWidth / uniformScale);
                var pixelHeight = (int)Math.Round(normalizedHeight / uniformScale);

                if (pixelWidth <= 0 || pixelHeight <= 0)
                {
                    return;
                }

                var geometry = new MagickGeometry(pixelX, pixelY, (uint)pixelWidth, (uint)pixelHeight);
                image.Crop(geometry);
                image.ResetPage();
                currentImageBytes = image.ToByteArray(MagickFormat.Jpeg);
                RefreshPreviewImageFromBytes(currentImageBytes);
            }
            catch
            {
                CancelCrop();
            }
        }

        private void CancelCrop()
        {
            if (CropSelected || ApplyCancelVisibility == Visibility.Visible)
            {
                cropper.RemoveCropFromCur();
            }
            cropTargetElement = null;
            ApplyCancelVisibility = Visibility.Collapsed;
            CropSelected = false;
        }

        public void CancelCropOrSkew()
        {
            if (isInSkewMode)
            {
                CancelSkew();
            }

            if (CropSelected || ApplyCancelVisibility == Visibility.Visible)
            {
                CancelCrop();
            }
        }

        public void CancelActiveEdits()
        {
            if (isInSkewMode)
            {
                CancelSkew();
            }
        }

        private void CancelSkew()
        {
            isDrawingSkewLine = false;
            if (skewLine != null && skewLine.Parent is Panel parentPanel)
            {
                parentPanel.Children.Remove(skewLine);
            }

            skewLine = null;
            isInSkewMode = false;
            SkewInstructionsVisibility = Visibility.Collapsed;
        }

        private void StartSkew()
        {
            if (currentImageBytes == null || isInSkewMode)
            {
                return;
            }

            isInSkewMode = true;
            CancelCrop();
            SkewInstructionsVisibility = Visibility.Visible;
        }

        public void HandleSkewMouseDown(IInputElement inputElement, Panel imageContainer, MouseButtonEventArgs e)
        {
            if (!isInSkewMode || inputElement == null || imageContainer == null)
            {
                return;
            }

            if (skewLine == null)
            {
                skewLine = new System.Windows.Shapes.Line
                {
                    Stroke = Brushes.Red,
                    StrokeThickness = 2
                };
                imageContainer.Children.Add(skewLine);
            }

            isDrawingSkewLine = true;
            skewStartPoint = e.GetPosition(inputElement);
            skewLine.X1 = skewStartPoint.X;
            skewLine.Y1 = skewStartPoint.Y;
            skewLine.X2 = skewStartPoint.X;
            skewLine.Y2 = skewStartPoint.Y;
        }

        public void HandleSkewMouseMove(IInputElement inputElement, MouseEventArgs e)
        {
            if (!isInSkewMode || !isDrawingSkewLine || skewLine == null || inputElement == null)
            {
                return;
            }

            var currentPoint = e.GetPosition(inputElement);
            skewLine.X2 = currentPoint.X;
            skewLine.Y2 = currentPoint.Y;
        }

        public void HandleSkewMouseUp(IInputElement inputElement, Panel imageContainer, MouseButtonEventArgs e)
        {
            if (!isInSkewMode || !isDrawingSkewLine || inputElement == null)
            {
                return;
            }

            isDrawingSkewLine = false;
            var endPoint = e.GetPosition(inputElement);
            double deltaX = endPoint.X - skewStartPoint.X;
            double deltaY = endPoint.Y - skewStartPoint.Y;
            double angle = -Math.Atan2(deltaY, deltaX) * 180 / Math.PI;

            PushUndoState();
            using (var image = new MagickImage(currentImageBytes))
            {
                image.Rotate(angle);
                currentImageBytes = image.ToByteArray(MagickFormat.Jpeg);
            }
            RefreshPreviewImageFromBytes(currentImageBytes);

            if (imageContainer != null && skewLine != null)
            {
                imageContainer.Children.Remove(skewLine);
            }

            CancelSkew();
        }
    }
}
