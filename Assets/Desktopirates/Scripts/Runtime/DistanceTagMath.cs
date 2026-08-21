using System.Collections.Generic;
using UnityEngine;

namespace Desktopirates
{
    public static class DistanceTagMath
    {
        public static float ScaleForDistance(float distance, float nearDistance = 7f, float farDistance = 60f)
        {
            float t = Mathf.InverseLerp(nearDistance, farDistance, Mathf.Max(0f, distance));
            // Smooth easing keeps nearby discoveries legible while making genuinely
            // distant hints recede instead of competing with the ship and HUD.
            t = t * t * (3f - 2f * t);
            return Mathf.Lerp(1.22f, 0.38f, t);
        }

        public static float PixelSizeForDistance(float distance)
            => Mathf.Clamp(76f * ScaleForDistance(distance), 30f, 94f);

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

        public static Vector2 PositionOutsideRim(Vector2 center, Vector2 rimPoint, float markerPixels, float padding)
        {
            Vector2 outward = rimPoint - center;
            if (outward.sqrMagnitude < 0.0001f) outward = Vector2.up;
            return rimPoint + outward.normalized * (Mathf.Max(0f, markerPixels) * 0.5f + Mathf.Max(0f, padding));
        }

        public static Vector2 ClampMarkerInside(Vector2 center, Rect canvasRect, float markerPixels, float padding)
        {
            float inset = Mathf.Max(0f, markerPixels) * 0.5f + Mathf.Max(0f, padding);
            float minX = canvasRect.xMin + inset;
            float maxX = canvasRect.xMax - inset;
            float minY = canvasRect.yMin + inset;
            float maxY = canvasRect.yMax - inset;
            if (minX > maxX) minX = maxX = canvasRect.center.x;
            if (minY > maxY) minY = maxY = canvasRect.center.y;
            return new Vector2(Mathf.Clamp(center.x, minX, maxX), Mathf.Clamp(center.y, minY, maxY));
        }

        public static Rect MarkerRect(Vector2 center, float markerPixels, float separation = 0f)
        {
            float size = Mathf.Max(0f, markerPixels) + Mathf.Max(0f, separation) * 2f;
            return new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size);
        }

        public static bool IsClear(Rect markerRect, IReadOnlyList<Rect> obstacles)
        {
            if (obstacles == null) return true;
            for (int i = 0; i < obstacles.Count; i++)
                if (markerRect.Overlaps(obstacles[i])) return false;
            return true;
        }

        public static Rect Expanded(Rect source, float padding)
        {
            float amount = Mathf.Max(0f, padding);
            return new Rect(source.xMin - amount, source.yMin - amount, source.width + amount * 2f, source.height + amount * 2f);
        }
    }
}
