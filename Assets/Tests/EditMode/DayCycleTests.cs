using NUnit.Framework;
using UnityEngine;

namespace Desktopirates.Tests
{
    public sealed class DayCycleTests
    {
        [TestCase(6f, TimeOfDayPhase.Dawn)]
        [TestCase(12f, TimeOfDayPhase.Day)]
        [TestCase(18f, TimeOfDayPhase.Evening)]
        [TestCase(23f, TimeOfDayPhase.Night)]
        [TestCase(2f, TimeOfDayPhase.Night)]
        public void PhaseMatchesClock(float hour, TimeOfDayPhase expected)
        {
            Assert.That(DayCycle.GetPhase(hour), Is.EqualTo(expected));
        }

        [Test]
        public void HourWrapsAcrossMidnight()
        {
            Assert.That(DayCycle.WrapHour(25f), Is.EqualTo(1f).Within(0.001f));
            Assert.That(DayCycle.WrapHour(-1f), Is.EqualTo(23f).Within(0.001f));
        }

        [Test]
        public void TimeOrbHasTransparentCornersAndOpaqueCenter()
        {
            Texture2D texture = PixelTextureFactory.CreateTimeOrb(23f, 48);
            Assert.That(texture.GetPixel(0, 0).a, Is.EqualTo(0f).Within(0.01f));
            Assert.That(texture.GetPixel(24, 24).a, Is.GreaterThan(0.95f));
            Object.DestroyImmediate(texture);
        }
    }
}
