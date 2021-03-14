using System;

namespace SimplePhotoEditor.Contracts.Services
{
    public interface IApplicationInfoService
    {
        Version GetVersion();
    }
}
