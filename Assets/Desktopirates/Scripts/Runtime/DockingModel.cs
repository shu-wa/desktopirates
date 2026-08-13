using UnityEngine;

namespace Desktopirates
{
    /// <summary>Stable harbor-pilot geometry shared by runtime motion and edit-mode tests.</summary>
    public static class DockingModel
    {
        public const float FinalHeading = 0f;
        public const float ArrivalDistance = 0.055f;

        public static Vector2 GetApproach(Vector2 portPosition)
            => portPosition + new Vector2(0.72f, -2.65f);

        public static Vector2 GetBerth(Vector2 portPosition)
            => portPosition + new Vector2(0.72f, -1.32f);

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
