using System;
using System.Diagnostics;
using System.Reflection;

using SimplePhotoEditor.Contracts.Services;

namespace SimplePhotoEditor.Services
{
    public class ApplicationInfoService : IApplicationInfoService
    {
        public ApplicationInfoService()
        {
        }

        public Version GetVersion()
        {
            // Set the app version in SimplePhotoEditor > Properties > Package > PackageVersion
            var processPath = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(processPath))
            {
                var fileVersion = FileVersionInfo.GetVersionInfo(processPath).FileVersion;
                if (!string.IsNullOrWhiteSpace(fileVersion))
                {
                    return new Version(fileVersion);
                }
            }

            return Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0);
        }
    }
}
