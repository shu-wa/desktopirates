using UnityEngine;

namespace Desktopirates
{
    /// <summary>Pure cruise-speed rules shared by input, HUD, upgrades and tests.</summary>
    public static class CruiseModel
    {
        public const int BaseCruiseSteps = 3;
        public const int MaxEngineLevel = 5;
        public const float SpeedPerStep = 1.15f;

        public static int GetMaxStep(int engineLevel) => BaseCruiseSteps + Mathf.Clamp(engineLevel, 0, MaxEngineLevel);

        public static int ChangeStep(int currentStep, int delta, int engineLevel)
            => Mathf.Clamp(currentStep + delta, 0, GetMaxStep(engineLevel));

        public static float GetTargetSpeed(int cruiseStep, int engineLevel)
            => Mathf.Clamp(cruiseStep, 0, GetMaxStep(engineLevel)) * SpeedPerStep;

        public static float GetMaxSpeed(int engineLevel) => GetMaxStep(engineLevel) * SpeedPerStep;

        public static int GetUpgradeCost(int engineLevel) => 90 + Mathf.Clamp(engineLevel, 0, MaxEngineLevel) * 65;
    }
}
