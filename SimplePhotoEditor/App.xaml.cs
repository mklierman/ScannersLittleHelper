using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Configuration;

using Prism.Ioc;
using Prism.Mvvm;
using Prism.Unity;

using SimplePhotoEditor.Constants;
using SimplePhotoEditor.Contracts.Services;
using SimplePhotoEditor.Core.Contracts.Services;
using SimplePhotoEditor.Core.Services;
using SimplePhotoEditor.Models;
using SimplePhotoEditor.Services;
using SimplePhotoEditor.ViewModels;
using SimplePhotoEditor.Views;

namespace SimplePhotoEditor
{
    // For more inforation about application lifecyle events see https://docs.microsoft.com/dotnet/framework/wpf/app-development/application-management-overview
    // For docs about using Prism in WPF see https://prismlibrary.com/docs/wpf/introduction.html

    // WPF UI elements use language en-US by default.
    // If you need to support other cultures make sure you add converters and review dates and numbers in your UI to ensure everything adapts correctly.
    // Tracking issue for improving this is https://github.com/dotnet/wpf/issues/1946
    public partial class App : PrismApplication
    {
        private string[] _startUpArgs;

        public App()
        {
        }

        protected override Window CreateShell()
            => Container.Resolve<ShellWindow>();

        protected override async void OnInitialized()
        {
            var persistAndRestoreService = Container.Resolve<IPersistAndRestoreService>();
            persistAndRestoreService.RestoreData();

            var themeSelectorService = Container.Resolve<IThemeSelectorService>();
            themeSelectorService.InitializeTheme();

            if (App.Current.Properties.Contains("AppHeight"))
            {
                App.Current.MainWindow.Height = Convert.ToDouble(App.Current.Properties["AppHeight"].ToString());
            }
            if (App.Current.Properties.Contains("AppWidth"))
            {
                App.Current.MainWindow.Width = Convert.ToDouble(App.Current.Properties["AppWidth"].ToString());
            }
            if (App.Current.Properties.Contains("AppLeft"))
            {
                App.Current.MainWindow.Left = Convert.ToDouble(App.Current.Properties["AppLeft"].ToString());
            }
            if (App.Current.Properties.Contains("AppTop"))
            {
                App.Current.MainWindow.Top = Convert.ToDouble(App.Current.Properties["AppTop"].ToString());
            }
            if (App.Current.Properties.Contains("AppIsMaximized"))
            {
                var isMaximized = Convert.ToBoolean(App.Current.Properties["AppIsMaximized"].ToString());
                App.Current.MainWindow.WindowState = isMaximized ? WindowState.Maximized : WindowState.Normal;
            }


            base.OnInitialized();
            await Task.CompletedTask;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            _startUpArgs = e.Args;
            base.OnStartup(e);
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Core Services
            containerRegistry.Register<IFileService, FileService>();

            // App Services
            containerRegistry.Register<IApplicationInfoService, ApplicationInfoService>();
            containerRegistry.Register<ISystemService, SystemService>();
            containerRegistry.Register<IPersistAndRestoreService, PersistAndRestoreService>();
            containerRegistry.Register<IThemeSelectorService, ThemeSelectorService>();
            containerRegistry.Register<ISessionService, SessionService>();
            containerRegistry.RegisterSingleton<IToastService, ToastService>();

            // ViewModels
            containerRegistry.RegisterSingleton<MetadataViewModel>();

            // Views
            containerRegistry.RegisterForNavigation<SettingsPage, SettingsViewModel>(PageKeys.Settings);
            containerRegistry.RegisterForNavigation<ThumbnailPage, ThumbnailViewModel>(PageKeys.Thumbnail);
            containerRegistry.RegisterForNavigation<SingleImagePage, SingleImageViewModel>(PageKeys.SingleImage);
            containerRegistry.RegisterForNavigation<ScanPage, ScanViewModel>(PageKeys.Scan);
            containerRegistry.RegisterForNavigation<MetadataPage, MetadataViewModel>();
            containerRegistry.RegisterForNavigation<ShellWindow, ShellViewModel>();

            // Dialogs
            containerRegistry.RegisterDialog<NewFolderDialog, NewFolderDialogViewModel>("InputDialog");
            containerRegistry.RegisterDialog<ErrorDialog, ErrorDialogViewModel>("ErrorDialog");
            containerRegistry.RegisterDialog<ConfirmationDialog, ConfirmationDialogViewModel>("ConfirmationDialog");

            // Configuration
            var configuration = BuildConfiguration();
            var appConfig = new AppConfig
            {
                ConfigurationsFolder = "SimplePhotoEditor\\Configurations",
                AppPropertiesFileName = "AppProperties.json",
                PrivacyStatement = "https://YourPrivacyUrlGoesHere/"
            };

            // Register configurations to IoC (configuration is currently only used for potential future extensions)
            containerRegistry.RegisterInstance<IConfiguration>(configuration);
            containerRegistry.RegisterInstance(appConfig);
        }

        private IConfiguration BuildConfiguration()
        {
            var appLocation = AppContext.BaseDirectory;
            return new ConfigurationBuilder()
                .SetBasePath(appLocation)
                .AddCommandLine(_startUpArgs)
                .Build();
        }

        private void OnExit(object sender, ExitEventArgs e)
        {
            var persistAndRestoreService = Container.Resolve<IPersistAndRestoreService>();
            persistAndRestoreService.PersistData();
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // TODO WTS: Please log and handle the exception as appropriate to your scenario
            // For more info see https://docs.microsoft.com/dotnet/api/system.windows.application.dispatcherunhandledexception?view=netcore-3.0
        }
    }
}
