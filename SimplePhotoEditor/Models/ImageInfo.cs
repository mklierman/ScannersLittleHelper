using System;
using System.Collections.ObjectModel;

namespace SimplePhotoEditor.Models
{
    public class ImageInfo
    {
        public string SelectedSaveToFolder;
        public DateTime? DateTaken;
        public string Comment;
        public string FileName;
        public string FilePath;
        public string NewFilePath;
        public string Subject;
        public ObservableCollection<string> Tags;
        public string Title;
    }
}