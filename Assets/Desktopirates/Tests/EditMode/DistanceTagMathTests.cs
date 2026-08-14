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
    }
}
