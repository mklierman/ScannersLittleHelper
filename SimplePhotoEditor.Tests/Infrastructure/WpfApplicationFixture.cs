using System;
using System.Collections;
using System.Windows;
using Xunit;

namespace SimplePhotoEditor.Tests.Infrastructure
{
    /// <summary>
    /// Ensures a WPF <see cref="Application"/> singleton exists so production code that reads
    /// <c>App.Current.Properties</c> (the Application properties bag) does not NRE in tests.
    /// </summary>
    public sealed class WpfApplicationFixture : IDisposable
    {
        public WpfApplicationFixture()
        {
            if (Application.Current == null)
            {
                _ = new Application();
            }

            ResetProperties();
        }

        public IDictionary Properties => Application.Current.Properties;

        public void ResetProperties()
        {
            Application.Current.Properties.Clear();
        }

        public void Dispose()
        {
            ResetProperties();
        }
    }

    [CollectionDefinition(WpfApplicationCollection.Name)]
    public class WpfApplicationCollection : ICollectionFixture<WpfApplicationFixture>
    {
        public const string Name = "WpfApplication";
    }
}
