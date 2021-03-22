using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows;
using System.Windows.Documents;

namespace SimplePhotoEditor.Managers
{
    public class CropManager
    {
        private CroppingAdorner.CroppingAdorner cropAdorner;
        private FrameworkElement currentFrameworkElement = null;
        private Brush originalBrush;
        public void AddCropToElement(FrameworkElement frameworkElement)
        {
            if (currentFrameworkElement != null)
            {
                RemoveCropFromCur();
            }
            Rect interiorRectangle = new Rect(
                frameworkElement.ActualWidth * 0.2,
                frameworkElement.ActualHeight * 0.2,
                frameworkElement.ActualWidth * 0.6,
                frameworkElement.ActualHeight * 0.6);
            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(frameworkElement);
            cropAdorner = new CroppingAdorner.CroppingAdorner(frameworkElement, interiorRectangle);
            adornerLayer.Add(cropAdorner);
            //imgCrop.Source = _clp.BpsCrop();
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

                //tblkClippingRectangle.Text = string.Format(
                //    "Clipping Rectangle: ({0:N1}, {1:N1}, {2:N1}, {3:N1})",
                //    rc.Left,
                //    rc.Top,
                //    rc.Right,
                //    rc.Bottom);
                //imgCrop.Source = _clp.BpsCrop();
            }
        }

        private void CropChanged(Object sender, RoutedEventArgs args)
        {
            RefreshCropImage();
        }
    }
}
