using Microsoft.WindowsAPICodePack.Shell;
using Newtonsoft.Json.Linq;
using Prism.Commands;
using Prism.Mvvm;
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
        private string filePath;
        private string fileName;
        private string title;
        private string subject;
        private string comments;
        private bool isSaving = false;
        private DateTime dateTaken;
        private ObservableCollection<string> tags;
        private string tag;
        private ICommand saveCommand;
        private ICommand saveNextCommand;
        private ICommand cancelCommand;


        public ICommand CancelCommand => cancelCommand ?? (cancelCommand = new DelegateCommand(GetMetadata));
        public ICommand SaveCommand => saveCommand ?? (saveCommand = new DelegateCommand(OnSave));
        public ICommand SaveNextCommand => saveNextCommand ?? (saveNextCommand = new DelegateCommand(OnSave));

        private void GetMetadata()
        {
            ShellFile shellFile = ShellFile.FromFilePath(FilePath);
            FileName = Path.GetFileNameWithoutExtension(FilePath);
            Title = shellFile.Properties.System.Title.Value;
            Subject = shellFile.Properties.System.Subject.Value;
            Comments = shellFile.Properties.System.Comment.Value;
            DateTaken = Convert.ToDateTime(shellFile.Properties.System.Photo.DateTaken.Value);
            string[] tagsArray = shellFile.Properties.System.Photo.TagViewAggregate.Value;
            if (tagsArray != null)
            {
                Tags = new ObservableCollection<string>(tagsArray);
            }
        }

        public bool IsSaving { get => isSaving; set => SetProperty(ref isSaving, value); }

        private async void OnSave()
        {
            IsSaving = true;
            await Task.Run(async () => await SaveImage());
            IsSaving = false;
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
            if (Comments != shellFile.Properties.System.Comment.Value)
            {
                shellFile.Properties.System.Comment.Value = Comments;
            }
            if (DateTaken != Convert.ToDateTime(shellFile.Properties.System.Photo.DateTaken.Value))
            {
                shellFile.Properties.System.Photo.DateTaken.Value = DateTaken;
            }
            string[] tagsArray = null;
            Tags?.CopyTo(tagsArray, 0);
            if (tagsArray != shellFile.Properties.System.Photo.TagViewAggregate.Value)
            {
                shellFile.Properties.System.Photo.TagViewAggregate.Value = tagsArray;
            }
            shellFile.Dispose();


            if (FileName != Path.GetFileNameWithoutExtension(FilePath))
            {
                File.Move(FilePath, Path.GetDirectoryName(FilePath) + "\\" + FileName + Path.GetExtension(FilePath));
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
                    GetMetadata();
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

        public string Comments
        {
            get => comments;
            set
            {
                if (comments != value)
                {
                    comments = value;
                    RaisePropertyChanged(nameof(Comments));
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
