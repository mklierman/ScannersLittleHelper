using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
        private const string LastSelectedScannerNameKey = "LastSelectedScannerName";
        private const string LastSelectedDpiKeyPrefix = "LastSelectedDpi::";
        private const string AutoCropStrengthKey = "AutoCropStrength";
        private readonly AppConfig _appConfig;
        private readonly IThemeSelectorService _themeSelectorService;
        private readonly ISystemService _systemService;
        private readonly IApplicationInfoService _applicationInfoService;
        private AppTheme _theme;
        private string _versionDescription;
        private ICommand _setThemeCommand;
        private ICommand _privacyStatementCommand;
        private ICommand _refreshScannersCommand;
        private Dictionary<string, ScannerSettings> scannerList = new Dictionary<string, ScannerSettings>();
        private ScannerSettings selectedScanner;
        private string selectedScannerName;
        private ObservableCollection<int> dpiList = new ObservableCollection<int>();
        private int selectedDPI;
        private ObservableCollection<string> autoCropStrengthOptions = new ObservableCollection<string> { "Low", "Medium", "High" };
        private string selectedAutoCropStrength = "Medium";


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
            set
            {
                if (SetProperty(ref selectedScanner, value))
                {
                    Debug.WriteLine($"[SettingsVM] SelectedScanner changed -> '{GetSelectedScannerName() ?? "<null>"}'");
                    PopulateDPIList();
                    SaveSelectedScannerName();
                }
            }
        }

        public string SelectedScannerName
        {
            get { return selectedScannerName; }
            set
            {
                if (SetProperty(ref selectedScannerName, value))
                {
                    Debug.WriteLine($"[SettingsVM] SelectedScannerName changed -> '{SelectedScannerName ?? "<null>"}'");
                    if (!string.IsNullOrWhiteSpace(SelectedScannerName) && ScannerList.TryGetValue(SelectedScannerName, out var scanner))
                    {
                        SelectedScanner = scanner;
                    }
                    else
                    {
                        SelectedScanner = null;
                        DPIList.Clear();
                        SelectedDPI = 0;
                    }
                }
            }
        }

        private void PopulateScanners()
        {
            Debug.WriteLine("[SettingsVM] PopulateScanners: start");
            try
            {
                var newScannerList = new Dictionary<string, ScannerSettings>();
                var scanners = SystemDevices.GetScannerDevices();
                var discoveredCount = 0;
                foreach (var scanner in scanners)
                {
                    try
                    {
                        discoveredCount++;
                        scanner.ScannerDeviceSettings.TryGetValue("Name", out object scannerName);
                        var name = scannerName?.ToString();
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            Debug.WriteLine("[SettingsVM] PopulateScanners: skipping scanner with empty name");
                            continue;
                        }

                        if (newScannerList.ContainsKey(name))
                        {
                            Debug.WriteLine($"[SettingsVM] PopulateScanners: duplicate scanner name '{name}', skipping duplicate entry");
                            continue;
                        }

                        newScannerList.Add(name, scanner);
                        Debug.WriteLine($"[SettingsVM] PopulateScanners: added '{name}'");
                    }
                    catch (Exception ex)
                    {
                        // Keep loading other scanners if one device throws COM/runtime errors.
                        Debug.WriteLine($"[SettingsVM] PopulateScanners: scanner item exception: {ex}");
                    }
                }

                ScannerList = newScannerList;
                Debug.WriteLine($"[SettingsVM] PopulateScanners: enumerated {discoveredCount} scanner item(s), valid entries={ScannerList.Count}");
                RestoreSelectedScanner();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SettingsVM] PopulateScanners exception: {ex}");
            }

            Debug.WriteLine("[SettingsVM] PopulateScanners: end");
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
            set
            {
                if (SetProperty(ref selectedDPI, value))
                {
                    Debug.WriteLine($"[SettingsVM] SelectedDPI changed -> {SelectedDPI}");
                    SaveSelectedDpi();
                }
            }
        }

        public ObservableCollection<string> AutoCropStrengthOptions
        {
            get => autoCropStrengthOptions;
            set => SetProperty(ref autoCropStrengthOptions, value);
        }

        public string SelectedAutoCropStrength
        {
            get => selectedAutoCropStrength;
            set
            {
                if (SetProperty(ref selectedAutoCropStrength, value))
                {
                    SaveAutoCropStrength();
                }
            }
        }

        private void PopulateDPIList()
        {
            Debug.WriteLine($"[SettingsVM] PopulateDPIList: start for scanner '{GetSelectedScannerName() ?? "<null>"}'");
            if (SelectedScanner != null)
            {
                DPIList.Clear();
                var supportedDPIs = SelectedScanner.SupportedResolutions;
                Debug.WriteLine($"[SettingsVM] PopulateDPIList: scanner reports {supportedDPIs?.Count ?? 0} DPI value(s)");
                foreach (var res in supportedDPIs)
                {
                    DPIList.Add(res);
                }

                RestoreSelectedDpi();

                // Keep UI and scan settings in a valid state even when
                // previously saved DPI is missing or no longer supported.
                if ((SelectedDPI <= 0 || !DPIList.Contains(SelectedDPI)) && DPIList.Count > 0)
                {
                    Debug.WriteLine($"[SettingsVM] PopulateDPIList: applying fallback DPI {DPIList[0]}");
                    SelectedDPI = DPIList[0];
                }
            }
            else
            {
                Debug.WriteLine("[SettingsVM] PopulateDPIList: SelectedScanner is null");
            }

            Debug.WriteLine($"[SettingsVM] PopulateDPIList: end with SelectedDPI={SelectedDPI}, DPI count={DPIList.Count}");
        }

        private void SaveSelectedScannerName()
        {
            var scannerName = SelectedScannerName;
            if (string.IsNullOrWhiteSpace(scannerName))
            {
                scannerName = GetSelectedScannerName();
            }

            if (string.IsNullOrWhiteSpace(scannerName))
            {
                Debug.WriteLine("[SettingsVM] SaveSelectedScannerName: skipped (scanner name is null/empty)");
                return;
            }

            App.Current.Properties[LastSelectedScannerNameKey] = scannerName;
            Debug.WriteLine($"[SettingsVM] SaveSelectedScannerName: '{scannerName}'");
        }

        private string GetSelectedScannerName()
        {
            if (!string.IsNullOrWhiteSpace(SelectedScannerName))
            {
                return SelectedScannerName;
            }

            if (SelectedScanner == null || SelectedScanner.ScannerDeviceSettings == null)
            {
                return null;
            }

            if (SelectedScanner.ScannerDeviceSettings.TryGetValue("Name", out var scannerNameObj))
            {
                var scannerName = scannerNameObj?.ToString();
                if (!string.IsNullOrWhiteSpace(scannerName))
                {
                    return scannerName;
                }
            }

            return null;
        }

        private void SaveSelectedDpi()
        {
            var scannerName = GetSelectedScannerName();
            if (string.IsNullOrWhiteSpace(scannerName))
            {
                Debug.WriteLine("[SettingsVM] SaveSelectedDpi: skipped (scanner name is null/empty)");
                return;
            }

            if (SelectedDPI > 0)
            {
                var dpiKey = $"{LastSelectedDpiKeyPrefix}{scannerName}";
                App.Current.Properties[dpiKey] = SelectedDPI;
                Debug.WriteLine($"[SettingsVM] SaveSelectedDpi: key='{dpiKey}', value={SelectedDPI}");
            }
            else
            {
                Debug.WriteLine($"[SettingsVM] SaveSelectedDpi: skipped (SelectedDPI={SelectedDPI})");
            }
        }

        private void RestoreSelectedDpi()
        {
            try
            {
                var scannerName = GetSelectedScannerName();
                if (string.IsNullOrWhiteSpace(scannerName))
                {
                    Debug.WriteLine("[SettingsVM] RestoreSelectedDpi: skipped (scanner name is null/empty)");
                    return;
                }

                var dpiKey = $"{LastSelectedDpiKeyPrefix}{scannerName}";
                if (!App.Current.Properties.Contains(dpiKey))
                {
                    Debug.WriteLine($"[SettingsVM] RestoreSelectedDpi: no saved DPI for key='{dpiKey}'");
                    return;
                }

                var dpiValue = App.Current.Properties[dpiKey];
                Debug.WriteLine($"[SettingsVM] RestoreSelectedDpi: found raw value='{dpiValue ?? "<null>"}' for key='{dpiKey}'");

                var savedDpi = dpiValue is int dpiInt
                    ? dpiInt
                    : Convert.ToInt32(dpiValue?.ToString());
                Debug.WriteLine($"[SettingsVM] RestoreSelectedDpi: parsed saved DPI={savedDpi}");

                if (DPIList.Contains(savedDpi))
                {
                    Debug.WriteLine($"[SettingsVM] RestoreSelectedDpi: applying saved DPI {savedDpi}");
                    SelectedDPI = savedDpi;
                }
                else
                {
                    Debug.WriteLine($"[SettingsVM] RestoreSelectedDpi: saved DPI {savedDpi} not in current list");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SettingsVM] RestoreSelectedDpi exception: {ex}");
            }
        }

        private void RestoreSelectedScanner()
        {
            if (!App.Current.Properties.Contains(LastSelectedScannerNameKey))
            {
                Debug.WriteLine("[SettingsVM] RestoreSelectedScanner: no saved scanner name");
                return;
            }

            var scannerName = App.Current.Properties[LastSelectedScannerNameKey]?.ToString();
            if (string.IsNullOrWhiteSpace(scannerName))
            {
                Debug.WriteLine("[SettingsVM] RestoreSelectedScanner: saved scanner is null/empty");
                return;
            }

            if (ScannerList.TryGetValue(scannerName, out var scanner))
            {
                Debug.WriteLine($"[SettingsVM] RestoreSelectedScanner: restoring '{scannerName}'");
                SelectedScannerName = scannerName;
            }
            else
            {
                Debug.WriteLine($"[SettingsVM] RestoreSelectedScanner: saved scanner '{scannerName}' not found in current list");
            }
        }

        private void SaveAutoCropStrength()
        {
            if (!string.IsNullOrWhiteSpace(SelectedAutoCropStrength))
            {
                App.Current.Properties[AutoCropStrengthKey] = SelectedAutoCropStrength;
            }
        }

        private void RestoreAutoCropStrength()
        {
            if (App.Current.Properties.Contains(AutoCropStrengthKey))
            {
                var savedStrength = App.Current.Properties[AutoCropStrengthKey]?.ToString();
                if (!string.IsNullOrWhiteSpace(savedStrength) && AutoCropStrengthOptions.Contains(savedStrength))
                {
                    SelectedAutoCropStrength = savedStrength;
                    return;
                }
            }

            SelectedAutoCropStrength = "Medium";
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

        public ICommand RefreshScannersCommand =>
            _refreshScannersCommand ??= new DelegateCommand(PopulateScanners);

        public SettingsViewModel(AppConfig appConfig, IThemeSelectorService themeSelectorService, ISystemService systemService, IApplicationInfoService applicationInfoService)
        {
            _appConfig = appConfig;
            _themeSelectorService = themeSelectorService;
            _systemService = systemService;
            _applicationInfoService = applicationInfoService;
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            Debug.WriteLine("[SettingsVM] OnNavigatedTo");
            VersionDescription = $"SimplePhotoEditor - {_applicationInfoService.GetVersion()}";
            Theme = _themeSelectorService.GetCurrentTheme();
            PopulateScanners();
            RestoreAutoCropStrength();
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            Debug.WriteLine("[SettingsVM] OnNavigatedFrom");
            SaveSelectedScannerName();
            SaveSelectedDpi();
            SaveAutoCropStrength();
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
