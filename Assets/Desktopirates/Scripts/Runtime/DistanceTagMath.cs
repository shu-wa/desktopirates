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

        public static Vector2 WorldToScreenDirection(Vector2 worldDirection, float cameraYawDegrees)
        {
            // Positive camera yaw moves the camera around the ship toward world-west. From that
            // viewpoint world-north appears on the left, which is the same positive 2D rotation.
            return Rotate(worldDirection, cameraYawDegrees);
        }

        public static Vector2 PositionOnEllipse(Vector2 direction, Vector2 center, Vector2 radii)
        {
            Vector2 unit = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.up;
            return center + new Vector2(unit.x * radii.x, unit.y * radii.y);
        }
    }
}
