using System.Collections.Generic;
using UnityEngine;

namespace Desktopirates
{
    public enum CannonSlot
    {
        Bow,
        PortFore,
        PortAft,
        StarboardFore,
        StarboardAft,
        Stern
    }

    public readonly struct CannonSlotDefinition
    {
        public readonly CannonSlot Slot;
        public readonly string ShortName;
        public readonly string ArcName;
        public readonly float Bearing;
        public readonly float HalfArc;

        public CannonSlotDefinition(CannonSlot slot, string shortName, string arcName, float bearing, float halfArc)
        {
            Slot = slot;
            ShortName = shortName;
            ArcName = arcName;
            Bearing = bearing;
            HalfArc = halfArc;
        }
    }

    public readonly struct SalvoSolution
    {
        public readonly int CannonsInArc;
        public readonly int CannonsFiring;
        public readonly string ArcName;

        public SalvoSolution(int cannonsInArc, int cannonsFiring, string arcName)
        {
            CannonsInArc = cannonsInArc;
            CannonsFiring = cannonsFiring;
            ArcName = arcName;
        }
    }

    /// <summary>Pure ship-layout, mass, performance and firing-arc rules.</summary>
    public static class ShipCustomizationModel
    {
        public const int CannonSlotCount = 6;
        public const int MaxUpgradeLevel = 5;
        public const int CannonPrice = 80;
        public const int BaseCrewHireCost = 28;
        public const float CannonMass = 2.6f;
        public const float CrewMass = 0.65f;
        public const int BaseCannonDamage = 35;
        public const int CannonUpgradeDamage = 20;

        private static readonly CannonSlotDefinition[] Definitions =
        {
            new CannonSlotDefinition(CannonSlot.Bow, "BOW", "BOW", 0f, 38f),
            new CannonSlotDefinition(CannonSlot.PortFore, "P-F", "PORT", -90f, 54f),
            new CannonSlotDefinition(CannonSlot.PortAft, "P-A", "PORT", -90f, 54f),
            new CannonSlotDefinition(CannonSlot.StarboardFore, "S-F", "STARBOARD", 90f, 54f),
            new CannonSlotDefinition(CannonSlot.StarboardAft, "S-A", "STARBOARD", 90f, 54f),
            new CannonSlotDefinition(CannonSlot.Stern, "AFT", "STERN", 180f, 38f)
        };

        public static CannonSlotDefinition GetDefinition(CannonSlot slot) => Definitions[(int)slot];

        public static bool HasCannon(GameState state, CannonSlot slot)
            => (state.CannonMountMask & (1 << (int)slot)) != 0;

        public static bool TryMountStoredCannon(GameState state, CannonSlot slot)
        {
            if (state == null || state.SpareCannons <= 0 || HasCannon(state, slot)) return false;
            if (!IsHardpointUnlocked(state, slot) || GetInstalledCannonCount(state) >= GetCannonCapacity(state)) return false;
            // Inventory is weightless by design, but a cannon becomes installed equipment
            // when it moves onto a hardpoint and must fit within the ship's load limit.
            if (!CanAddMass(state, CannonMass)) return false;
            state.SpareCannons--;
            state.CannonMountMask |= 1 << (int)slot;
            return true;
        }

        public static int GetUpgradeCap(GameState state) => ShipProgressionModel.Get(state.ShipLevel).UpgradeCap;
        public static int GetCannonCapacity(GameState state) => ShipProgressionModel.Get(state.ShipLevel).MaxCannons;
        public static int GetCrewCapacity(GameState state) => ShipProgressionModel.Get(state.ShipLevel).MaxCrew;
        public static bool IsHardpointUnlocked(GameState state, CannonSlot slot)
        {
            int level = Mathf.Clamp(state.ShipLevel, 0, ShipProgressionModel.TierCount - 1);
            int required = slot switch
            {
                CannonSlot.PortFore => 1,
                CannonSlot.StarboardFore => 1,
                CannonSlot.Bow => 0,
                CannonSlot.PortAft => 3,
                CannonSlot.StarboardAft => 4,
                CannonSlot.Stern => 5,
                _ => 5
            };
            return level >= required;
        }

        public static int GetInstalledCannonCount(GameState state)
        {
            int count = 0;
            int mask = state.CannonMountMask;
            for (int i = 0; i < CannonSlotCount; i++) if ((mask & (1 << i)) != 0) count++;
            return count;
        }

        public static float GetMass(GameState state)
        {
            float structure = 5.5f + state.ShipLevel * 5.2f + state.CapacityLevel * 1.6f;
            float machinery = state.EngineLevel * 1.25f + state.TurningLevel * 0.75f;
            float armor = state.ArmorLevel * 3.4f;
            float weapons = GetInstalledCannonCount(state) * CannonMass;
            float crew = Mathf.Max(0, state.Crew) * CrewMass;
            // Food, water, supplies, salvage, perks and spare cannons are inventory. They
            // never consume the equipment-load budget or reduce sailing performance.
            return structure + machinery + armor + weapons + crew;
        }

