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
    }
}
