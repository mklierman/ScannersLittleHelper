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
            if (WindowState == System.Windows.WindowState.Maximized)
            {
                App.Current.Properties["AppHeight"] = RestoreBounds.Height;
                App.Current.Properties["AppWidth"] = RestoreBounds.Width;
                App.Current.Properties["AppLeft"] = RestoreBounds.Left;
                App.Current.Properties["AppTop"] = RestoreBounds.Top;
            }
            else
            {
                App.Current.Properties["AppHeight"] = App.Current.MainWindow.Height;
                App.Current.Properties["AppWidth"] = App.Current.MainWindow.Width;
                App.Current.Properties["AppLeft"] = App.Current.MainWindow.Left;
                App.Current.Properties["AppTop"] = App.Current.MainWindow.Top;
            }

            App.Current.Properties["AppIsMaximized"] = WindowState == System.Windows.WindowState.Maximized;
        }

        private void MetroWindow_Initialized(object sender, EventArgs e)
        {
        }

        private void MetroWindow_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            if (WindowState == System.Windows.WindowState.Normal)
            {
                App.Current.Properties["AppHeight"] = App.Current.MainWindow.Height;
                App.Current.Properties["AppWidth"] = App.Current.MainWindow.Width;
                App.Current.Properties["AppLeft"] = App.Current.MainWindow.Left;
                App.Current.Properties["AppTop"] = App.Current.MainWindow.Top;
            }

            App.Current.Properties["AppIsMaximized"] = WindowState == System.Windows.WindowState.Maximized;
        }

        private void MetroWindow_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            regionManager.Regions[Regions.Main].Add(new ScanPage(), PageKeys.Scan);
            regionManager.Regions[Regions.Main].Add(new ThumbnailPage(), PageKeys.Thumbnail);
            regionManager.Regions[Regions.Main].Add(new SingleImagePage(), PageKeys.SingleImage);

        }
    }
}
