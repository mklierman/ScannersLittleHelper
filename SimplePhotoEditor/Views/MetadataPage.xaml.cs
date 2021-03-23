using SimplePhotoEditor.Helpers;
using SimplePhotoEditor.ViewModels;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;
using System.Text.RegularExpressions;
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
        private readonly string fileValidationRegex = @"\A(?!(?:COM[0-9]|CON|LPT[0-9]|NUL|PRN|AUX|com[0-9]|con|lpt[0-9]|nul|prn|aux)(\.|\z)|\s|[\.]{2,})[^\\\/:*""?<>|]{1,254}(?<![\s\.])\z";
        public MetadataPage()
        {
            InitializeComponent();
        }

        [Import]
        public MetadataViewModel ViewModel
        {
            set { this.DataContext = value;      }
            get { return (MetadataViewModel)this.DataContext; }
        }

        private void TagTextboxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ViewModel.AddTag();
            }
        }

        public void FocusOnName()
        {
            FilenameTextbox.Focus();
            FilenameTextbox.SelectAll();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void FilenameTextbox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.Match(((TextBox)sender).Text + e.Text, fileValidationRegex).Success;
        }
    }
}
