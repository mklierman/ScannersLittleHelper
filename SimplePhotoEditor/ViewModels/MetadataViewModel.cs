using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Services.Dialogs;
using SimplePhotoEditor.Constants;
using SimplePhotoEditor.Contracts.Services;
using SimplePhotoEditor.Helpers;
using SimplePhotoEditor.Models;
using SimplePhotoEditor.Views;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Diagnostics;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Png;
using Directory = System.IO.Directory;  // Explicitly use System.IO.Directory to resolve ambiguity
using System.Collections.Generic;
using System.Windows;

namespace SimplePhotoEditor.ViewModels
{
    /// <summary>
    /// ViewModel responsible for managing image metadata including title, subject, comments, tags, and file organization.
    /// Handles saving metadata to image files and managing file locations.
    /// </summary>
    public class MetadataViewModel : BindableBase
    {
        private const string LastSaveToRootFolderKey = "LastSaveToRootFolder";
        private const string LastSelectedSaveToFolderKey = "LastSelectedSaveToFolder";

        #region Private Fields
        // UI State
        private bool creatingNewDir = false;
        private bool focusOnFileName;
        private bool isSaving = false;
        private int maxFileNameLength = 230; // Default value for Windows path length limit
        private bool isPngFile;

        // Commands
        private ICommand addTagCommand;
        private ICommand cancelCommand;
        private ICommand changeRootFolderCommand;
        private ICommand removeTagCommand;
        private ICommand saveCommand;
        private ICommand saveNextCommand;

        // Services
        private IDialogService DialogService;
        private IRegionManager regionManager;
        private ISessionService sessionService;
        private readonly IToastService toastService;

        // Collections
        private ObservableCollection<string> saveToFolderOptions = new ObservableCollection<string>();
        private ObservableCollection<string> tags = new ObservableCollection<string>();

        // View Models
        private SingleImageViewModel SingleImageViewModel;
        private ScanViewModel ScanViewModel;
        private ThumbnailViewModel ThumbnailViewModel;

        // Data Properties
        private string callingPage;
        private string comment;
        private string fileName;
        private string filePath;
        private string saveToRootFolder;
        private string selectedSaveToFolder;
        private string selectedTag;
        private string subject;
        private string tag;
        private string title;
        private DateTime? dateTaken;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the MetadataViewModel.
        /// </summary>
        /// <param name="regionManager">The region manager for navigation.</param>
        /// <param name="dialogService">The dialog service for showing dialogs.</param>
        /// <param name="sessionService">The session service for maintaining application state.</param>
        /// <param name="toastService">Service for non-blocking save confirmations.</param>
        public MetadataViewModel(IRegionManager regionManager, IDialogService dialogService, ISessionService sessionService, IToastService toastService)
        {
            this.regionManager = regionManager;
            DialogService = dialogService;
            this.sessionService = sessionService;
            this.toastService = toastService;
            Tags = new ObservableCollection<string>();
            SaveToFolderOptions = new ObservableCollection<string>();
        }
        #endregion

        #region Commands
        /// <summary>
        /// Command to add a new tag to the image.
        /// </summary>
        public ICommand AddTagCommand => addTagCommand ?? (addTagCommand = new DelegateCommand(AddTag));

        /// <summary>
        /// Command to cancel metadata changes and reload original metadata.
        /// </summary>
        public ICommand CancelCommand => cancelCommand ?? (cancelCommand = new DelegateCommand(GetMetadata));

        /// <summary>
        /// Command to change the root folder for saving images.
        /// </summary>
        public ICommand ChangeRootFolderCommand => changeRootFolderCommand ??= new DelegateCommand(ChangeRootFolder);

        /// <summary>
        /// Command to remove a selected tag from the image.
        /// </summary>
        public ICommand RemoveTagCommand => removeTagCommand ?? (removeTagCommand = new DelegateCommand(RemoveTag));

        /// <summary>
        /// Command to save the current metadata changes.
        /// </summary>
        public ICommand SaveCommand => saveCommand ?? (saveCommand = new DelegateCommand<string>(OnSave));

        /// <summary>
        /// Command to save the current metadata and move to the next image.
        /// </summary>
        public ICommand SaveNextCommand => saveNextCommand ?? (saveNextCommand = new DelegateCommand<string>(OnSave));
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the comment for the image.
        /// </summary>
        public string Comment
        {
            get => comment;
            set
            {
                if (comment != value)
                {
                    comment = value;
                    RaisePropertyChanged(nameof(Comment));
                }
            }
        }

        /// <summary>
        /// Gets or sets the date when the image was taken.
        /// </summary>
        public DateTime? DateTaken
        {
            get => dateTaken;
            set => SetProperty(ref dateTaken, value);
        }

