using MahApps.Metro.Controls;

using Prism.Regions;

using SimplePhotoEditor.Constants;
using System;

namespace SimplePhotoEditor.Views
{
    public partial class ShellWindow : MetroWindow
    {
        private IRegionManager regionManager;
        public ShellWindow(IRegionManager regionManager)
        {
            InitializeComponent();
            RegionManager.SetRegionName(hamburgerMenuContentControl, Regions.Main);
            RegionManager.SetRegionManager(hamburgerMenuContentControl, regionManager);
            this.regionManager = regionManager;


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

        private void MetroWindow_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            regionManager.Regions[Regions.Main].Add(new ThumbnailPage(), PageKeys.Thumbnail);
            regionManager.Regions[Regions.Main].Add(new SingleImagePage(), PageKeys.SingleImage);
            regionManager.Regions[Regions.Main].Add(new ScanPage(), PageKeys.Scan);

        }
    }
}
