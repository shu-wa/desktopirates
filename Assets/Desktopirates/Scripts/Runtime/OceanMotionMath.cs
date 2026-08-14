using UnityEngine;

namespace Desktopirates
{
    public static class OceanMotionMath
    {
        public const float TextureScale = 0.075f;
        private const float WrapPeriod = 256f;

        public static Vector2 TextureOffsetForPosition(Vector2 worldPosition)
        {
            // Sampling at uv + offset makes the visible texture travel in the opposite direction.
            // A forward-moving ship therefore needs a positive world-position offset so the sea
            // visibly streams aft instead of appearing to carry the ship backwards.
            return new Vector2(
                Mathf.Repeat(worldPosition.x * TextureScale, WrapPeriod),
                Mathf.Repeat(worldPosition.y * TextureScale, WrapPeriod));
        }
    }
}
