using System.Collections.ObjectModel;
using SimplePhotoEditor.Helpers;
using SimplePhotoEditor.Models;
using Xunit;

namespace SimplePhotoEditor.Tests.Helpers
{
    public class ExtensionMethodsTests
    {
        [Fact]
        public void Sort_ObservableCollection_AscendingByDefault()
        {
            var collection = new ObservableCollection<int> { 3, 1, 4, 1, 5, 9, 2, 6 };

            collection.Sort(x => x);

            Assert.Equal(new[] { 1, 1, 2, 3, 4, 5, 6, 9 }, collection);
        }

        [Fact]
        public void Sort_ObservableCollection_Descending()
        {
            var collection = new ObservableCollection<int> { 3, 1, 4, 1, 5, 9, 2, 6 };

            collection.Sort(x => x, desc: true);

            Assert.Equal(new[] { 9, 6, 5, 4, 3, 2, 1, 1 }, collection);
        }

        [Fact]
        public void Sort_ObservableCollection_ByKeySelector()
        {
            var collection = new ObservableCollection<Thumbnail>
            {
                new Thumbnail { FileName = "charlie.jpg" },
                new Thumbnail { FileName = "alpha.jpg" },
                new Thumbnail { FileName = "bravo.jpg" }
            };

            collection.Sort(x => x.FileName);

            Assert.Equal("alpha.jpg", collection[0].FileName);
            Assert.Equal("bravo.jpg", collection[1].FileName);
            Assert.Equal("charlie.jpg", collection[2].FileName);
        }

        [Fact]
        public void Sort_ObservableCollection_ByKeySelectorDescending()
        {
            var collection = new ObservableCollection<Thumbnail>
            {
                new Thumbnail { FileName = "alpha.jpg" },
                new Thumbnail { FileName = "charlie.jpg" },
                new Thumbnail { FileName = "bravo.jpg" }
            };

            collection.Sort(x => x.FileName, desc: true);

            Assert.Equal("charlie.jpg", collection[0].FileName);
            Assert.Equal("bravo.jpg", collection[1].FileName);
            Assert.Equal("alpha.jpg", collection[2].FileName);
        }

        [Fact]
        public void Sort_ObservableCollection_StableForAlreadySorted()
        {
            var collection = new ObservableCollection<int> { 1, 2, 3, 4, 5 };

            collection.Sort(x => x);

            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, collection);
        }

        [Fact]
        public void Sort_ObservableCollection_HandlesEmptyCollection()
        {
            var collection = new ObservableCollection<int>();

            collection.Sort(x => x);

            Assert.Empty(collection);
        }

        [Fact]
        public void Sort_ObservableCollection_HandlesSingleElement()
        {
            var collection = new ObservableCollection<int> { 42 };

            collection.Sort(x => x);

            Assert.Single(collection);
            Assert.Equal(42, collection[0]);
        }

        [Fact]
        public void Sort_ObservableCollection_NullSourceIsNoOp()
        {
            ObservableCollection<int> collection = null;

            var ex = Record.Exception(() => collection.Sort(x => x));

            Assert.Null(ex);
        }

        [Fact]
        public void Sort_AsyncObservableCollection_AscendingByDefault()
        {
            var collection = new AsyncObservableCollection<int> { 3, 1, 4, 1, 5, 9, 2, 6 };

            collection.Sort(x => x);

            Assert.Equal(new[] { 1, 1, 2, 3, 4, 5, 6, 9 }, collection);
        }

        [Fact]
        public void Sort_AsyncObservableCollection_Descending()
        {
            var collection = new AsyncObservableCollection<int> { 3, 1, 4, 1, 5, 9, 2, 6 };

            collection.Sort(x => x, desc: true);

            Assert.Equal(new[] { 9, 6, 5, 4, 3, 2, 1, 1 }, collection);
        }

        [Fact]
        public void Sort_AsyncObservableCollection_NullSourceIsNoOp()
        {
            AsyncObservableCollection<int> collection = null;

            var ex = Record.Exception(() => collection.Sort(x => x));

            Assert.Null(ex);
        }
    }
}
