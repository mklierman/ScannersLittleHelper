using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using DNTScanner.Core;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;

using SimplePhotoEditor.Contracts.Services;
using SimplePhotoEditor.Models;

namespace SimplePhotoEditor.ViewModels
{
    // TODO WTS: Change the URL for your privacy policy in the appsettings.json file, currently set to https://YourPrivacyUrlGoesHere
    public class SettingsViewModel : BindableBase, INavigationAware
    {
        private readonly AppConfig _appConfig;
        private readonly IThemeSelectorService _themeSelectorService;
        private readonly ISystemService _systemService;
        private readonly IApplicationInfoService _applicationInfoService;
        private AppTheme _theme;
        private string _versionDescription;
        private ICommand _setThemeCommand;
        private ICommand _privacyStatementCommand;
        private Dictionary<string, ScannerSettings> scannerList = new Dictionary<string, ScannerSettings>();
        private ScannerSettings selectedScanner;
        private ObservableCollection<int> dpiList = new ObservableCollection<int>();
        private int selectedDPI;


        public Dictionary<string, ScannerSettings> ScannerList
        {
            get
            {
                return scannerList;
            }
            set { SetProperty(ref scannerList, value); }
        }

        public ScannerSettings SelectedScanner
        {
            get { return selectedScanner; }
            set { SetProperty(ref selectedScanner, value); PopulateDPIList(); }
        }

        private void PopulateScanners()
        {
            ScannerList.Clear();
            var scanners = SystemDevices.GetScannerDevices();
            foreach (var scanner in scanners)
            {
                scanner.ScannerDeviceSettings.TryGetValue("Name", out object scannerName);
                ScannerList.Add(scannerName.ToString(), scanner);
            }
        }

        public ObservableCollection<int> DPIList
        {
            get
            {
                return dpiList;
            }
            set { SetProperty(ref dpiList, value); }
        }

        public int SelectedDPI
        {
            get { return selectedDPI; }
            set { SetProperty(ref selectedDPI, value); }
        }

        private void PopulateDPIList()
        {
            if (SelectedScanner != null)
            {
                DPIList.Clear();
                var supportedDPIs = SelectedScanner.SupportedResolutions;
                foreach (var res in supportedDPIs)
                {
                    DPIList.Add(res);
                }
            }
        }

        public AppTheme Theme
        {
            get { return _theme; }
            set { SetProperty(ref _theme, value); }
        }

        public string VersionDescription
        {
            get { return _versionDescription; }
            set { SetProperty(ref _versionDescription, value); }
        }

        public ICommand SetThemeCommand => _setThemeCommand ?? (_setThemeCommand = new DelegateCommand<string>(OnSetTheme));

        public ICommand PrivacyStatementCommand => _privacyStatementCommand ?? (_privacyStatementCommand = new DelegateCommand(OnPrivacyStatement));

        public SettingsViewModel(AppConfig appConfig, IThemeSelectorService themeSelectorService, ISystemService systemService, IApplicationInfoService applicationInfoService)
        {
            _appConfig = appConfig;
            _themeSelectorService = themeSelectorService;
            _systemService = systemService;
            _applicationInfoService = applicationInfoService;
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            VersionDescription = $"SimplePhotoEditor - {_applicationInfoService.GetVersion()}";
            Theme = _themeSelectorService.GetCurrentTheme();
            PopulateScanners();
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }

        private void OnSetTheme(string themeName)
        {
            var theme = (AppTheme)Enum.Parse(typeof(AppTheme), themeName);
            _themeSelectorService.SetTheme(theme);
        }

        private void OnPrivacyStatement()
            => _systemService.OpenInWebBrowser(_appConfig.PrivacyStatement);

        public bool IsNavigationTarget(NavigationContext navigationContext)
            => true;
    }
}
