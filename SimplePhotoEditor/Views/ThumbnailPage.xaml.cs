using Prism.Regions;
using SimplePhotoEditor.Constants;
using SimplePhotoEditor.ViewModels;
using System.Composition;
using System.Windows.Controls;

namespace SimplePhotoEditor.Views
{
    public partial class ThumbnailPage : UserControl
    {

        public ThumbnailPage()
        {
            InitializeComponent();
        }

        [Import]
        public ThumbnailViewModel ViewModel
        {
            set { this.DataContext = value; }
            get { return (ThumbnailViewModel)this.DataContext; }
        }

        private void Image_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ViewModel.OpenSingleImage();
            }
        }

        private void UserControl_Initialized(object sender, System.EventArgs e)
        {

        }
    }
}
