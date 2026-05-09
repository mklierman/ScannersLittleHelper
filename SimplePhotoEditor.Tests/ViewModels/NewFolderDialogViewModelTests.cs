using Prism.Services.Dialogs;
using SimplePhotoEditor.ViewModels;
using Xunit;

namespace SimplePhotoEditor.Tests.ViewModels
{
    public class NewFolderDialogViewModelTests
    {
        [Fact]
        public void Defaults_AreSet()
        {
            var sut = new NewFolderDialogViewModel();

            Assert.Equal("Notification", sut.Title);
            Assert.Equal("Notification", sut.Input);
            Assert.Equal("OK", sut.AffirmativeButtonText);
            Assert.Equal("Cancel", sut.NegativeButtonText);
            Assert.Null(sut.Message);
            Assert.True(sut.CanCloseDialog());
        }

        [Fact]
        public void OnDialogOpened_AppliesAllParameters()
        {
            var sut = new NewFolderDialogViewModel();
            var parameters = new DialogParameters
            {
                { "Message", "Pick a folder" },
                { "Input", "MyFolder" },
                { "AffirmativeButtonText", "Create" },
                { "NegativeButtonText", "Abort" },
                { "Title", "New Folder" }
            };

            sut.OnDialogOpened(parameters);

            Assert.Equal("Pick a folder", sut.Message);
            Assert.Equal("MyFolder", sut.Input);
            Assert.Equal("Create", sut.AffirmativeButtonText);
            Assert.Equal("Abort", sut.NegativeButtonText);
            Assert.Equal("New Folder", sut.Title);
        }

        [Fact]
        public void CloseDialogCommand_TrueResultsInOk_AndForwardsInput()
        {
            var sut = new NewFolderDialogViewModel { Input = "FolderName" };
            IDialogResult captured = null;
            sut.RequestClose += r => captured = r;

            sut.CloseDialogCommand.Execute("true");

            Assert.NotNull(captured);
            Assert.Equal(ButtonResult.OK, captured.Result);
            Assert.Equal("FolderName", captured.Parameters.GetValue<string>("UserInput"));
        }

        [Fact]
        public void CloseDialogCommand_FalseResultsInCancel()
        {
            var sut = new NewFolderDialogViewModel();
            IDialogResult captured = null;
            sut.RequestClose += r => captured = r;

            sut.CloseDialogCommand.Execute("false");

            Assert.NotNull(captured);
            Assert.Equal(ButtonResult.Cancel, captured.Result);
        }

        [Fact]
        public void CloseDialogCommand_UnknownParameterResultsInNone()
        {
            var sut = new NewFolderDialogViewModel();
            IDialogResult captured = null;
            sut.RequestClose += r => captured = r;

            sut.CloseDialogCommand.Execute("maybe");

            Assert.NotNull(captured);
            Assert.Equal(ButtonResult.None, captured.Result);
        }

        [Fact]
        public void CloseDialogCommand_NullParameterResultsInNone()
        {
            var sut = new NewFolderDialogViewModel();
            IDialogResult captured = null;
            sut.RequestClose += r => captured = r;

            sut.CloseDialogCommand.Execute(null);

            Assert.NotNull(captured);
            Assert.Equal(ButtonResult.None, captured.Result);
        }

        [Fact]
        public void CloseDialogCommand_DoesNotThrowWithoutSubscriber()
        {
            var sut = new NewFolderDialogViewModel();

            var ex = Record.Exception(() => sut.CloseDialogCommand.Execute("true"));

            Assert.Null(ex);
        }

        [Fact]
        public void OnDialogClosed_DoesNothing()
        {
            var sut = new NewFolderDialogViewModel();

            var ex = Record.Exception(() => sut.OnDialogClosed());

            Assert.Null(ex);
        }

        [Fact]
        public void RaiseRequestClose_InvokesEvent()
        {
            var sut = new NewFolderDialogViewModel();
            IDialogResult captured = null;
            sut.RequestClose += r => captured = r;
            var expected = new DialogResult(ButtonResult.OK);

            sut.RaiseRequestClose(expected);

            Assert.Same(expected, captured);
        }

        [Fact]
        public void Properties_RaisePropertyChanged()
        {
            var sut = new NewFolderDialogViewModel();
            var changes = new System.Collections.Generic.List<string>();
            sut.PropertyChanged += (_, e) => changes.Add(e.PropertyName);

            sut.Title = "T";
            sut.Message = "M";
            sut.Input = "I";
            sut.AffirmativeButtonText = "OK!";
            sut.NegativeButtonText = "Nope";

            Assert.Contains(nameof(NewFolderDialogViewModel.Title), changes);
            Assert.Contains(nameof(NewFolderDialogViewModel.Message), changes);
            Assert.Contains(nameof(NewFolderDialogViewModel.Input), changes);
            Assert.Contains(nameof(NewFolderDialogViewModel.AffirmativeButtonText), changes);
            Assert.Contains(nameof(NewFolderDialogViewModel.NegativeButtonText), changes);
        }
    }
}
