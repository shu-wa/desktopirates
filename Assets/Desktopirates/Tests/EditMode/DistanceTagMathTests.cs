using NUnit.Framework;
using UnityEngine;

namespace Desktopirates.Tests
{
    public sealed class DistanceTagMathTests
    {
        [Test]
        public void TagGrowsAsTargetGetsCloser()
        {
            float near = DistanceTagMath.ScaleForDistance(9f);
            float far = DistanceTagMath.ScaleForDistance(60f);
            Assert.That(near, Is.GreaterThan(far));
            Assert.That(DistanceTagMath.PixelSizeForDistance(9f), Is.GreaterThan(80f));
            Assert.That(DistanceTagMath.PixelSizeForDistance(60f), Is.LessThanOrEqualTo(30f));
        }

        [Test]
        public void CameraRightQuarterTurnMovesWorldNorthToScreenLeft()
        {
            Vector2 rotated = DistanceTagMath.WorldToScreenDirection(Vector2.up, 90f);
            Assert.That(rotated.x, Is.EqualTo(-1f).Within(0.001f));
            Assert.That(rotated.y, Is.EqualTo(0f).Within(0.001f));
        }

        [TestCase(0f, 0f, 1f)]
        [TestCase(90f, -1f, 0f)]
        [TestCase(180f, 0f, -1f)]
        [TestCase(270f, 1f, 0f)]
        public void WorldNorthTracksAllCameraQuadrants(float yaw, float expectedX, float expectedY)
        {
            Vector2 screen = DistanceTagMath.WorldToScreenDirection(Vector2.up, yaw);
            Assert.That(screen.x, Is.EqualTo(expectedX).Within(0.001f));
            Assert.That(screen.y, Is.EqualTo(expectedY).Within(0.001f));
        }

        [Test]
        public void WorldEastRemainsScreenRightAtDefaultCamera()
        {
            Vector2 screen = DistanceTagMath.WorldToScreenDirection(Vector2.right, 0f);
            Assert.That(screen.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(screen.y, Is.EqualTo(0f).Within(0.001f));
        }

        [TestCase(1f, 0f)]
        [TestCase(0f, 1f)]
        [TestCase(-1f, 0f)]
        [TestCase(0f, -1f)]
        [TestCase(0.7071068f, 0.7071068f)]
        public void LandmarkPositionAlwaysLiesOnTheSeaEllipse(float x, float y)
        {
            Vector2 center = new Vector2(0f, -63f);
            Vector2 radii = new Vector2(312f, 218f);
            Vector2 position = DistanceTagMath.PositionOnEllipse(new Vector2(x, y), center, radii);
            Vector2 normalized = new Vector2(
                (position.x - center.x) / radii.x,
                (position.y - center.y) / radii.y);
            Assert.That(normalized.sqrMagnitude, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void LargestTopTagKeepsClearOfTheDashboard()
        {
            const float largestMarkerRadius = 49f;
            Assert.That(TagRingController.HudSafeTop + largestMarkerRadius, Is.LessThan(160f));
        }
    }
}
