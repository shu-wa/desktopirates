using UnityEngine;

namespace Desktopirates
{
    public enum CannonRoundKind { RoundShot, ChainShot, FireShot }

    public readonly struct CannonRoundProfile
    {
        public readonly CannonRoundKind Kind;
        public readonly string Name;
        public readonly float ReloadMultiplier;
        public readonly float RangeMultiplier;
        public readonly float DamageMultiplier;

        public CannonRoundProfile(CannonRoundKind kind, string name, float reload, float range, float damage)
        {
            Kind = kind; Name = name; ReloadMultiplier = reload; RangeMultiplier = range; DamageMultiplier = damage;
        }
    }

    /// <summary>Independent hardpoint progression and ammunition timings used by automatic fire.</summary>
    public static class CannonUpgradeModel
    {
        public const int MaxSlotUpgrade = 5;
        public const float BaseRange = 4.25f;
        public const float BaseReloadSeconds = 1.25f;

        private static readonly CannonRoundProfile[] Profiles =
        {
            new CannonRoundProfile(CannonRoundKind.RoundShot, "ROUND SHOT", 1.00f, 1.00f, 1.00f),
            new CannonRoundProfile(CannonRoundKind.ChainShot, "CHAIN SHOT", 0.82f, 0.88f, 0.78f),
            new CannonRoundProfile(CannonRoundKind.FireShot, "FIRE SHOT", 1.28f, 0.94f, 0.92f)
        };

        public static CannonRoundProfile GetProfile(CannonRoundKind kind) => Profiles[Mathf.Clamp((int)kind, 0, Profiles.Length - 1)];
        public static CannonRoundKind NextRound(CannonRoundKind kind) => (CannonRoundKind)(((int)kind + 1) % Profiles.Length);
        public static int GetUpgradeCost(int currentLevel) => 55 + Mathf.Clamp(currentLevel, 0, MaxSlotUpgrade) * 45;

        public static int GetDamage(GameState state, CannonSlot slot)
        {
            int baseDamage = ShipCustomizationModel.BaseCannonDamage + state.CannonLevel * ShipCustomizationModel.CannonUpgradeDamage;
            float slotBonus = 1f + state.GetCannonDamageLevel(slot) * 0.12f;
            return Mathf.Max(1, Mathf.RoundToInt(baseDamage * slotBonus * GetProfile(state.GetCannonRound(slot)).DamageMultiplier
                * CrewManagementModel.GetCannonDamageMultiplier(state)));
        }

        public static float GetRange(GameState state, CannonSlot slot)
            => BaseRange * (1f + state.GetCannonRangeLevel(slot) * 0.07f) * GetProfile(state.GetCannonRound(slot)).RangeMultiplier;

        public static float GetReloadSeconds(GameState state, CannonSlot slot)
        {
            float global = Mathf.Max(0.72f, 1f - state.CannonLevel * 0.055f);
            float slotSpeed = 1f + state.GetCannonReloadLevel(slot) * 0.11f;
            return Mathf.Max(0.34f, BaseReloadSeconds * global * GetProfile(state.GetCannonRound(slot)).ReloadMultiplier
                * CrewManagementModel.GetReloadMultiplier(state) / slotSpeed);
        }
    }
}
