using ImageProcessor.Imaging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

namespace SimplePhotoEditor.Models
{
    public class EditUndoModel
    {
        public ImageSource ImageSource;
        public ImageEdits ImageEdits;
        public string TempFilePath;

        public EditUndoModel()
        {
            ImageEdits = new ImageEdits();
        }

        public EditUndoModel(ImageSource imageSource, string tempFilePath, CropLayer cropLayer)
        {
            ImageEdits = new ImageEdits();
            TempFilePath = tempFilePath;
            ImageSource = imageSource;
            ImageEdits.cropLayer = cropLayer;
        }
    }
}
