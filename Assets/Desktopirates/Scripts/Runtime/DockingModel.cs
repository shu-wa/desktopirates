using UnityEngine;

namespace Desktopirates
{
    /// <summary>Stable harbor-pilot geometry shared by runtime motion and edit-mode tests.</summary>
    public static class DockingModel
    {
        public const int BerthCount = 3;
        public const int DefaultBerthIndex = 1;
        public const float FinalHeading = 0f;
        public const float ArrivalDistance = 0.055f;
        public const float ReentryCaptureDistance = 0.72f;
        public const int MaxRouteWaypoints = 4;

        // These are the water-channel centres between the authored harbor_v02 piers after
        // its 1.48 presentation scale. They deliberately avoid the pier mesh footprints.
        private static readonly float[] BerthOffsetsX = { -1.76f, -0.18f, 1.41f };
        private const float ApproachOffsetY = -3.72f;
        private const float BerthOffsetY = -1.82f;
        private const float HarborLeft = -3.58f;
        private const float HarborRight = 3.58f;
        private const float HarborBottom = -2.92f;
        private const float HarborTop = 2.62f;
        private const float BypassOffsetX = 4.35f;

        public static Vector2 GetApproach(Vector2 portPosition)
            => GetApproach(portPosition, DefaultBerthIndex);

        public static Vector2 GetApproach(Vector2 portPosition, int berthIndex)
            => portPosition + new Vector2(BerthOffsetsX[ClampBerth(berthIndex)], ApproachOffsetY);

        public static Vector2 GetBerth(Vector2 portPosition)
            => GetBerth(portPosition, DefaultBerthIndex);

        public static Vector2 GetBerth(Vector2 portPosition, int berthIndex)
            => portPosition + new Vector2(BerthOffsetsX[ClampBerth(berthIndex)], BerthOffsetY);

        public static int GetNearestBerthIndex(Vector2 portPosition, Vector2 vesselPosition)
        {
            int nearest = 0;
            float best = float.MaxValue;
            for (int i = 0; i < BerthCount; i++)
            {
                float distance = (vesselPosition - GetBerth(portPosition, i)).sqrMagnitude;
                if (distance >= best) continue;
                best = distance;
                nearest = i;
            }
            return nearest;
        }

        public static bool IsAtBerth(Vector2 vesselPosition, Vector2 portPosition, int berthIndex)
            => Vector2.Distance(vesselPosition, GetBerth(portPosition, berthIndex)) <= ReentryCaptureDistance;

        /// <summary>
        /// Builds a route that enters from the harbor's open south side. If a direct leg would
        /// cross the authored quay/pier footprint, the pilot first runs outside either harbor
        /// wall, then turns into the fairway. Returns the number of populated destinations.
        /// </summary>
        public static int BuildRoute(Vector2 portPosition, Vector2 vesselPosition, int berthIndex, Vector2[] destinations)
        {
            if (destinations == null || destinations.Length < MaxRouteWaypoints) return 0;
            berthIndex = ClampBerth(berthIndex);
            Vector2 approach = GetApproach(portPosition, berthIndex);
            Vector2 berth = GetBerth(portPosition, berthIndex);
            int count = 0;

            bool alreadyInChannel = Mathf.Abs(vesselPosition.x - approach.x) <= 0.62f
                && vesselPosition.y <= portPosition.y + 0.25f
                && vesselPosition.y >= portPosition.y + ApproachOffsetY - 0.35f;
            if (!alreadyInChannel && SegmentCrossesHarbor(vesselPosition, approach, portPosition))
            {
                float leftX = portPosition.x - BypassOffsetX;
                float rightX = portPosition.x + BypassOffsetX;
                float sideX = Mathf.Abs(vesselPosition.x - leftX) <= Mathf.Abs(vesselPosition.x - rightX) ? leftX : rightX;
                float safeUpperY = Mathf.Max(vesselPosition.y, portPosition.y + HarborTop + 0.32f);
                if (vesselPosition.y > portPosition.y + HarborBottom)
                    destinations[count++] = new Vector2(sideX, safeUpperY);
                destinations[count++] = new Vector2(sideX, approach.y);
            }

            destinations[count++] = approach;
            destinations[count++] = berth;
            return count;
        }

        public static bool SegmentCrossesHarbor(Vector2 start, Vector2 end, Vector2 portPosition)
        {
            float minX = portPosition.x + HarborLeft;
            float maxX = portPosition.x + HarborRight;
            float minY = portPosition.y + HarborBottom;
            float maxY = portPosition.y + HarborTop;
            if (PointInside(start, minX, maxX, minY, maxY) || PointInside(end, minX, maxX, minY, maxY)) return true;
            return SegmentsIntersect(start, end, new Vector2(minX, minY), new Vector2(maxX, minY))
                || SegmentsIntersect(start, end, new Vector2(maxX, minY), new Vector2(maxX, maxY))
                || SegmentsIntersect(start, end, new Vector2(maxX, maxY), new Vector2(minX, maxY))
                || SegmentsIntersect(start, end, new Vector2(minX, maxY), new Vector2(minX, minY));
        }

        public static bool IsValidBerth(int berthIndex) => berthIndex >= 0 && berthIndex < BerthCount;

        private static int ClampBerth(int berthIndex) => Mathf.Clamp(berthIndex, 0, BerthCount - 1);

        private static bool PointInside(Vector2 point, float minX, float maxX, float minY, float maxY)
            => point.x >= minX && point.x <= maxX && point.y >= minY && point.y <= maxY;

        private static bool SegmentsIntersect(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            float abC = Cross(b - a, c - a);
            float abD = Cross(b - a, d - a);
            float cdA = Cross(d - c, a - c);
            float cdB = Cross(d - c, b - c);
            return abC * abD <= 0f && cdA * cdB <= 0f;
        }

        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        public static float GetHeading(Vector2 from, Vector2 to)
        {
            Vector2 delta = to - from;
            return delta.sqrMagnitude < 0.0001f ? FinalHeading : Mathf.Atan2(delta.x, delta.y) * Mathf.Rad2Deg;
        }

        public static float GetPilotSpeed(float distance, bool finalLeg)
        {
            if (!finalLeg) return Mathf.Lerp(0.72f, 1.35f, Mathf.Clamp01(distance / 2.4f));
            return Mathf.Lerp(0.18f, 0.72f, Mathf.Clamp01(distance / 1.25f));
        }
    }
}
