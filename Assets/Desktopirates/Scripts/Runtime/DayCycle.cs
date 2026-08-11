using UnityEngine;

namespace Desktopirates
{
    public enum TimeOfDayPhase
    {
        Dawn,
        Day,
        Evening,
        Night
    }

    public struct DayCycleState
    {
        public TimeOfDayPhase Phase;
        public Color SkyTop;
        public Color SkyBottom;
        public Color Water;
        public Color Ambient;

        public static DayCycleState Lerp(DayCycleState a, DayCycleState b, float t, TimeOfDayPhase phase)
        {
            return new DayCycleState
            {
                Phase = phase,
                SkyTop = Color.Lerp(a.SkyTop, b.SkyTop, t),
                SkyBottom = Color.Lerp(a.SkyBottom, b.SkyBottom, t),
                Water = Color.Lerp(a.Water, b.Water, t),
                Ambient = Color.Lerp(a.Ambient, b.Ambient, t)
            };
        }
    }

    public static class DayCycle
    {
        private static readonly DayCycleState Dawn = Make(
            TimeOfDayPhase.Dawn,
            new Color(0.25f, 0.24f, 0.46f),
            new Color(0.98f, 0.48f, 0.30f),
            new Color(0.08f, 0.30f, 0.36f),
            new Color(0.47f, 0.36f, 0.42f));

        private static readonly DayCycleState Day = Make(
            TimeOfDayPhase.Day,
            new Color(0.19f, 0.58f, 0.83f),
            new Color(0.61f, 0.88f, 0.93f),
            new Color(0.05f, 0.43f, 0.49f),
            new Color(0.72f, 0.78f, 0.75f));

        private static readonly DayCycleState Evening = Make(
            TimeOfDayPhase.Evening,
            new Color(0.19f, 0.16f, 0.37f),
            new Color(0.98f, 0.35f, 0.15f),
            new Color(0.06f, 0.24f, 0.32f),
            new Color(0.46f, 0.29f, 0.29f));

        private static readonly DayCycleState Night = Make(
            TimeOfDayPhase.Night,
            new Color(0.035f, 0.07f, 0.20f),
            new Color(0.07f, 0.19f, 0.33f),
            new Color(0.09f, 0.34f, 0.42f),
            new Color(0.42f, 0.47f, 0.58f));

        public static float WrapHour(float hour)
        {
            hour %= 24f;
            return hour < 0f ? hour + 24f : hour;
        }

        public static TimeOfDayPhase GetPhase(float hour)
        {
            hour = WrapHour(hour);
            if (hour >= 5f && hour < 8f) return TimeOfDayPhase.Dawn;
            if (hour >= 8f && hour < 17f) return TimeOfDayPhase.Day;
            if (hour >= 17f && hour < 20f) return TimeOfDayPhase.Evening;
            return TimeOfDayPhase.Night;
        }

        public static DayCycleState Evaluate(float hour)
        {
            hour = WrapHour(hour);

            if (hour < 5f) return Night;
            if (hour < 8f)
                return DayCycleState.Lerp(Night, Dawn, Mathf.InverseLerp(5f, 7f, hour), TimeOfDayPhase.Dawn);
            if (hour < 10f)
                return DayCycleState.Lerp(Dawn, Day, Mathf.InverseLerp(8f, 10f, hour), TimeOfDayPhase.Day);
            if (hour < 17f) return Day;
            if (hour < 20f)
                return DayCycleState.Lerp(Day, Evening, Mathf.InverseLerp(17f, 19f, hour), TimeOfDayPhase.Evening);
            if (hour < 22f)
                return DayCycleState.Lerp(Evening, Night, Mathf.InverseLerp(20f, 22f, hour), TimeOfDayPhase.Night);
            return Night;
        }

        private static DayCycleState Make(TimeOfDayPhase phase, Color top, Color bottom, Color water, Color ambient)
        {
            return new DayCycleState
            {
                Phase = phase,
                SkyTop = top,
                SkyBottom = bottom,
                Water = water,
                Ambient = ambient
            };
        }
    }
}
