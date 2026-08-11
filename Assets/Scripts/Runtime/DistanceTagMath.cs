using UnityEngine;

namespace Desktopirates
{
    public static class DistanceTagMath
    {
        public static float ScaleForDistance(float distance, float nearDistance = 7f, float farDistance = 70f)
        {
            float t = Mathf.InverseLerp(nearDistance, farDistance, Mathf.Max(0f, distance));
            return Mathf.Lerp(1.45f, 0.62f, t);
        }

        public static Vector2 Rotate(Vector2 value, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);
            return new Vector2(value.x * cos - value.y * sin, value.x * sin + value.y * cos);
        }
    }
}
