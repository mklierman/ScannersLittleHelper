using ImageProcessor;
using ImageProcessor.Imaging;
using SimplePhotoEditor.ViewModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Diagnostics;

namespace SimplePhotoEditor.Managers
{
    public class CropManager
    {
        private CroppingAdorner.CroppingAdorner cropAdorner;
        private FrameworkElement currentFrameworkElement = null;
        private Rect currentCropRect;
        private System.Drawing.Brush originalBrush;
        public void AddCropToElement(FrameworkElement frameworkElement)
        {
            if (currentFrameworkElement != null)
            {
                RemoveCropFromCur();
            }
            
            // Start with a smaller initial crop box in the center
            Rect interiorRectangle = new Rect(
                frameworkElement.ActualWidth * 0.4,
                frameworkElement.ActualHeight * 0.4,
                frameworkElement.ActualWidth * 0.2,
                frameworkElement.ActualHeight * 0.2);

            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(frameworkElement);
            cropAdorner = new CroppingAdorner.CroppingAdorner(frameworkElement, interiorRectangle);
            adornerLayer.Add(cropAdorner);
            cropAdorner.CropChanged += CropChanged;
            currentFrameworkElement = frameworkElement;
        }

        public void RemoveCropFromCur()
        {
            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(currentFrameworkElement);
            adornerLayer.Remove(cropAdorner);
        }

        private void RefreshCropImage()
        {
            if (cropAdorner != null)
            {
                Rect rc = cropAdorner.ClippingRectangle;
                ImageSource src = (ImageSource)currentFrameworkElement.DataContext;
                var widthRatio = src.Width / currentFrameworkElement.ActualWidth;
                var heightRatio = src.Height / currentFrameworkElement.ActualHeight;
                rc.Width *= widthRatio;
                rc.Height *= heightRatio;

            }
        }

        private void CropChanged(Object sender, RoutedEventArgs args)
        {
            if (cropAdorner != null)
            {
                //Rect rc = cropAdorner.ClippingRectangle;
                //var src = ((SingleImageViewModel)currentFrameworkElement.DataContext).PreviewImage;
                //var widthRatio = src.Width / currentFrameworkElement.ActualWidth;
                //var heightRatio = src.Height / currentFrameworkElement.ActualHeight;
                //rc.Width *= widthRatio;
                //rc.Height *= heightRatio;

                //currentCropRect = rc;
            }
        }

        public CropLayer ApplyCrop()
        {
            Rect rc = cropAdorner.GetCropRect();
            var src = ((SingleImageViewModel)currentFrameworkElement.DataContext).PreviewImage;
            RemoveCropFromCur();
            var extraWidth = src.Width - currentFrameworkElement.ActualWidth;
            var extraHeight = src.Height - currentFrameworkElement.ActualHeight;

            currentCropRect = rc;

            //ImageProcessor.ImageFactory imageFactory = new ImageFactory();
            //imageFactory.Load(path);
            return new CropLayer((float)currentCropRect.Left, (float)currentCropRect.Top, (float)currentCropRect.Right, (float)currentCropRect.Bottom, CropMode.Pixels);
            //imageFactory.Crop(cropLayer);
            //imageFactory.Save(newPath);
        }

        public Rectangle GetCropRect()
        {
            if (cropAdorner != null)
            {
                var rect = cropAdorner.GetCropRect();
                var src = ((SingleImageViewModel)currentFrameworkElement.DataContext).PreviewImage;
                
                // Calculate scaling factors between UI and actual image dimensions
                var widthRatio = src.Width / currentFrameworkElement.ActualWidth;
                var heightRatio = src.Height / currentFrameworkElement.ActualHeight;
                
                // Scale the crop rectangle coordinates
                var scaledX = (int)(rect.X * widthRatio);
                var scaledY = (int)(rect.Y * heightRatio);
                var scaledWidth = (int)(rect.Width * widthRatio);
                var scaledHeight = (int)(rect.Height * heightRatio);
                
                Debug.WriteLine($"UI Crop: X={rect.X}, Y={rect.Y}, Width={rect.Width}, Height={rect.Height}");
                Debug.WriteLine($"Scaled Crop: X={scaledX}, Y={scaledY}, Width={scaledWidth}, Height={scaledHeight}");
                Debug.WriteLine($"Ratios: Width={widthRatio}, Height={heightRatio}");
                
                return new Rectangle(scaledX, scaledY, scaledWidth, scaledHeight);
            }

            return new Rectangle(
                (int)(currentFrameworkElement.ActualWidth * 0.2),
                (int)(currentFrameworkElement.ActualHeight * 0.2),
                (int)currentFrameworkElement.ActualWidth,
                (int)currentFrameworkElement.ActualHeight);
        }
    }
}
