using SimplePhotoEditor.ViewModels;
using System.Windows.Controls;
using System.ComponentModel;
using System.Composition;

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
    }
}
