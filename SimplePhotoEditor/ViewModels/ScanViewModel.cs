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
using DNTScanner.Core;
using System.Runtime.InteropServices;
using System.Linq;
using SimplePhotoEditor.Contracts.Services;

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
        private Visibility scanWarningVisibility = Visibility.Collapsed;

        public ScanViewModel(IRegionManager regionManager, IDialogService dialogService, ISessionService sessionService)
        {
            RegionManager = regionManager;
            DialogService = dialogService;
            SessionService = sessionService;
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

        private MetadataViewModel metadataViewModel;
        public MetadataViewModel MetaDataViewModel { get => metadataViewModel; set => SetProperty(ref metadataViewModel, value); }



        private DelegateCommand scanPreview;
        public ICommand ScanPreview => scanPreview ??= new DelegateCommand(PerformScanPreview);

        private void PerformScanPreview()
        {
        }

        private DelegateCommand scanFull;
        public ICommand ScanFull => scanFull ??= new DelegateCommand(PerformScanFull);

        private void PerformScanFull()
        {
            try
            {
                var scanners = SystemDevices.GetScannerDevices();
                if (scanners == null || scanners.Count == 0)
                {
                    Console.WriteLine("Please connect your scanner to the system and also make sure its driver is installed.");
                    return;
                }

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

                var selectedScanner = scanners.FirstOrDefault(s =>
                    s.ScannerDeviceSettings.TryGetValue("Name", out var scannerNameObj) &&
                    string.Equals(scannerNameObj?.ToString(), selectedScannerName, StringComparison.Ordinal));

                if (selectedScanner == null)
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

                var selectedDpi = Convert.ToInt32(App.Current.Properties[dpiKey].ToString());
                if (!selectedScanner.SupportedResolutions.Contains(selectedDpi))
                {
                    ShowScanWarning("The selected DPI is not supported by the current scanner. Choose another DPI in Settings.");
                    return;
                }

                Console.WriteLine($"Using scanner '{selectedScannerName}' with DPI {selectedDpi}");

                using var scannerDevice = new ScannerDevice(selectedScanner);
                scannerDevice.ScannerPictureSettings(config =>
                {
                    config.ColorFormat(ColorType.Color)
                          .Resolution(selectedDpi)
                          .Brightness(1)
                          .Contrast(1)
                          .StartPosition(left: 0, top: 0);
                });

                // If your scanner is a duplex or automatic document feeder, set these options
                scannerDevice.ScannerDeviceSettings(config =>
                {
                    // config.Source(DocumentSource.DoubleSided);
                    // ...
                });

                scannerDevice.PerformScan(WiaImageFormat.Jpeg);

                // An optional post processing of scanned images.
                // At least using its `Compress` method is recommended!
                scannerDevice.ProcessScannedImages(process =>
                {
                    process.ScaleByPixels(maximumWidth: 1000, maximumHeight: 1000, preserveAspectRatio: true)
                           .CropByPixels(left: 10, top: 10, right: 10, bottom: 10)
                           .RotateFlip(rotationAngle: 90, flipHorizontal: false, flipVertical: false)
                           .Compress(quality: 90);
                });

                var fileName = Path.Combine(Directory.GetCurrentDirectory(), "test.jpg");
                string firstSavedFilePath = null;
                foreach (var file in scannerDevice.SaveScannedImageFiles(fileName))
                {
                    Console.WriteLine($"Saved image file to: {file}");
                    if (string.IsNullOrWhiteSpace(firstSavedFilePath))
                    {
                        firstSavedFilePath = file;
                    }
                }

                // Or you can access the scanned images bytes
                foreach (var fileBytes in scannerDevice.ExtractScannedImageFiles())
                {
                    // You can convert them to Image objects
                    // var img = Image.FromStream(new MemoryStream(fileBytes));
                    Console.WriteLine($"fileBytes len: {fileBytes.Length}");
                    File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "test2.jpg"), fileBytes);
                }

                if (!string.IsNullOrWhiteSpace(firstSavedFilePath) && File.Exists(firstSavedFilePath))
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.UriSource = new Uri(firstSavedFilePath, UriKind.Absolute);
                    image.EndInit();
                    image.Freeze();

                    PreviewImage = image;
                    FileName = Path.GetFileName(firstSavedFilePath);
                    ScanWarningMessage = string.Empty;
                    ScanWarningVisibility = Visibility.Collapsed;
                }
            }
            catch (COMException ex)
            {
                var friendlyErrorMessage = ex.GetComErrorMessage(); // How to show a better error message to users
                Console.WriteLine(friendlyErrorMessage);
                Console.WriteLine(ex);
            }
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
    }
}
