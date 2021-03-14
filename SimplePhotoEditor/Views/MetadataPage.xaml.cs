using SimplePhotoEditor.ViewModels;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SimplePhotoEditor.Views
{
    /// <summary>
    /// Interaction logic for MetadataPage.xaml
    /// </summary>
    public partial class MetadataPage : UserControl
    {
        public MetadataPage()
        {
            InitializeComponent();
        }

        [Import]
        public MetadataViewModel ViewModel
        {
            set { this.DataContext = value; }
            get { return (MetadataViewModel)this.DataContext; }
        }
    }
}
