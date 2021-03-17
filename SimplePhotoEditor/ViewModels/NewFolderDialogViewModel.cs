using ControlzEx.Standard;
using MahApps.Metro.Controls.Dialogs;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace SimplePhotoEditor.ViewModels
{
    public class NewFolderDialogViewModel : BindableBase, IDialogAware
    {
        private IDialogParameters DialogParameters;
        private DelegateCommand<string> _closeDialogCommand;
        public DelegateCommand<string> CloseDialogCommand =>
            _closeDialogCommand ?? (_closeDialogCommand = new DelegateCommand<string>(CloseDialog));

        private string _message;
        public string Message
        {
            get { return _message; }
            set { SetProperty(ref _message, value); }
        }

        private string _title = "Notification";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        public event Action<IDialogResult> RequestClose;

        protected virtual void CloseDialog(string parameter)
        {
            ButtonResult result = ButtonResult.None;

            if (parameter?.ToLower() == "true")
                result = ButtonResult.OK;
            else if (parameter?.ToLower() == "false")
                result = ButtonResult.Cancel;

            DialogParameters = new DialogParameters { { "UserInput", Input } };
            RaiseRequestClose(new DialogResult(result, DialogParameters));
        }


        public virtual void RaiseRequestClose(IDialogResult dialogResult)
        {
            RequestClose?.Invoke(dialogResult);
        }

        public virtual bool CanCloseDialog()
        {
            return true;
        }

        public virtual void OnDialogClosed()
        {

        }

        public virtual void OnDialogOpened(IDialogParameters parameters)
        {
            Message = parameters.GetValue<string>("Message");
            Input = parameters.GetValue<string>("Input");
            AffirmativeButtonText = parameters.GetValue<string>("AffirmativeButtonText");
            NegativeButtonText = parameters.GetValue<string>("NegativeButtonText");
            Title = parameters.GetValue<string>("Title");
        }


        private string _input = "Notification";
        public string Input
        {
            get { return _input; }
            set { SetProperty(ref _input, value); }
        }

        private string _affirmativeButtonText = "OK";
        public string AffirmativeButtonText
        {
            get { return _affirmativeButtonText; }
            set { SetProperty(ref _affirmativeButtonText, value); }
        }

        private string _negativeButtonText = "Cancel";
        public string NegativeButtonText
        {
            get { return _negativeButtonText; }
            set { SetProperty(ref _negativeButtonText, value); }
        }
    }
}

