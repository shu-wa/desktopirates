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
        public void CameraQuarterTurnRotatesDirection()
        {
            Vector2 rotated = DistanceTagMath.Rotate(Vector2.up, -90f);
            Assert.That(rotated.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(rotated.y, Is.EqualTo(0f).Within(0.001f));
        }
    }
}
