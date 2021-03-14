using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ImageProcessor;
using Prism.Mvvm;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using System.IO;
using Prism.Commands;
using System.Windows.Input;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Forms;
using ControlzEx.Theming;
using Prism.Regions;

namespace SimplePhotoEditor.ViewModels
{
    public class SingleImageViewModel : BindableBase, IAutoInitialize
    {
        private string filePath;
        private ImageFactory selectedImage;
        private ImageSource previewImage;


        private string fileName;
        private string title;
        private string subject;
        private string comments;
        private MetadataViewModel metadataViewModel = new MetadataViewModel();

        private DateTime dateTaken;
        private ObservableCollection<string> tags;
        private string tag;
        private ICommand saveCommand;
        private ICommand saveNextCommand;
        private ICommand cancelCommand;
        private ICommand cropCommand;
        private ICommand autoCropCommand;
        private ICommand rotateLeftCommand;
        private ICommand rotateRightCommand;
        private ICommand skewCommand;
        private ICommand undoCommand;


        public SingleImageViewModel()
        {
            var theme = ThemeManager.Current.DetectTheme(SimplePhotoEditor.App.Current);
            //FilePath = @"C:\temp\ASD.tif";
            //ImageSelected();



            //GetMetadata();
        }
        public ICommand CancelCommand => cancelCommand ?? (cancelCommand = new DelegateCommand(GetMetadata));
        public ICommand SaveCommand => saveCommand ?? (saveCommand = new DelegateCommand(OnSave));
        public ICommand SaveNextCommand => saveNextCommand ?? (saveNextCommand = new DelegateCommand(OnSave));
        public ICommand CropCommand => cropCommand ?? (cropCommand = new DelegateCommand(GetMetadata));
        public ICommand AutoCropCommand => autoCropCommand ?? (autoCropCommand = new DelegateCommand(GetMetadata));
        public ICommand RotateLeftCommand => rotateLeftCommand ?? (rotateLeftCommand = new DelegateCommand(GetMetadata));
        public ICommand RotateRightCommand => rotateRightCommand ?? (rotateRightCommand = new DelegateCommand(GetMetadata));
        public ICommand SkewCommand => skewCommand ?? (skewCommand = new DelegateCommand(GetMetadata));
        public ICommand UndoCommand => undoCommand ?? (undoCommand = new DelegateCommand(GetMetadata));

        public MetadataViewModel MetaDataViewModel { get => metadataViewModel; set => SetProperty(ref metadataViewModel, value); }
        private void ImageSelected()
        {
            metadataViewModel.FilePath = FilePath;
        }

        private void GetImagePreview()
        {
            var bmi = new BitmapImage();
            bmi.BeginInit();
            bmi.CacheOption = BitmapCacheOption.OnLoad;
            bmi.UriSource = new Uri(FilePath);
            bmi.EndInit();
            PreviewImage = bmi;
        }

        private void GetMetadata()
        {
            ShellFile shellFile = ShellFile.FromFilePath(FilePath);
            FileName = Path.GetFileNameWithoutExtension(FilePath);
            Title = shellFile.Properties.System.Title.Value;
            Subject = shellFile.Properties.System.Subject.Value;
            Comments = shellFile.Properties.System.Comment.Value;
            DateTaken = Convert.ToDateTime(shellFile.Properties.System.Photo.DateTaken.Value);
            string[] tagsArray = shellFile.Properties.System.Photo.TagViewAggregate.Value;
            if (tagsArray != null)
            {
                Tags = new ObservableCollection<string>(tagsArray);
            }
        }


