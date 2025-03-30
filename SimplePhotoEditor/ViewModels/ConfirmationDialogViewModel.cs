using System;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace SimplePhotoEditor.ViewModels
{
    public class ConfirmationDialogViewModel : BindableBase, IDialogAware
    {
        private string message;
        private DelegateCommand<string> closeDialogCommand;

        public string Message
        {
            get => message;
            set => SetProperty(ref message, value);
        }

        public DelegateCommand<string> CloseDialogCommand =>
            closeDialogCommand ??= new DelegateCommand<string>(CloseDialog);

        public string Title => "Confirm";

        public event Action<IDialogResult> RequestClose;

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            if (parameters.TryGetValue("message", out string message))
            {
                Message = message;
            }
        }

        private void CloseDialog(string result)
        {
            var dialogResult = bool.TryParse(result, out bool boolResult) && boolResult
                ? ButtonResult.OK
                : ButtonResult.Cancel;
            RequestClose?.Invoke(new DialogResult(dialogResult));
        }
    }
} 