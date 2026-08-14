using NUnit.Framework;
using UnityEngine;

namespace Desktopirates.Tests
{
    public sealed class OceanMotionMathTests
    {
        [Test]
        public void MovingNorthIncreasesVOffsetSoTextureTravelsSouth()
        {
            Vector2 start = OceanMotionMath.TextureOffsetForPosition(Vector2.zero);
            Vector2 north = OceanMotionMath.TextureOffsetForPosition(new Vector2(0f, 4f));
            Assert.That(north.y, Is.GreaterThan(start.y));
            Assert.That(north.y - start.y, Is.EqualTo(4f * OceanMotionMath.TextureScale).Within(0.0001f));
        }

        [Test]
        public void MovingEastIncreasesUOffsetSoTextureTravelsWest()
        {
            Vector2 start = OceanMotionMath.TextureOffsetForPosition(Vector2.zero);
            Vector2 east = OceanMotionMath.TextureOffsetForPosition(new Vector2(4f, 0f));
            Assert.That(east.x, Is.GreaterThan(start.x));
            Assert.That(east.x - start.x, Is.EqualTo(4f * OceanMotionMath.TextureScale).Within(0.0001f));
        }
    }
}
