using Prism.Services.Dialogs;
using SimplePhotoEditor.ViewModels;
using Xunit;

namespace SimplePhotoEditor.Tests.ViewModels
{
    public class ErrorDialogViewModelTests
    {
        [Fact]
        public void Defaults_AreSet()
        {
            var sut = new ErrorDialogViewModel();

            Assert.Equal("Initializing...", sut.Message);
            Assert.Equal("Error", sut.Title);
            Assert.True(sut.CanCloseDialog());
        }

        [Fact]
        public void OnDialogOpened_AppliesLowercaseMessageParameter()
        {
            var sut = new ErrorDialogViewModel();
            var parameters = new DialogParameters { { "message", "Boom" } };

            sut.OnDialogOpened(parameters);

            Assert.Equal("Boom", sut.Message);
        }

        [Fact]
        public void OnDialogOpened_AppliesUppercaseMessageParameter()
        {
            var sut = new ErrorDialogViewModel();
            var parameters = new DialogParameters { { "Message", "Capital Boom" } };

            sut.OnDialogOpened(parameters);

            Assert.Equal("Capital Boom", sut.Message);
        }

        [Fact]
        public void OnDialogOpened_FallsBackWhenMessageMissing()
        {
            var sut = new ErrorDialogViewModel();

            sut.OnDialogOpened(new DialogParameters());

            Assert.Equal("No message provided", sut.Message);
        }

        [Fact]
        public void CloseDialogCommand_AlwaysReturnsOk()
        {
            var sut = new ErrorDialogViewModel();
            IDialogResult captured = null;
            sut.RequestClose += r => captured = r;

            sut.CloseDialogCommand.Execute("anything");

            Assert.NotNull(captured);
            Assert.Equal(ButtonResult.OK, captured.Result);
        }

        [Fact]
        public void CloseDialogCommand_DoesNotThrowWithoutSubscriber()
        {
            var sut = new ErrorDialogViewModel();

            var ex = Record.Exception(() => sut.CloseDialogCommand.Execute("ok"));

            Assert.Null(ex);
        }
    }
}