        public static float GetCapacity(GameState state) => 14f + state.ShipLevel * 10f + Mathf.Clamp(state.CapacityLevel, 0, GetUpgradeCap(state)) * 7.5f;

        public static float GetLoadRatio(GameState state) => GetMass(state) / Mathf.Max(1f, GetCapacity(state));

        public static float GetSpeedMultiplier(GameState state)
            => Mathf.Clamp(1.12f - GetLoadRatio(state) * 0.34f, 0.48f, 1.02f)
               * CrewManagementModel.GetSailSpeedMultiplier(state)
               * Mathf.Clamp(state.SpeedStatusMultiplier, 0.2f, 1f)
               * Mathf.Clamp(state.RegionSpeedMultiplier, 0.55f, 1.25f);

        public static float GetTurningMultiplier(GameState state)
        {
            float steeringGear = 1f + Mathf.Clamp(state.TurningLevel, 0, MaxUpgradeLevel) * 0.11f;
            float staffed = state.GetRoleCrew(CrewRole.Helm) > 0 ? 1f : 0.82f;
            return Mathf.Clamp((1.10f - GetLoadRatio(state) * 0.42f) * steeringGear * staffed * CrewManagementModel.GetTurningMultiplier(state)
                * Mathf.Clamp(state.TurnStatusMultiplier, 0.2f, 1f) * Mathf.Clamp(state.RegionTurnMultiplier, 0.45f, 1.20f), 0.20f, 1.65f);
        }

        public static bool CanAddMass(GameState state, float additionalMass)
            => GetMass(state) + Mathf.Max(0f, additionalMass) <= GetCapacity(state) + 0.001f;

        public static int GetCrewHireCost(GameState state) => BaseCrewHireCost + Mathf.Max(0, state.Crew - 2) * 9;
        public static int GetCapacityUpgradeCost(int level) => 85 + Mathf.Clamp(level, 0, MaxUpgradeLevel) * 70;
        public static int GetArmorUpgradeCost(int level) => 95 + Mathf.Clamp(level, 0, MaxUpgradeLevel) * 80;
        public static int GetTurningUpgradeCost(int level) => 80 + Mathf.Clamp(level, 0, MaxUpgradeLevel) * 65;
        public static int GetGunUpgradeCost(int level) => 100 + Mathf.Clamp(level, 0, MaxUpgradeLevel) * 75;

        public static bool IsInArc(CannonSlot slot, float relativeBearing)
        {
            CannonSlotDefinition definition = GetDefinition(slot);
            return Mathf.Abs(Mathf.DeltaAngle(definition.Bearing, relativeBearing)) <= definition.HalfArc;
        }

        public static float GetRelativeBearing(float shipHeading, Vector2 shipPosition, Vector2 targetPosition)
        {
            Vector2 direction = targetPosition - shipPosition;
            if (direction.sqrMagnitude < 0.0001f) return 0f;
            float targetHeading = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
            return Mathf.DeltaAngle(shipHeading, targetHeading);
        }

        public static SalvoSolution GetSalvo(GameState state, float relativeBearing)
        {
            int inArc = 0;
            string arcName = GetBearingName(relativeBearing);
            for (int i = 0; i < CannonSlotCount; i++)
            {
                CannonSlot slot = (CannonSlot)i;
                if (!HasCannon(state, slot) || !IsInArc(slot, relativeBearing)) continue;
                inArc++;
                arcName = GetDefinition(slot).ArcName;
            }
            return new SalvoSolution(inArc, Mathf.Min(inArc, Mathf.Max(0, state.GetRoleCrew(CrewRole.Cannons))), arcName);
        }

        public static int GetFiringSlots(GameState state, float relativeBearing, IList<CannonSlot> output)
        {
            output.Clear();
            int availableCrew = Mathf.Max(0, state.GetRoleCrew(CrewRole.Cannons));
            for (int i = 0; i < CannonSlotCount && output.Count < availableCrew; i++)
            {
                CannonSlot slot = (CannonSlot)i;
                if (HasCannon(state, slot) && IsInArc(slot, relativeBearing)) output.Add(slot);
            }
            return output.Count;
        }

        public static int GetSalvoDamage(GameState state, SalvoSolution salvo)
            => Mathf.Max(1, Mathf.RoundToInt(salvo.CannonsFiring * (BaseCannonDamage + Mathf.Clamp(state.CannonLevel, 0, GetUpgradeCap(state)) * CannonUpgradeDamage)
                * CrewManagementModel.GetCannonDamageMultiplier(state)));

        public static string GetBearingName(float relativeBearing)
        {
            float bearing = Mathf.DeltaAngle(0f, relativeBearing);
            if (Mathf.Abs(bearing) <= 45f) return "BOW";
            if (Mathf.Abs(bearing) >= 135f) return "STERN";
            return bearing < 0f ? "PORT" : "STARBOARD";
        }
    }
}
