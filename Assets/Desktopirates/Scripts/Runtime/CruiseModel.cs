using UnityEngine;

namespace Desktopirates
{
    /// <summary>Pure cruise-speed rules shared by input, HUD, upgrades and tests.</summary>
    public static class CruiseModel
    {
        public const int BaseCruiseSteps = 3;
        public const int MaxEngineLevel = 5;
        public const float SpeedPerStep = 1.15f;
        public const float EngineAcceleration = 1.55f;
        public const float StillWaterDrag = 0.065f;
        public const float HydrodynamicDrag = 0.10f;
        public const float StopSnapSpeed = 0.08f;
        public const float CoastingTolerance = 0.06f;

        public static int GetMaxStep(int engineLevel) => BaseCruiseSteps + Mathf.Clamp(engineLevel, 0, MaxEngineLevel);

        public static int ChangeStep(int currentStep, int delta, int engineLevel)
            => Mathf.Clamp(currentStep + delta, 0, GetMaxStep(engineLevel));

        public static float GetTargetSpeed(int cruiseStep, int engineLevel)
            => Mathf.Clamp(cruiseStep, 0, GetMaxStep(engineLevel)) * SpeedPerStep;

        public static float GetMaxSpeed(int engineLevel) => GetMaxStep(engineLevel) * SpeedPerStep;

        /// <summary>
        /// Advances forward speed using engine thrust or quadratic water resistance.
        /// The analytic drag solution keeps coasting consistent across frame rates.
        /// </summary>
        public static float IntegrateForwardSpeed(float currentSpeed, float targetSpeed, float deltaTime)
        {
            float current = Mathf.Max(0f, currentSpeed);
            float target = Mathf.Max(0f, targetSpeed);
            float elapsed = Mathf.Max(0f, deltaTime);
            if (elapsed <= 0f) return current;

            if (current < target)
            {
                return Mathf.MoveTowards(current, target, EngineAcceleration * elapsed);
            }

            if (current <= target) return current;

            // dv/dt = -(a + b*v^2). Solving it directly avoids frame-rate dependent drag.
            float speedScale = Mathf.Sqrt(StillWaterDrag / HydrodynamicDrag);
            float angle = Mathf.Atan(current / speedScale);
            angle = Mathf.Max(0f, angle - Mathf.Sqrt(StillWaterDrag * HydrodynamicDrag) * elapsed);
            float next = Mathf.Tan(angle) * speedScale;
            next = Mathf.Max(target, next);
            return target <= 0f && next <= StopSnapSpeed ? 0f : next;
        }

        public static bool IsCoasting(float currentSpeed, float targetSpeed)
            => currentSpeed > targetSpeed + CoastingTolerance;

        public static int GetUpgradeCost(int engineLevel) => 90 + Mathf.Clamp(engineLevel, 0, MaxEngineLevel) * 65;
    }

    /// <summary>
    /// Shared forward-only steering rules for the player and AI vessels. A rudder
    /// cannot rotate a stopped hull, and every turn is limited by water speed.
    /// </summary>
    public static class VesselMotionModel
    {
        public const float MinimumTurnRate = 18f;
        public const float MaximumTurnRate = 62f;

        public static float GetTurnRate(float speed, float maximumSpeed, float multiplier = 1f)
        {
            if (speed <= CruiseModel.StopSnapSpeed) return 0f;
            float speedRatio = Mathf.Clamp01(speed / Mathf.Max(0.01f, maximumSpeed));
            return Mathf.Lerp(MinimumTurnRate, MaximumTurnRate, speedRatio) * Mathf.Max(0f, multiplier);
        }

        public static float TurnToward(float headingDegrees, float desiredHeadingDegrees, float speed,
            float maximumSpeed, float multiplier, float deltaTime)
        {
            float rate = GetTurnRate(speed, maximumSpeed, multiplier);
            return Mathf.Repeat(Mathf.MoveTowardsAngle(headingDegrees, desiredHeadingDegrees,
                rate * Mathf.Max(0f, deltaTime)), 360f);
        }

        public static Vector2 GetForward(float headingDegrees)
        {
            float radians = headingDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
        }

        public static float GetHeading(Vector2 direction)
            => direction.sqrMagnitude <= 0.0001f ? 0f : Mathf.Repeat(Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg, 360f);
    }
}
