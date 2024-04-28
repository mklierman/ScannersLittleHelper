using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplePhotoEditor.Contracts.Services
{
    public interface ISessionService
    {
        string CurrentImagePath { get; set; }
        string CurrentFolder {  get; set; }
        string CurrentTempFilePath { get; set; }
        string PeviousView {  get; set; }
    }
}
