using ImageProcessor.Imaging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace SimplePhotoEditor.Models
{
    public class ImageEdits
    {
        public CropLayer cropLayer { get; set; }
        public double rotateAmount {get; set; }
    }
}