        private void OnSave()
        {
            ShellFile shellFile = ShellFile.FromFilePath(FilePath);


            if (Title != shellFile.Properties.System.Title.Value)
            {
                shellFile.Properties.System.Title.Value = Title;
            }
            if (Subject != shellFile.Properties.System.Subject.Value)
            {
                shellFile.Properties.System.Subject.Value = Subject;
            }
            if (Comments != shellFile.Properties.System.Comment.Value)
            {
                shellFile.Properties.System.Comment.Value = Comments;
            }
            if (DateTaken != Convert.ToDateTime(shellFile.Properties.System.Photo.DateTaken.Value))
            {
                shellFile.Properties.System.Photo.DateTaken.Value = DateTaken;
            }
            string[] tagsArray = null;
            Tags?.CopyTo(tagsArray, 0);
            if (tagsArray != shellFile.Properties.System.Photo.TagViewAggregate.Value)
            {
                shellFile.Properties.System.Photo.TagViewAggregate.Value = tagsArray;
            }
            shellFile.Dispose();


            if (FileName != Path.GetFileNameWithoutExtension(FilePath))
            {
                File.Move(FilePath, Path.GetDirectoryName(FilePath) + "\\" + FileName + Path.GetExtension(FilePath));
            }
        }

        public ImageSource PreviewImage
        {
            get => previewImage;
            set
            {
                if (previewImage != value)
                {
                    previewImage = value;
                    RaisePropertyChanged(nameof(PreviewImage));
                }
            }
        }

        public string FilePath
        {
            get => filePath;
            set
            {
                if (filePath != value)
                {
                    filePath = value;
                    RaisePropertyChanged(nameof(FilePath));
                    if (File.Exists(FilePath))
                    {
                        GetImagePreview();
                        GetMetadata();
                    }
                }
            }
        }

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

        public string Title
        {
            get => title;
            set
            {
                if (title != value)
                {
                    title = value;
                    RaisePropertyChanged(nameof(Title));
                }
            }
        }

        public string Subject
        {
            get => subject;
            set
            {
                if (subject != value)
                {
                    subject = value;
                    RaisePropertyChanged(nameof(Subject));
                }
            }
        }

        public string Comments
        {
            get => comments;
            set
            {
                if (comments != value)
                {
                    comments = value;
                    RaisePropertyChanged(nameof(Comments));
                }
            }
        }

        public DateTime DateTaken
        {
            get => dateTaken;
            set
            {
                if (dateTaken != value)
                {
                    dateTaken = value;
                    RaisePropertyChanged(nameof(DateTaken));
                }
            }
        }

        public ObservableCollection<string> Tags
        {
            get => tags;
            set
            {
                if (tags != value)
                {
                    tags = value;
                    RaisePropertyChanged(nameof(Tags));
                }
            }
        }

        public string Tag
        {
            get => tag;
            set
            {
                if (tag != value)
                {
                    tag = value;
                    RaisePropertyChanged(nameof(Tag));
                }
            }
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            FilePath = navigationContext.Parameters["FilePath"].ToString();
        }
        //        internal void DrawCropRectangle(object sender, MouseButtonEventArgs e)
        //        {
        //            if (e.LeftButton == MouseButtonState.Pressed)
        //            {
        //                System.Windows.Point point = e.GetPosition((System.Windows.IInputElement)sender);

        //                cropX = point.X;
        //                cropY = point.Y;
        //                cropPen = new System.Drawing.Pen(System.Drawing.Color.Black, 1);
        //                cropPen.DashStyle = System.Drawing.Drawing2D.DashStyle.DashDot;
        //                System.Windows.Controls.Image imageControl = (System.Windows.Controls.Image)sender;
        //                imageControl.UpdateLayout();
        //            }

        //        }


        //        internal void MoveCropRectangle(object sender, MouseEventArgs e)
        //{
        //            cropWidth = e.X - cropX;
        //            cropHeight = e.Y - cropY;
        //            System.Windows.Controls.Image imageControl = (System.Windows.Controls.Image)sender;
        //            imageControl.CreateGraphics().DrawRectangle(cropPen, cropX, cropY, cropWidth, cropHeight);
        //            imageControl.
        //        }
    }
}
