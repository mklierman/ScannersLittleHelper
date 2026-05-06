using SimplePhotoEditor.ViewModels;
using System.Windows.Controls;
using System.ComponentModel;
using System.Composition;
using Prism.Regions;
using System.Windows;
using System.Windows.Input;

namespace SimplePhotoEditor.Views
{
    public partial class ScanPage : UserControl
    {
        public ScanPage()
        {
            InitializeComponent();
            Loaded += ScanPage_Loaded;
        }

        [Import]
        public ScanViewModel ViewModel
        {
            set { this.DataContext = value; }
            get { return (ScanViewModel)this.DataContext; }
        }

        private void Image_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ViewModel?.HandleSkewMouseDown(ImageElement, ImageContainer, e);
        }

        private void Image_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ViewModel?.HandleSkewMouseMove(ImageElement, e);
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            //ViewModel.FilePath = navigationContext.Parameters["FilePath"].ToString();
        }

        private void CropButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ViewModel?.CropCommand?.Execute(ImageElement);
        }

        private void ImageContainer_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ViewModel?.HandleSkewMouseDown(ImageContainer, ImageContainer, e);
        }

        private void ImageContainer_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ViewModel?.HandleSkewMouseMove(ImageContainer, e);
        }

        private void ImageContainer_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ViewModel?.HandleSkewMouseUp(ImageContainer, ImageContainer, e);
        }

        private void ScanPage_Loaded(object sender, RoutedEventArgs e)
        {
            Keyboard.Focus(this);
        }

        private void ScanPage_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                ViewModel?.CancelCropOrSkew();
                e.Handled = true;
            }
        }

        private void ScanPage_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            ViewModel?.CancelActiveEdits();
            e.Handled = true;
        }
    }
}
