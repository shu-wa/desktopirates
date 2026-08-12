using UnityEngine;

namespace Desktopirates
{
    public static class SpeedGaugeModel
    {
        public const int SegmentCount = 8;

        public static string GetLabel(int step, int maxStep)
        {
            if (step <= 0) return "STOP";
            float ratio = Mathf.Clamp01(step / (float)Mathf.Max(1, maxStep));
            if (ratio <= 0.34f) return "SLOW";
            if (ratio <= 0.67f) return "CRUISE";
            if (ratio < 0.99f) return "HALF";
            return "FULL";
        }

        public static float GetNeedle01(int step, int maxStep)
        {
            return Mathf.Clamp01(step / (float)Mathf.Max(1, maxStep));
        }
    }
}
