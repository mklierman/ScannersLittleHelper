using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.WindowsAPICodePack.Shell;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Services.Dialogs;
using SimplePhotoEditor.Constants;
using SimplePhotoEditor.Helpers;
using SimplePhotoEditor.Models;
using SimplePhotoEditor.Views;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SimplePhotoEditor.ViewModels
{
    public class MetadataViewModel : BindableBase
    {
        private bool creatingNewDir = false;
        private bool focusOnFileName;
        private bool isSaving = false;
        private DateTime? dateTaken;
        private ICommand addTagCommand;
        private ICommand cancelCommand;
        private ICommand changeRootFolderCommand;
        private ICommand removeTagCommand;
        private ICommand saveCommand;
        private ICommand saveNextCommand;
        private IDialogCoordinator dialogCoordinator = new DialogCoordinator();
        private IDialogService DialogService;
        private IRegionManager regionManager;
        private ObservableCollection<string> saveToFolderOptions = new ObservableCollection<string>();
        private ObservableCollection<string> tags = new ObservableCollection<string>();
        private SingleImageViewModel SingleImageViewModel;
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
        private ThumbnailViewModel ThumbnailViewModel;

        public MetadataViewModel(IRegionManager regionManager, IDialogService dialogService, string callingPage = "")
        {
            this.regionManager = regionManager;
            DialogService = dialogService;
            CallingPage = callingPage;
        }

        public ICommand AddTagCommand => addTagCommand ?? (addTagCommand = new DelegateCommand(AddTag));
        public ICommand CancelCommand => cancelCommand ?? (cancelCommand = new DelegateCommand(GetMetadata));
        public ICommand ChangeRootFolderCommand => changeRootFolderCommand ??= new DelegateCommand(ChangeRootFolder);

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

        public DateTime? DateTaken
        {
            get => dateTaken;
            set => SetProperty(ref dateTaken, value);
        }

        public string FileName
        {
            get => fileName;
            set => SetProperty(ref fileName, value);
        }

        public string FilePath
        {
            get => filePath;
            set
            {
                if (filePath != value)
                {
                    filePath = value;
                    if (value != null)
                    {
                        GetMetadata();
                        if (string.IsNullOrEmpty(SaveToRootFolder))
                        {
                            GetSaveToFolderOptions();
                        }
                        FocusOnFileName = true;
                    }
                    RaisePropertyChanged(nameof(FilePath));
                }
            }
        }

        public bool FocusOnFileName { get => focusOnFileName; set => SetProperty(ref focusOnFileName, value); }
        public bool IsSaving { get => isSaving; set => SetProperty(ref isSaving, value); }

        public string CallingPage { get => callingPage; set => SetProperty(ref callingPage, value); }
        public ICommand RemoveTagCommand => removeTagCommand ?? (removeTagCommand = new DelegateCommand(RemoveTag));
        public ICommand SaveCommand => saveCommand ?? (saveCommand = new DelegateCommand<string>(OnSave));
        public ICommand SaveNextCommand => saveNextCommand ?? (saveNextCommand = new DelegateCommand<string>(OnSave));
        public ObservableCollection<string> SaveToFolderOptions { get => saveToFolderOptions; set => SetProperty(ref saveToFolderOptions, value); }

        public string SaveToRootFolder { get => saveToRootFolder; set => SetProperty(ref saveToRootFolder, value); }

        public string SelectedSaveToFolder
        {
            get => selectedSaveToFolder; set
            {
                SetProperty(ref selectedSaveToFolder, value);
                if (SaveToRootFolder == null)
                {
                    SaveToRootFolder = SelectedSaveToFolder;
                }
                if (!creatingNewDir && string.Equals(SelectedSaveToFolder, "[Create New Directory]", StringComparison.OrdinalIgnoreCase))
                {
                    creatingNewDir = true;
                    CreateNewDirectory();
                    creatingNewDir = false;
                }
            }
        }

        public string SelectedTag
        {
            get => selectedTag; set
            {
                SetProperty(ref selectedTag, value);
                Tag = SelectedTag;
            }
        }

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

        public void AddTag()
        {
            if (Tags?.Contains(Tag) != true)
            {
                Tags.Add(Tag);
            }
        }

        public void RemoveTag()
        {
            if (Tags?.Contains(Tag) == true)
            {
                Tags.Remove(tag);
            }
        }

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

        private void CreateNewDirectory()
        {
            var newDirName = "";
            var dialogParams = new DialogParameters
                    {
                        { "Message", "Enter the new Directory name:" },
                        { "Input", "" },
                        { "AffirmativeButtonText", "Accept" },
                        { "NegativeButtonText", "Cancel"},
                        { "Title", "Create New Directory" }
                    };
            //New popup
            DialogService.ShowDialog("InputDialog", dialogParams, r =>
            {
                if (r.Result == ButtonResult.OK)
                    newDirName = r.Parameters.GetValue<string>("UserInput");
                else
                    return;
            });
            if (!string.IsNullOrEmpty(newDirName))
            {
                var root = SaveToRootFolder ?? Path.GetDirectoryName(FilePath);
                DirectoryInfo directoryInfo = new DirectoryInfo(root);
                var newPath = directoryInfo.CreateSubdirectory(newDirName).FullName;
                if (!SaveToFolderOptions.Contains(newPath))
                {
                    SaveToFolderOptions.Insert(SaveToFolderOptions.Count - 1, newPath); //Keep New Option at bottom
                    OrderSaveToFolderOptions();
                }
                OrderSaveToFolderOptions();
                SelectedSaveToFolder = newPath;
            }
        }

        private void GetMetadata()
        {
            ShellFile shellFile = ShellFile.FromFilePath(FilePath);
            FileName = Path.GetFileNameWithoutExtension(FilePath);
            Title = shellFile.Properties.System.Title.Value;
            Subject = shellFile.Properties.System.Subject.Value;
            Comment = shellFile.Properties.System.Comment.Value;
            var dateTakenValue = shellFile.Properties.System.Photo.DateTaken.Value;
            DateTaken = dateTakenValue == null ? dateTakenValue : (DateTime?)Convert.ToDateTime(dateTakenValue);
            string[] tagsArray = shellFile.Properties.System.Keywords.Value;
            if (tagsArray != null)
            {
                Tags = new ObservableCollection<string>(tagsArray);
            }
            else
            {
                Tags = null;
            }
        }

        private async Task<string> GetNewSaveToFolderAsync()
        {
            var result = await Task.Run(async () =>
            {
                var thumbnailView = (MetroWindow)regionManager.Regions[Regions.Main].GetView(PageKeys.Thumbnail);
                // var metroWindow = (Application.Current.MainWindow as MetroWindow);
                return await thumbnailView.ShowInputAsync("Create New Directory", "Enter the new directory name:");
            });
            return result;
        }

        private void GetSaveToFolderOptions(string rootFolder = "")
        {
            SaveToFolderOptions.Clear();
            DirectoryInfo directoryInfo;
            if (rootFolder == string.Empty)
            {
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
            SelectedSaveToFolder = directoryInfo.FullName;
        }

        private void GetThumbnailViewModel()
        {
            var thumbnailView = (ThumbnailPage)regionManager.Regions[Regions.Main].GetView(PageKeys.Thumbnail);
            ThumbnailViewModel = (ThumbnailViewModel)thumbnailView.DataContext;
        }

        private async void OnSave(string GoToNextImage = "")
        {
            ImageInfo dataToSave = new ImageInfo();
            dataToSave.SelectedSaveToFolder = SelectedSaveToFolder;
            dataToSave.DateTaken = DateTaken;
            dataToSave.Comment = Comment;
            dataToSave.FileName = FileName + Path.GetExtension(FilePath);
            dataToSave.FilePath = FilePath;
            dataToSave.NewFilePath = SelectedSaveToFolder + "\\" + FileName + Path.GetExtension(FilePath);
            dataToSave.Subject = Subject;
            dataToSave.Tags = Tags;
            dataToSave.Title = Title;

            IsSaving = true;
            _ = Task.Run(async () => await SaveImage(dataToSave));
            UpdateThumbnailDetails(GoToNextImage, dataToSave);
            if (CallingPage == PageKeys.SingleImage)
            {
                UpdateSingleImageDetails(GoToNextImage, dataToSave);
            }
            IsSaving = false;
        }

        private void GetSingleImageViewModel()
        {
            var singleImageView = (SingleImagePage)regionManager.Regions[Regions.Main].GetView(PageKeys.SingleImage);
            SingleImageViewModel = (SingleImageViewModel)singleImageView.DataContext;
        }

        private void OrderSaveToFolderOptions()
        {
            var newDir = SaveToFolderOptions[SaveToFolderOptions.Count - 1];
            var root = SaveToRootFolder ?? Path.GetDirectoryName(FilePath);
            SaveToFolderOptions.RemoveAt(SaveToFolderOptions.Count - 1); //Create new dir option
            SaveToFolderOptions.RemoveAt(0); //Root
            SaveToFolderOptions.Sort(o => o);
            SaveToFolderOptions.Insert(0, root);
            SaveToFolderOptions.Add(newDir);
        }

        private async Task SaveImage(ImageInfo data)
        {
            ShellFile shellFile = ShellFile.FromFilePath(data.FilePath);

            if (data.Title != shellFile.Properties.System.Title.Value)
            {
                shellFile.Properties.System.Title.Value = data.Title;
            }
            if (data.Subject != shellFile.Properties.System.Subject.Value)
            {
                shellFile.Properties.System.Subject.Value = data.Subject;
            }
            if (data.Comment != shellFile.Properties.System.Comment.Value)
            {
                shellFile.Properties.System.Comment.Value = data.Comment;
            }
            if (data.DateTaken != Convert.ToDateTime(shellFile.Properties.System.Photo.DateTaken.Value))
            {
                shellFile.Properties.System.Photo.DateTaken.Value = data.DateTaken;
            }
            if (data.Tags?.Count > 0)
            {
                string[] tagsArray = new string[data.Tags.Count];
                data.Tags?.CopyTo(tagsArray, 0);
                if (tagsArray != shellFile.Properties.System.Keywords.Value && tagsArray?.Length > 0)
                {
                    shellFile.Properties.System.Keywords.Value = tagsArray;
                }
            }
            shellFile.Dispose();

            if (data.FileName != Path.GetFileName(data.FilePath) || Path.GetDirectoryName(data.FilePath) != data.SelectedSaveToFolder)
            {
                var newPath = data.SelectedSaveToFolder + "\\" + data.FileName;
                File.Move(data.FilePath, newPath);
                if (File.Exists(newPath))
                {
                    data.FilePath = newPath;
                }
                else
                {
                    //Something went wrong.
                }
            }
        }

        private void UpdateThumbnailDetails(string GoToNextImage, ImageInfo data)
        {
            GetThumbnailViewModel();
            var currentImageIndex = ThumbnailViewModel.Images.IndexOf(ThumbnailViewModel.SelectedImage);
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

        private int maxFileNameLength;

        public int MaxFileNameLength { get => maxFileNameLength; set => SetProperty(ref maxFileNameLength, value); }
    }
}