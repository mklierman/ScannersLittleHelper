using System;
using Moq;
using Prism.Regions;
using Xunit;

namespace SimplePhotoEditor.Tests.Helpers
{
    public class IRegionNavigationServiceExtensionsTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void CanNavigate_ReturnsFalseForNullOrEmptyTarget(string target)
        {
            var service = new Mock<IRegionNavigationService>();

            var result = service.Object.CanNavigate(target);

            Assert.False(result);
        }

        [Fact]
        public void CanNavigate_ReturnsTrueWhenJournalHasNoCurrentEntry()
        {
            var journal = new Mock<IRegionNavigationJournal>();
            journal.SetupGet(j => j.CurrentEntry).Returns((IRegionNavigationJournalEntry)null);
            var service = new Mock<IRegionNavigationService>();
            service.SetupGet(s => s.Journal).Returns(journal.Object);

            var result = service.Object.CanNavigate("Scan");

            Assert.True(result);
        }

        [Fact]
        public void CanNavigate_ReturnsFalseWhenTargetMatchesCurrentEntry()
        {
            var entry = new Mock<IRegionNavigationJournalEntry>();
            entry.SetupGet(e => e.Uri).Returns(new Uri("Scan", UriKind.Relative));
            var journal = new Mock<IRegionNavigationJournal>();
            journal.SetupGet(j => j.CurrentEntry).Returns(entry.Object);
            var service = new Mock<IRegionNavigationService>();
            service.SetupGet(s => s.Journal).Returns(journal.Object);

            var result = service.Object.CanNavigate("Scan");

            Assert.False(result);
        }

        [Fact]
        public void CanNavigate_ReturnsTrueWhenTargetDiffersFromCurrentEntry()
        {
            var entry = new Mock<IRegionNavigationJournalEntry>();
            entry.SetupGet(e => e.Uri).Returns(new Uri("Thumbnail", UriKind.Relative));
            var journal = new Mock<IRegionNavigationJournal>();
            journal.SetupGet(j => j.CurrentEntry).Returns(entry.Object);
            var service = new Mock<IRegionNavigationService>();
            service.SetupGet(s => s.Journal).Returns(journal.Object);

            var result = service.Object.CanNavigate("Scan");

            Assert.True(result);
        }
    }
}
