using System.Windows;
using SimplePhotoEditor.Contracts.Services;
using SimplePhotoEditor.Views;

namespace SimplePhotoEditor.Services
{
    public class ToastService : IToastService
    {
        public void Show(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            Application.Current?.Dispatcher.Invoke(() =>
            {
                if (Application.Current?.MainWindow is ShellWindow shell)
                {
                    shell.ShowToast(message);
                }
            });
        }
    }
}
