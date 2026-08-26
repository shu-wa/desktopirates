using NUnit.Framework;

namespace Desktopirates.Tests
{
    public sealed class EnemyHudModelTests
    {
        [TestCase(500, 1000, 0.5f)]
        [TestCase(0, 1000, 0f)]
        [TestCase(-50, 1000, 0f)]
        [TestCase(1250, 1000, 1f)]
        [TestCase(1, 0, 1f)]
        public void HealthBarAlwaysReflectsClampedCurrentHealth(int health, int maximum, float expected)
        {
            Assert.That(EnemyHudModel.GetHealthRatio(health, maximum), Is.EqualTo(expected).Within(0.001f));
        }

        [Test]
        public void HealthLabelShowsCurrentAndMaximumValues()
        {
            Assert.That(EnemyHudModel.GetHealthLabel(375, 1200), Is.EqualTo("375 / 1200"));
        }

        [TestCase(108, 432, 47f)]
        [TestCase(170, 340, 94f)]
        [TestCase(482, 642, 141.1417f)]
        [TestCase(513, 513, 188f)]
        public void RenderedBarWidthTracksTheSameHealthRatio(int health, int maximum, float expectedWidth)
        {
            Assert.That(EnemyHudModel.GetBarWidth(188f, health, maximum), Is.EqualTo(expectedWidth).Within(0.01f));
        }

        [Test]
        public void IdentityPlateClearsTheShipSilhouette()
        {
            Assert.That(EnemyHudModel.GetVerticalOffset(false), Is.GreaterThanOrEqualTo(2.7f));
            Assert.That(EnemyHudModel.GetVerticalOffset(true), Is.GreaterThan(EnemyHudModel.GetVerticalOffset(false)));
            Assert.That(UiLayoutMetrics.EnemyHudTopSafeInset, Is.GreaterThan(UiLayoutMetrics.HudCardHeight * 2f));
        }
    }
}
