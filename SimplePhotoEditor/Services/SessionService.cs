using SimplePhotoEditor.Contracts.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplePhotoEditor.Services
{
    public class SessionService : ISessionService
    {
        private string currentImagePath;
        public string CurrentImagePath
        {
            get => currentImagePath;
            set => currentImagePath = value;
        }

        private string currentFolder;
        public string CurrentFolder
        {
            get => currentFolder;
            set => currentFolder = value;
        }

        private string currentTempFilePath;
        public string CurrentTempFilePath
        {
            get => currentTempFilePath;
            set => currentTempFilePath = value;
        }

        private string previousView;
        public string PeviousView
        {
            get => previousView;
            set => previousView = value;
        }
    }
}