        /// <summary>
        /// Gets or sets the file name of the image.
        /// </summary>
        public string FileName
        {
            get => fileName;
            set => SetProperty(ref fileName, value);
        }

        /// <summary>
        /// Gets or sets the full file path of the image.
        /// </summary>
        public string FilePath
        {
            get => filePath;
            set
            {
                if (filePath != value)
                {
                    filePath = value;
                    if (!string.IsNullOrEmpty(value) && File.Exists(value))
                    {
                        try
                        {
                            // Check if file is PNG
                            IsPngFile = Path.GetExtension(value).ToLower() == ".png";
                            
                        GetMetadata();
                        if (string.IsNullOrEmpty(SaveToRootFolder))
                        {
                            GetSaveToFolderOptions();
                        }
                        FocusOnFileName = true;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error in FilePath setter: {ex.Message}");
                            var dialogParams = new DialogParameters
                            {
                                { "message", $"Failed to load metadata: {ex.Message}" }
                            };
                            Debug.WriteLine("Showing error dialog with parameters");
                            DialogService.ShowDialog("ErrorDialog", dialogParams, null);
                        }
                    }
                    RaisePropertyChanged(nameof(FilePath));
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the file name input should receive focus.
        /// </summary>
        public bool FocusOnFileName { get => focusOnFileName; set => SetProperty(ref focusOnFileName, value); }

        /// <summary>
        /// Gets or sets whether the metadata is currently being saved.
        /// </summary>
        public bool IsSaving { get => isSaving; set => SetProperty(ref isSaving, value); }

        /// <summary>
        /// Gets or sets the page that called this view model.
        /// </summary>
        public string CallingPage { get => callingPage; set => SetProperty(ref callingPage, value); }

        /// <summary>
        /// Gets or sets the available folder options for saving images.
        /// </summary>
        public ObservableCollection<string> SaveToFolderOptions { get => saveToFolderOptions; set => SetProperty(ref saveToFolderOptions, value); }

        /// <summary>
        /// Gets or sets the root folder for saving images.
        /// </summary>
        public string SaveToRootFolder { get => saveToRootFolder; set => SetProperty(ref saveToRootFolder, value); }

        /// <summary>
        /// Gets or sets the selected folder for saving the image.
        /// </summary>
        public string SelectedSaveToFolder
        {
            get => selectedSaveToFolder;
            set
            {
                SetProperty(ref selectedSaveToFolder, value);
                if (string.IsNullOrWhiteSpace(SelectedSaveToFolder))
                {
                    if (SaveToRootFolder == null)
                    {
                        MaxFileNameLength = 230;
                    }
                    PersistSaveFolderLocations();
                    if (string.Equals(CallingPage, PageKeys.Scan, StringComparison.Ordinal))
                    {
                        GetScanViewModel();
                        ScanViewModel?.RefreshScanReadinessWarnings();
                    }
                    return;
                }

                if (SaveToRootFolder == null)
                {
                    SaveToRootFolder = SelectedSaveToFolder;
                    MaxFileNameLength = 230 - SelectedSaveToFolder.Length;
                }
                if (!creatingNewDir && string.Equals(SelectedSaveToFolder, "[Create New Directory]", StringComparison.OrdinalIgnoreCase))
                {
                    creatingNewDir = true;
                    CreateNewDirectory();
                    creatingNewDir = false;
                }

                PersistSaveFolderLocations();
                if (string.Equals(CallingPage, PageKeys.Scan, StringComparison.Ordinal))
                {
                    GetScanViewModel();
                    ScanViewModel?.RefreshScanReadinessWarnings();
                }
            }
        }

        /// <summary>
        /// Gets or sets the currently selected tag.
        /// </summary>
        public string SelectedTag
        {
            get => selectedTag;
            set
            {
                SetProperty(ref selectedTag, value);
                Tag = SelectedTag;
            }
        }

        /// <summary>
        /// Gets or sets the subject of the image.
        /// </summary>
        public string Subject
        {
            get => subject;
            set
            {
                if (subject != value)
                {
                    subject = value;
                    RaisePropertyChanged(nameof(Subject));
                }
            }
        }

        /// <summary>
        /// Gets or sets the current tag input value.
        /// </summary>
        public string Tag
        {
            get => tag;
            set
            {
                if (tag != value)
                {
                    tag = value;
                    RaisePropertyChanged(nameof(Tag));
                }
            }
        }

        /// <summary>
        /// Gets or sets the collection of tags for the image.
        /// </summary>
        public ObservableCollection<string> Tags
        {
            get => tags;
            set
            {
                if (tags != value)
                {
                    tags = value;
                    RaisePropertyChanged(nameof(Tags));
                }
            }
        }

        /// <summary>
        /// Gets or sets the title of the image.
        /// </summary>
        public string Title
        {
            get => title;
            set
            {
                if (title != value)
                {
                    title = value;
                    RaisePropertyChanged(nameof(Title));
                }
            }
        }

        /// <summary>
        /// Gets or sets the maximum length allowed for file names.
        /// </summary>
        public int MaxFileNameLength
        {
            get => maxFileNameLength;
            set
            {
                if (value <= 0 || value > 230)
                {
                    value = 230; // Default to maximum safe length
                }
                SetProperty(ref maxFileNameLength, value);
            }
        }

        /// <summary>
        /// Gets or sets whether the current file is a PNG file.
        /// </summary>
        public bool IsPngFile
        {
            get => isPngFile;
            set => SetProperty(ref isPngFile, value);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Clears metadata fields for scan workflows where no file is selected yet.
        /// </summary>
        public void ClearMetadataForScan()
        {
            FilePath = null;
            FileName = string.Empty;
            Title = string.Empty;
            Subject = string.Empty;
            Comment = string.Empty;
            DateTaken = null;
            Tag = string.Empty;
            SelectedTag = null;
            Tags = new ObservableCollection<string>();
            TryRestorePersistedSaveFolders();
            FocusOnFileName = false;
        }

        /// <summary>
        /// Ensures a real save directory is selected for scanning; restores from persisted settings,
        /// or prompts for a folder if needed.
        /// </summary>
        public bool EnsureSaveToFolderReadyForScan()
        {
            if (IsUsableSaveToFolder(SelectedSaveToFolder))
            {
                return true;
            }

            TryRestorePersistedSaveFolders();
            if (IsUsableSaveToFolder(SelectedSaveToFolder))
            {
                return true;
            }

            MessageBox.Show(
                "Choose a folder where scans will be saved. You can pick one now, or cancel and use Save To Folder on the right.",
                "Save folder required",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            using var folderBrowser = new System.Windows.Forms.FolderBrowserDialog
            {
                ShowNewFolderButton = true,
                RootFolder = Environment.SpecialFolder.MyComputer
            };

            if (folderBrowser.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return false;
            }

            SaveToRootFolder = folderBrowser.SelectedPath;
            GetSaveToFolderOptions(SaveToRootFolder);
            PersistSaveFolderLocations();
            return IsUsableSaveToFolder(SelectedSaveToFolder);
        }

        /// <summary>
        /// Adds a new tag to the image's tag collection.
        /// </summary>
        public void AddTag()
        {
            if (string.IsNullOrWhiteSpace(Tag)) return;
            
            if (Tags == null)
            {
                Tags = new ObservableCollection<string>();
            }
            
            if (!Tags.Contains(Tag))
            {
                Tags.Add(Tag);
                Tag = string.Empty;  // Clear the input after adding
            }
        }

        /// <summary>
        /// Removes the selected tag from the image's tag collection.
        /// </summary>
        public void RemoveTag()
        {
            if (Tags == null || string.IsNullOrWhiteSpace(SelectedTag)) return;
            
            if (Tags.Contains(SelectedTag))
            {
                Tags.Remove(SelectedTag);
                SelectedTag = null;
            }
        }
        #endregion

        #region Private Methods
        private static bool IsUsableSaveToFolder(string path)
        {
            return !string.IsNullOrWhiteSpace(path) &&
                   !string.Equals(path, "[Create New Directory]", StringComparison.OrdinalIgnoreCase) &&
                   Directory.Exists(path);
        }

        private void PersistSaveFolderLocations()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(SaveToRootFolder) && Directory.Exists(SaveToRootFolder))
                {
                    App.Current.Properties[LastSaveToRootFolderKey] = SaveToRootFolder;
                }

                if (!string.IsNullOrWhiteSpace(SelectedSaveToFolder) &&
                    !string.Equals(SelectedSaveToFolder, "[Create New Directory]", StringComparison.OrdinalIgnoreCase) &&
                    Directory.Exists(SelectedSaveToFolder))
                {
                    App.Current.Properties[LastSelectedSaveToFolderKey] = SelectedSaveToFolder;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PersistSaveFolderLocations: {ex.Message}");
            }
        }

        private void TryRestorePersistedSaveFolders()
        {
            try
            {
                var root = App.Current.Properties.Contains(LastSaveToRootFolderKey)
                    ? App.Current.Properties[LastSaveToRootFolderKey]?.ToString()
                    : null;
                var preferred = App.Current.Properties.Contains(LastSelectedSaveToFolderKey)
                    ? App.Current.Properties[LastSelectedSaveToFolderKey]?.ToString()
                    : null;

                if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                {
                    SaveToRootFolder = null;
                    SaveToFolderOptions = new ObservableCollection<string>();
                    SelectedSaveToFolder = null;
                    MaxFileNameLength = 230;
                    return;
                }

                SaveToRootFolder = root;
                GetSaveToFolderOptions(root, preferred);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"TryRestorePersistedSaveFolders: {ex.Message}");
            }
        }

        /// <summary>
        /// Opens a folder browser dialog to change the root folder for saving images.
        /// </summary>
        private void ChangeRootFolder()
        {
            var folderBrowser = new System.Windows.Forms.FolderBrowserDialog
            {
                ShowNewFolderButton = true,
                RootFolder = Environment.SpecialFolder.MyComputer
            };
            if (folderBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SaveToRootFolder = folderBrowser.SelectedPath;
                GetSaveToFolderOptions(SaveToRootFolder);
            }
        }

        /// <summary>
        /// Creates a new directory for saving images.
        /// </summary>
        private void CreateNewDirectory()
        {
            var dialogParams = new DialogParameters
                    {
                        { "Message", "Enter the new Directory name:" },
                        { "Input", "" },
                        { "AffirmativeButtonText", "Accept" },
                        { "NegativeButtonText", "Cancel"},
                        { "Title", "Create New Directory" }
                    };

            string newDirName = null;
            DialogService.ShowDialog("InputDialog", dialogParams, r =>
            {
                if (r.Result == ButtonResult.OK)
                {
                    newDirName = r.Parameters.GetValue<string>("UserInput");
                }
            });

            if (!string.IsNullOrWhiteSpace(newDirName))
            {
                try
            {
                var root = SaveToRootFolder ?? Path.GetDirectoryName(FilePath);
                DirectoryInfo directoryInfo = new DirectoryInfo(root);
                var newPath = directoryInfo.CreateSubdirectory(newDirName).FullName;
                    
                if (!SaveToFolderOptions.Contains(newPath))
                    {
                        SaveToFolderOptions.Insert(SaveToFolderOptions.Count - 1, newPath);
                        OrderSaveToFolderOptions();
                    }
                    
                    SelectedSaveToFolder = newPath;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error creating directory: {ex.Message}");
                    var errorParams = new DialogParameters
                    {
                        { "message", $"Failed to create directory: {ex.Message}" }
                    };
                    Debug.WriteLine("Showing error dialog with parameters");
                    DialogService.ShowDialog("ErrorDialog", errorParams, null);
                }
            }
        }

        /// <summary>
        /// For JPEG/TIFF, Windows Shell often reports <see cref="Subject"/> equal to <see cref="Title"/> when
        /// the EXIF Windows XP Subject tag (0x9C9F) is absent. Prefer that tag when present; otherwise
        /// treat Subject identical to Title as empty so the UI matches an unset subject.
        /// </summary>
        private static void ApplySubjectForDisplay(string filePath, string titleFromShell, ref string subjectFromShell)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            if (ext != ".jpg" && ext != ".jpeg" && ext != ".jpe" && ext != ".tif" && ext != ".tiff")
            {
                return;
            }

            try
            {
                var hasWinSubjectTag = false;
                string xpSubjectValue = null;

                foreach (var directory in ImageMetadataReader.ReadMetadata(filePath))
                {
                    if (!directory.ContainsTag(ExifDirectoryBase.TagWinSubject))
                    {
                        continue;
                    }

                    try
                    {
                        xpSubjectValue = directory.GetString(ExifDirectoryBase.TagWinSubject);
                        hasWinSubjectTag = true;
                        break;
                    }
                    catch
                    {
                        // Ignore a bad tag and keep scanning other directories.
                    }
                }

                if (hasWinSubjectTag)
                {
                    subjectFromShell = xpSubjectValue ?? string.Empty;
                    return;
                }

                if (!string.IsNullOrEmpty(titleFromShell) &&
                    string.Equals(subjectFromShell, titleFromShell, StringComparison.Ordinal))
                {
                    subjectFromShell = string.Empty;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApplySubjectForDisplay: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves metadata from the current image file.
        /// </summary>
        private void GetMetadata()
        {
            if (string.IsNullOrEmpty(FilePath) || !File.Exists(FilePath))
            {
                return;
            }

            try
            {
                using (var shellFile = ShellFile.FromFilePath(FilePath))
                {
                    FileName = Path.GetFileNameWithoutExtension(FilePath);
                    var titleFromShell = shellFile.Properties.System.Title.Value ?? string.Empty;
                    var subjectFromShell = shellFile.Properties.System.Subject.Value ?? string.Empty;
                    ApplySubjectForDisplay(FilePath, titleFromShell, ref subjectFromShell);
                    Title = titleFromShell;
                    Subject = subjectFromShell;
                    Comment = shellFile.Properties.System.Comment.Value ?? string.Empty;
                    
                    var dateTakenValue = shellFile.Properties.System.Photo.DateTaken.Value;
                    DateTaken = dateTakenValue == null ? null : (DateTime?)Convert.ToDateTime(dateTakenValue);
                    
                    string[] tagsArray = shellFile.Properties.System.Keywords.Value;
                    Tags = new ObservableCollection<string>(tagsArray ?? Array.Empty<string>());
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't throw to prevent UI disruption
                System.Diagnostics.Debug.WriteLine($"Error loading metadata: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the thumbnail view model from the region manager.
        /// </summary>
        private void GetThumbnailViewModel()
        {
            if (regionManager?.Regions[Regions.Main] == null) return;
            
            var thumbnailView = regionManager.Regions[Regions.Main].GetView(PageKeys.Thumbnail) as ThumbnailPage;
            if (thumbnailView?.DataContext is ThumbnailViewModel viewModel)
            {
                ThumbnailViewModel = viewModel;
            }
        }

        /// <summary>
        /// Gets the single image view model from the region manager.
        /// </summary>
        private void GetSingleImageViewModel()
        {
            if (regionManager?.Regions[Regions.Main] == null) return;
            
            var singleImageView = regionManager.Regions[Regions.Main].GetView(PageKeys.SingleImage) as SingleImagePage;
            if (singleImageView?.DataContext is SingleImageViewModel viewModel)
            {
                SingleImageViewModel = viewModel;
            }
        }

        private void GetScanViewModel()
        {
            if (regionManager?.Regions[Regions.Main] == null) return;

            var scanView = regionManager.Regions[Regions.Main].GetView(PageKeys.Scan) as ScanPage;
            if (scanView?.DataContext is ScanViewModel viewModel)
            {
                ScanViewModel = viewModel;
            }
        }

        /// <summary>
        /// Handles the save operation for the current image's metadata.
        /// </summary>
        /// <param name="GoToNextImage">Indicates whether to move to the next image after saving.</param>
        private void OnSave(string GoToNextImage = "")
        {
            if (IsSaving) return;
            
            try
            {
            IsSaving = true;
                var dataToSave = new ImageInfo
                {
                    SelectedSaveToFolder = SelectedSaveToFolder,
                    DateTaken = DateTaken,
                    Comment = Comment,
                    FileName = FileName + Path.GetExtension(FilePath),
                    FilePath = FilePath,
                    NewFilePath = Path.Combine(SelectedSaveToFolder, FileName + Path.GetExtension(FilePath)),
                    Subject = Subject,
                    Tags = Tags,
                    Title = Title
                };

                if (!SaveImage(dataToSave))
                {
                    return;
                }

                toastService.Show($"Saved to {dataToSave.NewFilePath}");

                UpdateThumbnailDetails(GoToNextImage, dataToSave);
                if (CallingPage == PageKeys.SingleImage)
                {
                    UpdateSingleImageDetails(GoToNextImage, dataToSave);
                }
            }
            finally
            {
            IsSaving = false;
        }
        }

        /// <summary>
        /// Saves the image metadata to the file.
        /// </summary>
        /// <param name="data">The metadata to save.</param>
        /// <returns>True if the file was written; false if the user cancelled overwrite or save failed.</returns>
        private bool SaveImage(ImageInfo data)
        {
            try
            {
                if (string.IsNullOrEmpty(FilePath) || !File.Exists(FilePath))
                {
                    throw new FileNotFoundException("Source file not found", FilePath);
                }

                // Ensure the target directory exists
                Directory.CreateDirectory(SelectedSaveToFolder);

                var newFilePath = Path.Combine(SelectedSaveToFolder, FileName + Path.GetExtension(FilePath));
                if (File.Exists(newFilePath) && newFilePath != FilePath)
                {
                    var shouldOverwrite = false;
                    var dialogParams = new DialogParameters
                    {
                        { "Message", $"A file named '{FileName}' already exists in the destination folder. Do you want to replace it?" },
                        { "Title", "File already exists" },
                        { "AffirmativeButtonText", "Yes" },
                        { "NegativeButtonText", "No" }
                    };

                    DialogService.ShowDialog("ConfirmationDialog", dialogParams, r =>
                    {
                        shouldOverwrite = r.Result == ButtonResult.OK;
                    });

                    if (!shouldOverwrite)
                    {
                        return false;
                    }
                }

                // Prefer in-memory bytes from the single-image editor when present; otherwise from the
                // scan page (same issue as SingleImage: preview may exist while byte[] was never set).
                GetSingleImageViewModel();
                var singleBytes = SingleImageViewModel?.CurrentImageBytes;
                if (singleBytes != null && singleBytes.Length > 0)
                {
                    File.WriteAllBytes(newFilePath, singleBytes);
                }
                else if (CallingPage == PageKeys.Scan)
                {
                    GetScanViewModel();
                    var scanBytes = ScanViewModel?.CurrentImageBytes;
                    if (scanBytes != null && scanBytes.Length > 0)
                    {
                        File.WriteAllBytes(newFilePath, scanBytes);
                    }
                    else
                    {
                        File.Copy(FilePath, newFilePath, true);
                    }
                }
                else
                {
                    File.Copy(FilePath, newFilePath, true);
                }

                string extension = Path.GetExtension(FilePath).ToLower();
                if (extension == ".png")
                {
                    SavePngMetadata(data, newFilePath);
                }
                else
                {
                    // Save metadata to the new file using Windows Shell API
                    using (var shellFile = ShellFile.FromFilePath(newFilePath))
                    {
                        try
                        {
                            // Try to set each property individually and catch specific failures
                            if (!string.IsNullOrEmpty(data.Title))
                            {
                                try 
                                { 
                                    shellFile.Properties.System.Title.Value = data.Title;
                                }
                                catch (Exception ex) 
                                { 
                                    Debug.WriteLine($"Failed to set Title: {ex.Message}"); 
                                }
                            }

                            // Always write Subject (including empty). Windows may mirror Title into Subject
                            // when XP/EXIF subject is unset; skipping this block leaves that behavior.
                            try
                            {
                                shellFile.Properties.System.Subject.Value = data.Subject ?? string.Empty;
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Failed to set Subject: {ex.Message}");
                            }

                            if (!string.IsNullOrEmpty(data.Comment))
                            {
                                try 
                                { 
                                    shellFile.Properties.System.Comment.Value = data.Comment;
                                }
                                catch (Exception ex) 
                                { 
                                    Debug.WriteLine($"Failed to set Comment: {ex.Message}"); 
                                }
                            }

                            if (data.DateTaken.HasValue)
                            {
                                try 
                                { 
                                    shellFile.Properties.System.Photo.DateTaken.Value = data.DateTaken.Value;
                                }
                                catch (Exception ex) 
                                { 
                                    Debug.WriteLine($"Failed to set DateTaken: {ex.Message}"); 
                                }
                            }

                            if (data.Tags != null && data.Tags.Any())
                            {
                                var tagsArray = data.Tags.ToArray();
                                try 
                                { 
                                    shellFile.Properties.System.Keywords.Value = tagsArray;
                                }
                                catch (Exception ex) 
                                { 
                                    Debug.WriteLine($"Failed to set Tags: {ex.Message}"); 
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error saving metadata: {ex.Message}");
                            throw new Exception("Unable to save metadata. The file format might not support all metadata properties.", ex);
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                var dialogParams = new DialogParameters
                {
                    { "Message", $"Failed to save image: {ex.Message}" },
                    { "Title", "Save Error" }
                };
                DialogService.ShowDialog("ErrorDialog", dialogParams, null);
                return false;
            }
        }

        private void SavePngMetadata(ImageInfo data, string filePath)
        {
            try
            {
                Debug.WriteLine($"Starting to save PNG metadata to: {filePath}");
                
                // Read the PNG file into memory
                byte[] pngBytes = File.ReadAllBytes(filePath);
                Debug.WriteLine($"Read {pngBytes.Length} bytes from PNG file");

                // Create a memory stream to work with the PNG data
                using (var ms = new MemoryStream(pngBytes))
                {
                    // Create a PNG metadata writer
                    var writer = new PngMetadataWriter();

                    // Only save Date Taken for PNG files
                    if (data.DateTaken.HasValue)
                    {
                        Debug.WriteLine($"Setting Creation Time: {data.DateTaken.Value}");
                        writer.SetCreationTime(data.DateTaken.Value);
                    }

                    // Write the updated PNG data back to the file
                    using (var fs = new FileStream(filePath, FileMode.Create))
                    {
                        writer.WriteMetadata(ms, fs);
                        Debug.WriteLine("Successfully wrote metadata to PNG file");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving PNG metadata: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw new Exception($"Unable to save PNG metadata: {ex.Message}", ex);
            }
        }

        private class PngMetadataWriter
        {
            private readonly Dictionary<string, string> textChunks = new Dictionary<string, string>();
            private DateTime? creationTime;

            public void SetTitle(string title)
            {
                textChunks["Title"] = title;
            }

            public void SetDescription(string description)
            {
                textChunks["Description"] = description;
            }

            public void SetComment(string comment)
            {
                textChunks["Comment"] = comment;
            }

            public void SetCreationTime(DateTime time)
            {
                creationTime = time;
            }

            public void SetKeywords(string keywords)
            {
                textChunks["Keywords"] = keywords;
            }

            public void WriteMetadata(Stream input, Stream output)
            {
                try
                {
                    using (var reader = new BinaryReader(input))
                    using (var writer = new BinaryWriter(output))
                    {
                        // Write PNG signature
                        var signature = reader.ReadBytes(8);
                        if (signature.Length != 8 || signature[0] != 0x89 || signature[1] != 0x50 || 
                            signature[2] != 0x4E || signature[3] != 0x47 || signature[4] != 0x0D || 
                            signature[5] != 0x0A || signature[6] != 0x1A || signature[7] != 0x0A)
                        {
                            throw new Exception("Invalid PNG signature");
                        }
                        writer.Write(signature);

                        // Read and write chunks
                        while (input.Position < input.Length)
                        {
                            // Read chunk header
                            var length = ReadInt32BigEndian(reader);
                            var type = new string(reader.ReadChars(4));
                            var data = reader.ReadBytes(length);
                            var crc = reader.ReadBytes(4);

                            // Skip existing text chunks
                            if (type == "tEXt" || type == "iTXt" || type == "zTXt")
                            {
                                Debug.WriteLine($"Skipping existing {type} chunk");
                                continue;
                            }

                            // Write the chunk
                            WriteChunk(writer, type, data);
                        }

                        // Write our text chunks
                        foreach (var chunk in textChunks)
                        {
                            Debug.WriteLine($"Writing {chunk.Key} chunk: {chunk.Value}");
                            byte[] textData = System.Text.Encoding.UTF8.GetBytes($"{chunk.Key}\0{chunk.Value}");
                            WriteChunk(writer, "tEXt", textData);
                        }

                        // Write creation time if set
                        if (creationTime.HasValue)
                        {
                            var timeStr = creationTime.Value.ToString("yyyy:MM:dd HH:mm:ss");
                            Debug.WriteLine($"Writing Creation Time chunk: {timeStr}");
                            byte[] timeData = System.Text.Encoding.UTF8.GetBytes($"Creation Time\0{timeStr}");
                            WriteChunk(writer, "tEXt", timeData);
                        }

                        Debug.WriteLine("Successfully wrote all chunks");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in WriteMetadata: {ex.Message}");
                    Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    throw;
                }
            }

            private static int ReadInt32BigEndian(BinaryReader reader)
            {
                var bytes = reader.ReadBytes(4);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(bytes);
                }
                return BitConverter.ToInt32(bytes, 0);
            }

            private static void WriteChunk(BinaryWriter writer, string type, byte[] data)
            {
                // Write length
                var lengthBytes = BitConverter.GetBytes(data.Length);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(lengthBytes);
                }
                writer.Write(lengthBytes);

                // Write type
                writer.Write(System.Text.Encoding.ASCII.GetBytes(type));

                // Write data
                writer.Write(data);

                // Calculate and write CRC
                var crc = CalculateCrc(type, data);
                var crcBytes = BitConverter.GetBytes(crc);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(crcBytes);
                }
                writer.Write(crcBytes);
            }

            private static uint CalculateCrc(string type, byte[] data)
            {
                var crc = new Crc32();
                crc.Update(System.Text.Encoding.ASCII.GetBytes(type));
                crc.Update(data);
                return crc.Value;
            }
        }

        private class Crc32
        {
            private uint crc = 0xffffffff;
            private static readonly uint[] Table;

            static Crc32()
            {
                Table = new uint[256];
                for (uint i = 0; i < 256; i++)
                {
                    uint c = i;
                    for (int j = 0; j < 8; j++)
                    {
                        if ((c & 1) == 1)
                            c = 0xEDB88320 ^ (c >> 1);
                        else
                            c = c >> 1;
                    }
                    Table[i] = c;
                }
            }

            public void Update(byte[] buffer)
            {
                foreach (byte b in buffer)
                {
                    crc = Table[(crc ^ b) & 0xff] ^ (crc >> 8);
                }
            }

            public uint Value => ~crc;
        }

        /// <summary>
        /// Index of the image being saved in the thumbnail list, or -1 if it is not shown (e.g. scan temp file).
        /// </summary>
        private static int ResolveThumbnailIndex(ThumbnailViewModel vm, ImageInfo data)
        {
            if (vm?.Images == null || vm.Images.Count == 0)
            {
                return -1;
            }

            if (vm.SelectedImage != null)
            {
                var byRef = vm.Images.IndexOf(vm.SelectedImage);
                if (byRef >= 0)
                {
                    return byRef;
                }
            }

            if (!string.IsNullOrEmpty(data.FilePath))
            {
                for (var i = 0; i < vm.Images.Count; i++)
                {
                    if (string.Equals(vm.Images[i].FilePath, data.FilePath, StringComparison.OrdinalIgnoreCase))
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// Updates the thumbnail view with the latest metadata changes.
        /// </summary>
        private void UpdateThumbnailDetails(string GoToNextImage, ImageInfo data)
        {
            GetThumbnailViewModel();
            if (ThumbnailViewModel?.Images == null) return;

            var currentImageIndex = ResolveThumbnailIndex(ThumbnailViewModel, data);
            if (currentImageIndex < 0)
            {
                return;
            }

            if (ThumbnailViewModel.CurrentFolder != data.SelectedSaveToFolder)
            {
                ThumbnailViewModel.Images.RemoveAt(currentImageIndex);
                ThumbnailViewModel.SelectNextImage(currentImageIndex);
            }
            else
            {
                ThumbnailViewModel.Images[currentImageIndex].FileName = data.FileName;
                ThumbnailViewModel.Images[currentImageIndex].FilePath = data.NewFilePath;
                ThumbnailViewModel.Images[currentImageIndex].MetaDataModified =
                        !string.IsNullOrEmpty(data.Title) ||
                        !string.IsNullOrEmpty(data.Subject) ||
                        !string.IsNullOrEmpty(data.Comment) ||
                        data.DateTaken != default ||
                        data.Tags?.Count > 0;

                if (string.Equals(GoToNextImage, "true", StringComparison.OrdinalIgnoreCase))
                {
                    ThumbnailViewModel.SelectNextImage(currentImageIndex + 1);
                }
            }
        }

        /// <summary>
        /// Updates the single image view with the latest metadata changes.
        /// </summary>
        private void UpdateSingleImageDetails(string GoToNextImage, ImageInfo data)
        {
            if (SingleImageViewModel == null)
            {
                GetSingleImageViewModel();
            }
            SingleImageViewModel.FileName = data.FileName;
            SingleImageViewModel.FilePath = data.NewFilePath;

            if (string.Equals(GoToNextImage, "true", StringComparison.OrdinalIgnoreCase))
            {
                SingleImageViewModel.SelectNextImage();
            }
        }

        /// <summary>
        /// Orders the save to folder options alphabetically while keeping the root and create new directory options in place.
        /// </summary>
        private void OrderSaveToFolderOptions()
        {
            if (SaveToFolderOptions == null || SaveToFolderOptions.Count < 2)
            {
                return;
            }

            var newDir = SaveToFolderOptions[SaveToFolderOptions.Count - 1];
            var root = SaveToRootFolder;
            if (string.IsNullOrEmpty(root) && !string.IsNullOrEmpty(FilePath))
            {
                root = Path.GetDirectoryName(FilePath);
            }

            if (string.IsNullOrEmpty(root))
            {
                return;
            }

            SaveToFolderOptions.RemoveAt(SaveToFolderOptions.Count - 1);
            SaveToFolderOptions.RemoveAt(0);
            SaveToFolderOptions.Sort(o => o);
            SaveToFolderOptions.Insert(0, root);
            SaveToFolderOptions.Add(newDir);
        }

        /// <summary>
        /// Gets the available folder options for saving images.
        /// </summary>
        /// <param name="rootFolder">The root folder to start searching from.</param>
        /// <param name="preferredSelectedFolder">Optional folder to select if it still exists under the root.</param>
        private void GetSaveToFolderOptions(string rootFolder = "", string preferredSelectedFolder = null)
        {
            SaveToFolderOptions.Clear();
            DirectoryInfo directoryInfo;
            if (string.IsNullOrEmpty(rootFolder))
            {
                if (string.IsNullOrEmpty(FilePath))
                {
                    SaveToFolderOptions.Add("[Create New Directory]");
                    return;
                }

                directoryInfo = new DirectoryInfo(Path.GetDirectoryName(FilePath));
            }
            else
            {
                directoryInfo = new DirectoryInfo(rootFolder);
            }

            SaveToFolderOptions.Add(directoryInfo.FullName);
            var dirs = directoryInfo.GetDirectories();
            foreach (DirectoryInfo dir in dirs)
            {
                SaveToFolderOptions.Add(dir.FullName);
            }

            SaveToFolderOptions.Add("[Create New Directory]");
            OrderSaveToFolderOptions();

            if (!string.IsNullOrWhiteSpace(preferredSelectedFolder) && Directory.Exists(preferredSelectedFolder))
            {
                var normalized = Path.GetFullPath(preferredSelectedFolder);
                var match = SaveToFolderOptions.FirstOrDefault(o =>
                    !string.Equals(o, "[Create New Directory]", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(Path.GetFullPath(o), normalized, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    SelectedSaveToFolder = match;
                    return;
                }
            }

            SelectedSaveToFolder = directoryInfo.FullName;
        }

        #endregion
    }
}