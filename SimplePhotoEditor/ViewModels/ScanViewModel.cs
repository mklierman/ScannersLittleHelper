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

namespace SimplePhotoEditor.ViewModels
{
    public class ScanViewModel : BindableBase, INavigationAware
    {
        private IRegionManager RegionManager;
        private IDialogService DialogService;
        public ScanViewModel(IRegionManager regionManager, IDialogService dialogService)
        {
            RegionManager = regionManager;
            DialogService = dialogService;
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
                //FilePath = parameter;
            }
            else
            {
                //GetThumbnailViewModel();
                //FilePath = ThumbnailViewModel.SelectedImage?.FilePath;
            }
            //CheckThumbnailListPosition();
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
                // Finding the first connected scanner to the system
                var scanners = SystemDevices.GetScannerDevices();
                var firstScanner = scanners.FirstOrDefault();
                if (firstScanner == null)
                {
                    Console.WriteLine("Please connect your scanner to the system and also make sure its driver is installed.");
                    return;
                }
                Console.WriteLine($"Using {firstScanner}");

                using (var scannerDevice = new ScannerDevice(firstScanner))
                {
                    scannerDevice.ScannerPictureSettings(config =>
                    {
                        config.ColorFormat(ColorType.Color)
                              // Optional settings
                              .Resolution(200)
                              .Brightness(1)
                              .Contrast(1)
                              .StartPosition(left: 0, top: 0)
                              //.Extent(width: 1250 * dpi, height: 1700 * dpi)
                              ;
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
                    foreach (var file in scannerDevice.SaveScannedImageFiles(fileName))
                    {
                        Console.WriteLine($"Saved image file to: {file}");
                    }

                    // Or you can access the scanned images bytes
                    foreach (var fileBytes in scannerDevice.ExtractScannedImageFiles())
                    {
                        // You can convert them to Image objects
                        // var img = Image.FromStream(new MemoryStream(fileBytes));
                        Console.WriteLine($"fileBytes len: {fileBytes.Length}");
                        File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "test2.jpg"), fileBytes);
                    }
                }
            }
            catch (COMException ex)
            {
                var friendlyErrorMessage = ex.GetComErrorMessage(); // How to show a better error message to users
                Console.WriteLine(friendlyErrorMessage);
                Console.WriteLine(ex);
            }
        }
    }
}
