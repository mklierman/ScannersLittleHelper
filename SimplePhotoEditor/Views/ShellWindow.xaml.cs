using MahApps.Metro.Controls;

using Prism.Regions;

using SimplePhotoEditor.Constants;
using System;

namespace SimplePhotoEditor.Views
{
    public partial class ShellWindow : MetroWindow
    {
        public ShellWindow(IRegionManager regionManager)
        {
            InitializeComponent();
            RegionManager.SetRegionName(hamburgerMenuContentControl, Regions.Main);
            RegionManager.SetRegionManager(hamburgerMenuContentControl, regionManager);

        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            App.Current.Properties["AppHeight"] = App.Current.MainWindow.Height;
            App.Current.Properties["AppWidth"] = App.Current.MainWindow.Width;
        }

        private void MetroWindow_Initialized(object sender, EventArgs e)
        {

        }

        private void MetroWindow_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            App.Current.Properties["AppHeight"] = App.Current.MainWindow.Height;
            App.Current.Properties["AppWidth"] = App.Current.MainWindow.Width;
        }
    }
}
