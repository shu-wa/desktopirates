using NUnit.Framework;

namespace Desktopirates.Tests
{
    public sealed class NotificationUiModelTests
    {
        [Test]
        public void FeedRemainsReadableThenFadesCompletelyAtFiveSeconds()
        {
            Assert.That(NotificationUiModel.OpaqueFeedBackgroundAlpha, Is.EqualTo(1f),
                "The feed backdrop must never blend with the Windows magenta transparency key.");
            Assert.That(NotificationUiModel.GetFeedAlpha(0f), Is.EqualTo(1f));
            Assert.That(NotificationUiModel.GetFeedAlpha(NotificationUiModel.FeedFadeStart), Is.EqualTo(1f));
            Assert.That(NotificationUiModel.GetFeedAlpha(4.2f), Is.InRange(0f, 1f));
            Assert.That(NotificationUiModel.GetFeedAlpha(5f), Is.EqualTo(0f));
        }

        [Test]
        public void NewNotificationsPushOlderEntriesUpward()
        {
            Assert.That(NotificationUiModel.GetStackY(0), Is.EqualTo(0f));
            Assert.That(NotificationUiModel.GetStackY(1), Is.EqualTo(NotificationUiModel.FeedStride));
            Assert.That(NotificationUiModel.GetStackY(3), Is.EqualTo(NotificationUiModel.FeedStride * 3f));
            Assert.That(NotificationUiModel.FeedBaseY, Is.GreaterThan(190f), "The feed must clear the speed telegraph and context prompt.");
        }
    }
}
