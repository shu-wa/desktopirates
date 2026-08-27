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

        private static readonly float[] BerthOffsetsX = { -1.45f, 0.05f, 1.52f };

        public static Vector2 GetApproach(Vector2 portPosition)
            => GetApproach(portPosition, DefaultBerthIndex);

        public static Vector2 GetApproach(Vector2 portPosition, int berthIndex)
            => portPosition + new Vector2(BerthOffsetsX[ClampBerth(berthIndex)], -3.70f);

        public static Vector2 GetBerth(Vector2 portPosition)
            => GetBerth(portPosition, DefaultBerthIndex);

        public static Vector2 GetBerth(Vector2 portPosition, int berthIndex)
            => portPosition + new Vector2(BerthOffsetsX[ClampBerth(berthIndex)], -1.82f);

        public static bool IsValidBerth(int berthIndex) => berthIndex >= 0 && berthIndex < BerthCount;

        private static int ClampBerth(int berthIndex) => Mathf.Clamp(berthIndex, 0, BerthCount - 1);

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
