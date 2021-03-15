using Microsoft.WindowsAPICodePack.Shell;
using Newtonsoft.Json.Linq;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using SimplePhotoEditor.Constants;
using SimplePhotoEditor.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Linq;

namespace SimplePhotoEditor.ViewModels
{
    public class MetadataViewModel : BindableBase
    {
        private ThumbnailViewModel ThumbnailViewModel;
        private SingleImageViewModel SingleImageViewModel;
        private IRegionManager regionManager;
        private string filePath;
        private string fileName;
        private string title;
        private string subject;
        private string comment;
        private bool isSaving = false;
        private bool focusOnFileName;
        private DateTime dateTaken;
        private string selectedTag;
        private ObservableCollection<string> tags = new ObservableCollection<string>();
        private string tag;
        private ICommand saveCommand;
        private ICommand saveNextCommand;
        private ICommand cancelCommand;
        private ICommand addTagCommand;
        private ICommand removeTagCommand;

        public ICommand CancelCommand => cancelCommand ?? (cancelCommand = new DelegateCommand(GetMetadata));
        public ICommand SaveCommand => saveCommand ?? (saveCommand = new DelegateCommand<string>(OnSave));
        public ICommand SaveNextCommand => saveNextCommand ?? (saveNextCommand = new DelegateCommand<string>(OnSave));
        public ICommand AddTagCommand => addTagCommand ?? (addTagCommand = new DelegateCommand(AddTag));
        public ICommand RemoveTagCommand => removeTagCommand ?? (removeTagCommand = new DelegateCommand(RemoveTag));
        public bool FocusOnFileName { get => focusOnFileName; set => SetProperty(ref focusOnFileName, value); }
        public string SelectedTag { get => selectedTag; set
            {
                SetProperty(ref selectedTag, value);
                Tag = SelectedTag;
            }
        }

        public MetadataViewModel(IRegionManager regionManager)
        {
            this.regionManager = regionManager;
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
        private void UpdateThumbnailDetails(ThumbnailViewModel thumbnailVM)
        {
            var currentImageIndex = thumbnailVM.Images.IndexOf(thumbnailVM.SelectedImage);
            thumbnailVM.Images[currentImageIndex].FileName = FileName;
            thumbnailVM.Images[currentImageIndex].FilePath = FilePath;
            thumbnailVM.Images[currentImageIndex].MetaDataModified =
                    !string.IsNullOrEmpty(Title) ||
                    !string.IsNullOrEmpty(Subject) ||
                    !string.IsNullOrEmpty(Comment) ||
                    DateTaken != default ||
                    Tags?.Count > 0;
        }

        private void GetMetadata()
        {
            ShellFile shellFile = ShellFile.FromFilePath(FilePath);
            FileName = Path.GetFileNameWithoutExtension(FilePath);
            Title = shellFile.Properties.System.Title.Value;
            Subject = shellFile.Properties.System.Subject.Value;
            Comment = shellFile.Properties.System.Comment.Value;
            DateTaken = Convert.ToDateTime(shellFile.Properties.System.Photo.DateTaken.Value);
            string[] tagsArray = shellFile.Properties.System.Photo.TagViewAggregate.Value;
            if (tagsArray != null)
            {
                Tags = new ObservableCollection<string>(tagsArray);
            }
        }

        public bool IsSaving { get => isSaving; set => SetProperty(ref isSaving, value); }
        private async void OnSave(string GoToNextImage = "")
        {
            IsSaving = true;
            await Task.Run(async () => await SaveImage());
            if (ThumbnailViewModel == null)
            {
                GetThumbnailViewModel();
            }
            UpdateThumbnailDetails(ThumbnailViewModel);
            if (string.Equals(GoToNextImage, "true", StringComparison.OrdinalIgnoreCase))
            {
                ThumbnailViewModel.SelectNextImage();
            }
            IsSaving = false;

        }

        private void GetThumbnailViewModel()
        {
            var thumbnailView = (ThumbnailPage)regionManager.Regions[Regions.Main].GetView(PageKeys.Thumbnail);
            ThumbnailViewModel = (ThumbnailViewModel)thumbnailView.DataContext;
        }

        private async Task SaveImage()
        {
            ShellFile shellFile = ShellFile.FromFilePath(FilePath);

            if (Title != shellFile.Properties.System.Title.Value)
            {
                shellFile.Properties.System.Title.Value = Title;
            }
            if (Subject != shellFile.Properties.System.Subject.Value)
            {
                shellFile.Properties.System.Subject.Value = Subject;
            }
            if (Comment != shellFile.Properties.System.Comment.Value)
            {
                shellFile.Properties.System.Comment.Value = Comment;
            }
            if (DateTaken != Convert.ToDateTime(shellFile.Properties.System.Photo.DateTaken.Value))
            {
                shellFile.Properties.System.Photo.DateTaken.Value = DateTaken;
            }
            //if (Tags?.Count > 0)
            //{
            //    string[] tagsArray = new string[Tags.Count];
            //    Tags?.CopyTo(tagsArray, 0);
            //    if (tagsArray != shellFile.Properties.System.Photo.TagViewAggregate.Value && tagsArray?.Length > 0)
            //    {
            //        shellFile.Properties.System.Photo.TagViewAggregate.Value = tagsArray;
            //    }
            //}
            shellFile.Dispose();


            if (FileName != Path.GetFileNameWithoutExtension(FilePath))
            {
                var newPath = Path.GetDirectoryName(FilePath) + "\\" + FileName + Path.GetExtension(FilePath);
                File.Move(FilePath, newPath);
                if (File.Exists(newPath))
                {
                    FilePath = newPath;
                }
                else
                {
                    //Something went wrong.
                }
            }
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
                    }
                    RaisePropertyChanged(nameof(FilePath));
                }
            }
        }

        public string FileName
        {
            get => fileName;
            set
            {
                if (fileName != value)
                {
                    fileName = value;
                    RaisePropertyChanged(nameof(FileName));
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

        public DateTime DateTaken
        {
            get => dateTaken;
            set
            {
                if (dateTaken != value)
                {
                    dateTaken = value;
                    RaisePropertyChanged(nameof(DateTaken));
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
    }
}
