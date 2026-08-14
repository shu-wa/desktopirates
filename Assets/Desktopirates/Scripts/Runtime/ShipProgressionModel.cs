using UnityEngine;

namespace Desktopirates
{
    public enum ShipTier : byte
    {
        Raft,
        SmallShip,
        SemiMediumShip,
        MediumShip,
        SemiLargeShip,
        LargeShip
    }

    public readonly struct ShipTierDefinition
    {
        public readonly ShipTier Tier;
        public readonly string Name;
        public readonly int BaseHull;
        public readonly int MaxCannons;
        public readonly int MaxCrew;
        public readonly int UpgradeCap;
        public readonly int UpgradeCost;
        public readonly float HullScale;

        public ShipTierDefinition(ShipTier tier, string name, int hull, int cannons, int crew, int cap, int cost, float scale)
        {
            Tier = tier; Name = name; BaseHull = hull; MaxCannons = cannons; MaxCrew = crew;
            UpgradeCap = cap; UpgradeCost = cost; HullScale = scale;
        }
    }

    public static class ShipProgressionModel
    {
        public const int TierCount = 6;
        private static readonly ShipTierDefinition[] Tiers =
        {
            new ShipTierDefinition(ShipTier.Raft, "RAFT", 6, 0, 2, 0, 0, 0.72f),
            new ShipTierDefinition(ShipTier.SmallShip, "SMALL SHIP", 10, 2, 4, 1, 180, 0.88f),
            new ShipTierDefinition(ShipTier.SemiMediumShip, "SEMI-MEDIUM", 16, 3, 6, 2, 380, 1.00f),
            new ShipTierDefinition(ShipTier.MediumShip, "MEDIUM SHIP", 24, 4, 9, 3, 700, 1.12f),
            new ShipTierDefinition(ShipTier.SemiLargeShip, "SEMI-LARGE", 34, 5, 13, 4, 1100, 1.24f),
            new ShipTierDefinition(ShipTier.LargeShip, "LARGE SHIP", 48, 6, 18, 5, 1750, 1.38f)
        };

        public static ShipTierDefinition Get(int level) => Tiers[Mathf.Clamp(level, 0, TierCount - 1)];
        public static bool IsMax(int level) => level >= TierCount - 1;
        public static int GetMaxHull(GameState state) => Get(state.ShipLevel).BaseHull + Mathf.Max(0, state.ArmorLevel) * 3;

        public static bool CanUpgrade(GameState state)
            => state != null && !IsMax(state.ShipLevel) && state.Gold >= Get(state.ShipLevel + 1).UpgradeCost;

        public static bool Upgrade(GameState state)
        {
            if (!CanUpgrade(state)) return false;
            ShipTierDefinition next = Get(state.ShipLevel + 1);
            int oldMax = state.MaxHull;
            state.Gold -= next.UpgradeCost;
            state.ShipLevel++;
            state.MaxHull = GetMaxHull(state);
            state.Hull = Mathf.Min(state.MaxHull, state.Hull + Mathf.Max(0, state.MaxHull - oldMax));
            return true;
        }
    }
}
