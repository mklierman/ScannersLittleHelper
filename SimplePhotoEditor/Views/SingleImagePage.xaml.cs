using SimplePhotoEditor.ViewModels;
using System.Windows.Controls;
using System.ComponentModel;
using System.Composition;
using Prism.Regions;
using System.Windows;
using System.Windows.Input;

namespace SimplePhotoEditor.Views
{
    public partial class SingleImagePage : UserControl
    {
        public SingleImagePage()
        {
            InitializeComponent();
        }

        [Import]
        public SingleImageViewModel ViewModel
        {
            set { this.DataContext = value; }
            get { return (SingleImageViewModel)this.DataContext; }
        }

        private void Image_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //ViewModel.DrawCropRectangle(sender, e);
        }

        private void Image_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            //ViewModel.MoveCropRectangle(sender, e);
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            ViewModel.FilePath = navigationContext.Parameters["FilePath"].ToString();
        }

        private void CropButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var imageContainer = this.FindName("ImageContainer") as Grid;
            if (imageContainer != null)
            {
                ViewModel.CropCommand.Execute(imageContainer);
            }
        }

        private void ImageContainer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is SingleImageViewModel viewModel)
            {
                viewModel.HandleSkewMouseDown(sender, e);
            }
        }

        private void ImageContainer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is SingleImageViewModel viewModel)
            {
                viewModel.HandleSkewMouseUp(sender, e);
            }
        }

        private void ImageContainer_MouseMove(object sender, MouseEventArgs e)
        {
            if (DataContext is SingleImageViewModel viewModel)
            {
                viewModel.HandleSkewMouseMove(sender, e);
            }
        }
    }
}
