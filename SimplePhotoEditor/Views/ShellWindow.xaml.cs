using MahApps.Metro.Controls;

using Prism.Regions;

using SimplePhotoEditor.Constants;
using System;
using System.Windows;
using System.Windows.Threading;

namespace SimplePhotoEditor.Views
{
    public partial class ShellWindow : MetroWindow
    {
        private IRegionManager regionManager;
        private DispatcherTimer toastDismissTimer;

        public ShellWindow(IRegionManager regionManager)
        {
            InitializeComponent();
            RegionManager.SetRegionName(hamburgerMenuContentControl, Regions.Main);
            RegionManager.SetRegionManager(hamburgerMenuContentControl, regionManager);
            this.regionManager = regionManager;


        }

        /// <summary>
        /// Shows a short toast at the bottom-right of the shell; dismissed after a few seconds.
        /// </summary>
        public void ShowToast(string message)
        {
            if (ToastHost == null || ToastText == null)
            {
                return;
            }

            ToastText.Text = message;
            ToastHost.Visibility = Visibility.Visible;

            toastDismissTimer?.Stop();
            toastDismissTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
            {
                Interval = TimeSpan.FromSeconds(4.5)
            };
            toastDismissTimer.Tick += ToastDismissTimer_OnTick;
            toastDismissTimer.Start();
        }

        private void ToastDismissTimer_OnTick(object sender, EventArgs e)
        {
            toastDismissTimer.Stop();
            toastDismissTimer.Tick -= ToastDismissTimer_OnTick;
            if (ToastHost != null)
            {
                ToastHost.Visibility = Visibility.Collapsed;
            }
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            toastDismissTimer?.Stop();
            toastDismissTimer = null;

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
