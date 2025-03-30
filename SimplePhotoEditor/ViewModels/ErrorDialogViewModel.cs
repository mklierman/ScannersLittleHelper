using System;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System.Diagnostics;

namespace SimplePhotoEditor.ViewModels
{
    public class ErrorDialogViewModel : BindableBase, IDialogAware
    {
        private string messageText;
        private DelegateCommand<string> closeDialogCommand;

        public ErrorDialogViewModel()
        {
            Debug.WriteLine("ErrorDialogViewModel constructor called");
            Message = "Initializing...";
        }

        public string Message
        {
            get => messageText;
            set
            {
                Debug.WriteLine($"Setting Message to: {value}");
                SetProperty(ref messageText, value);
            }
        }

        public DelegateCommand<string> CloseDialogCommand =>
            closeDialogCommand ??= new DelegateCommand<string>(CloseDialog);

        public string Title => "Error";

        public event Action<IDialogResult> RequestClose;

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
            Debug.WriteLine("OnDialogClosed called");
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            Debug.WriteLine("OnDialogOpened called");
            Debug.WriteLine($"Parameters count: {parameters.Count}");
            
            if (parameters.TryGetValue("message", out string message))
            {
                Debug.WriteLine($"Found message parameter: {message}");
                Message = message;
            }
            else if (parameters.TryGetValue("Message", out string messageCapital))
            {
                Debug.WriteLine($"Found Message parameter: {messageCapital}");
                Message = messageCapital;
            }
            else
            {
                Debug.WriteLine("No message parameter found in parameters");
                Message = "No message provided";
            }
        }

        private void CloseDialog(string result)
        {
            Debug.WriteLine("CloseDialog called");
            RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
        }
    }
} 