using System;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace SimplePhotoEditor.ViewModels
{
    public class ConfirmationDialogViewModel : BindableBase, IDialogAware
    {
        private string message;
        private string title = "Confirm";
        private string affirmativeButtonText = "Yes";
        private string negativeButtonText = "No";
        private DelegateCommand<string> closeDialogCommand;

        public string Message
        {
            get => message;
            set => SetProperty(ref message, value);
        }

        public string Title
        {
            get => title;
            set => SetProperty(ref title, value);
        }

        public string AffirmativeButtonText
        {
            get => affirmativeButtonText;
            set => SetProperty(ref affirmativeButtonText, value);
        }

        public string NegativeButtonText
        {
            get => negativeButtonText;
            set => SetProperty(ref negativeButtonText, value);
        }

        public DelegateCommand<string> CloseDialogCommand =>
            closeDialogCommand ??= new DelegateCommand<string>(CloseDialog);

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
            if (parameters.TryGetValue("message", out string msgLower))
            {
                Message = msgLower;
            }
            else if (parameters.TryGetValue("Message", out string msgUpper))
            {
                Message = msgUpper;
            }

            if (parameters.TryGetValue("Title", out string titleParam) && !string.IsNullOrWhiteSpace(titleParam))
            {
                Title = titleParam;
            }

            if (parameters.TryGetValue("AffirmativeButtonText", out string affirmative) &&
                !string.IsNullOrWhiteSpace(affirmative))
            {
                AffirmativeButtonText = affirmative;
            }

            if (parameters.TryGetValue("NegativeButtonText", out string negative) &&
                !string.IsNullOrWhiteSpace(negative))
            {
                NegativeButtonText = negative;
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
