namespace SimplePhotoEditor.Contracts.Services
{
    /// <summary>
    /// Shows short, non-blocking status messages (e.g. after save).
    /// </summary>
    public interface IToastService
    {
        void Show(string message);
    }
}
