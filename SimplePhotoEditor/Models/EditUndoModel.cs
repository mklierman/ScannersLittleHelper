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
        public byte[] ImageBytes;
        public ImageEdits ImageEdits;
        public string TempFilePath;

        public EditUndoModel()
        {
            ImageEdits = new ImageEdits();
        }

        public EditUndoModel(ImageSource imageSource, byte[] imageBytes, string tempFilePath, CropLayer cropLayer)
        {
            ImageEdits = new ImageEdits();
            TempFilePath = tempFilePath;
            ImageSource = imageSource;
            ImageBytes = imageBytes;
            ImageEdits.cropLayer = cropLayer;
        }
    }
}
