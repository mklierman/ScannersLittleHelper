using Prism.Services.Dialogs;
using SimplePhotoEditor.ViewModels;
using Xunit;

namespace SimplePhotoEditor.Tests.ViewModels
{
    public class ConfirmationDialogViewModelTests
    {
        [Fact]
        public void Defaults_AreSet()
        {
            var sut = new ConfirmationDialogViewModel();

            Assert.Null(sut.Message);
            Assert.Equal("Confirm", sut.Title);
            Assert.Equal("Yes", sut.AffirmativeButtonText);
            Assert.Equal("No", sut.NegativeButtonText);
            Assert.True(sut.CanCloseDialog());
        }

        [Fact]
        public void OnDialogOpened_AppliesLowercaseMessageParameter()
        {
            var sut = new ConfirmationDialogViewModel();
            var parameters = new DialogParameters { { "message", "Are you sure?" } };

            sut.OnDialogOpened(parameters);

            Assert.Equal("Are you sure?", sut.Message);
        }

        [Fact]
        public void OnDialogOpened_AppliesUppercaseMessageParameter()
        {
            var sut = new ConfirmationDialogViewModel();
            var parameters = new DialogParameters { { "Message", "Hello" } };

            sut.OnDialogOpened(parameters);

            Assert.Equal("Hello", sut.Message);
        }

        [Fact]
        public void OnDialogOpened_AppliesAllOptionalParameters()
        {
            var sut = new ConfirmationDialogViewModel();
            var parameters = new DialogParameters
            {
                { "message", "Continue?" },
                { "Title", "Custom Title" },
                { "AffirmativeButtonText", "Sure" },
                { "NegativeButtonText", "Nope" }
            };

            sut.OnDialogOpened(parameters);

            Assert.Equal("Continue?", sut.Message);
            Assert.Equal("Custom Title", sut.Title);
            Assert.Equal("Sure", sut.AffirmativeButtonText);
            Assert.Equal("Nope", sut.NegativeButtonText);
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("")]
        public void OnDialogOpened_IgnoresWhitespaceTitle(string whitespace)
        {
            var sut = new ConfirmationDialogViewModel();
            var parameters = new DialogParameters { { "Title", whitespace } };

            sut.OnDialogOpened(parameters);

            Assert.Equal("Confirm", sut.Title);
        }

        [Fact]
        public void CloseDialogCommand_TrueResultsInOk()
        {
            var sut = new ConfirmationDialogViewModel();
            IDialogResult capturedResult = null;
            sut.RequestClose += r => capturedResult = r;

            sut.CloseDialogCommand.Execute("true");

            Assert.NotNull(capturedResult);
            Assert.Equal(ButtonResult.OK, capturedResult.Result);
        }

        [Fact]
        public void CloseDialogCommand_FalseResultsInCancel()
        {
            var sut = new ConfirmationDialogViewModel();
            IDialogResult capturedResult = null;
            sut.RequestClose += r => capturedResult = r;

            sut.CloseDialogCommand.Execute("false");

            Assert.NotNull(capturedResult);
            Assert.Equal(ButtonResult.Cancel, capturedResult.Result);
        }

        [Fact]
        public void CloseDialogCommand_UnknownStringResultsInCancel()
        {
            var sut = new ConfirmationDialogViewModel();
            IDialogResult capturedResult = null;
            sut.RequestClose += r => capturedResult = r;

            sut.CloseDialogCommand.Execute("maybe");

            Assert.NotNull(capturedResult);
            Assert.Equal(ButtonResult.Cancel, capturedResult.Result);
        }

        [Fact]
        public void CloseDialogCommand_DoesNotThrowWithoutSubscriber()
        {
            var sut = new ConfirmationDialogViewModel();

            var ex = Record.Exception(() => sut.CloseDialogCommand.Execute("true"));

            Assert.Null(ex);
        }

        [Fact]
        public void OnDialogClosed_DoesNothing()
        {
            var sut = new ConfirmationDialogViewModel();

            var ex = Record.Exception(() => sut.OnDialogClosed());

            Assert.Null(ex);
        }

        [Fact]
        public void OnDialogOpened_LeavesDefaultsWhenParametersAreEmpty()
        {
            var sut = new ConfirmationDialogViewModel();

            sut.OnDialogOpened(new DialogParameters());

            Assert.Null(sut.Message);
            Assert.Equal("Confirm", sut.Title);
            Assert.Equal("Yes", sut.AffirmativeButtonText);
            Assert.Equal("No", sut.NegativeButtonText);
        }
    }
}
