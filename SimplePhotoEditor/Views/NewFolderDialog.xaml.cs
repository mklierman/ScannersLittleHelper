using MahApps.Metro.Controls.Dialogs;
using Prism.Commands;
using Prism.Services.Dialogs;
using SimplePhotoEditor.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Composition;
using System.Runtime.CompilerServices;
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
    /// Interaction logic for NewFolderDialog.xaml
    /// </summary>
    public partial class NewFolderDialog : BaseMetroDialog
    {
        public NewFolderDialog()
        {
            InitializeComponent();
        }


        [Import]
        public NewFolderDialogViewModel ViewModel
        {
            set { this.DataContext = value; }
            get { return (NewFolderDialogViewModel)this.DataContext; }
        }

        private void BaseMetroDialog_Loaded(object sender, RoutedEventArgs e)
        {
            PART_TextBox.Focus();
        }
    }
}
