using UnityEngine;

namespace Desktopirates
{
    public enum CrewRole : byte { Cannons, Helm, Sails, Anchor, Repairer }
    public enum PerkRank : byte { I, II, III, IV }
    public enum CrewPerk : byte
    {
        None,
        PowderExpert,
        FastHands,
        Firebrand,
        Helmsman,
        TideReader,
        WindWhisperer,
        ReefingMaster,
        AnchorMaster,
        Quartermaster,
        FieldSurgeon,
        Lookout,
        Salvager,
        Stormwise,
        RapidRepair,
        ReinforcedPatch,
        ConditionSpecialist,
        VenomShot,
        FrostShot,
        TarShot
    }

    public static class PerkRankModel
    {
        public const int RankCount = 4;

        public static float GetPower(PerkRank rank) => rank switch
        {
            PerkRank.II => 1.45f,
            PerkRank.III => 2.05f,
            PerkRank.IV => 2.85f,
            _ => 1f
        };

        public static float GetStatusChance(PerkRank rank) => rank switch
        {
            PerkRank.II => 0.38f,
            PerkRank.III => 0.62f,
            PerkRank.IV => 0.86f,
            _ => 0.20f
        };

        public static string GetLabel(PerkRank rank) => $"R{(int)rank + 1}";
    }

    public static class CrewManagementModel
    {
        public const int RoleCount = 5;
        public const int PerkCount = 20;

        public static int GetAssignedTotal(GameState state)
        {
            int total = 0;
            for (int i = 0; i < RoleCount; i++) total += state.GetRoleCrew((CrewRole)i);
            return total;
        }

        public static int GetUnassigned(GameState state) => Mathf.Max(0, state.Crew - GetAssignedTotal(state));

        public static bool AssignOne(GameState state, CrewRole role)
        {
            if (GetUnassigned(state) <= 0) return false;
            state.SetRoleCrew(role, state.GetRoleCrew(role) + 1);
            return true;
        }

        public static bool UnassignOne(GameState state, CrewRole role)
        {
            int current = state.GetRoleCrew(role);
            if (current <= 0) return false;
            state.SetRoleCrew(role, current - 1);
            state.TrimEquippedPerks(role);
            return true;
        }

        public static float GetCannonDamageMultiplier(GameState state)
            => 1f + GetEffectiveStacks(state, CrewRole.Cannons, CrewPerk.PowderExpert) * 0.25f;
        public static float GetReloadMultiplier(GameState state)
            => 1f / (1f + GetEffectiveStacks(state, CrewRole.Cannons, CrewPerk.FastHands) * 0.28f);
        public static bool HasIncendiaryRounds(GameState state)
            => state.GetEquippedPerkCount(CrewRole.Cannons, CrewPerk.Firebrand) > 0;
        public static bool HasVenomRounds(GameState state)
            => state.GetEquippedPerkCount(CrewRole.Cannons, CrewPerk.VenomShot) > 0;
        public static bool HasFrostRounds(GameState state)
            => state.GetEquippedPerkCount(CrewRole.Cannons, CrewPerk.FrostShot) > 0;
        public static bool HasTarRounds(GameState state)
            => state.GetEquippedPerkCount(CrewRole.Cannons, CrewPerk.TarShot) > 0;
        public static float GetStatusDuration(GameState state, CrewPerk perk, float baseDuration)
            => baseDuration * (1f + Mathf.Max(0f, GetEffectiveStacks(state, CrewRole.Cannons, perk) - 1f) * 0.22f);

        public static float GetStatusProcChance(GameState state, CrewPerk perk)
        {
            float noProc = 1f;
            float performance = Mathf.Clamp01(state.CrewPerformanceMultiplier);
            for (int rank = 0; rank < PerkRankModel.RankCount; rank++)
            {
                int count = state.GetEquippedPerkCount(CrewRole.Cannons, perk, (PerkRank)rank);
                float chance = PerkRankModel.GetStatusChance((PerkRank)rank) * performance;
                for (int i = 0; i < count; i++) noProc *= 1f - chance;
            }
            return 1f - noProc;
        }

        public static bool RollStatusProc(GameState state, CrewPerk perk, ulong entropy)
        {
            float unit = (entropy & 0xFFFFFFUL) / 16777215f;
            return unit < GetStatusProcChance(state, perk);
        }
        public static float GetTurningMultiplier(GameState state)
            => 1f + GetEffectiveStacks(state, CrewRole.Helm, CrewPerk.Helmsman) * 0.18f;
        public static float GetSailSpeedMultiplier(GameState state)
            => 1f + GetEffectiveStacks(state, CrewRole.Sails, CrewPerk.WindWhisperer) * 0.15f;
        public static float GetAnchorBrakingMultiplier(GameState state)
        {
            float staffed = state.GetRoleCrew(CrewRole.Anchor) > 0 ? 1.28f : 1f;
            return staffed * (1f + GetEffectiveStacks(state, CrewRole.Anchor, CrewPerk.AnchorMaster) * 0.35f);
        }

        public static float GetRepairIntervalMultiplier(GameState state)
            => 1f / (1f + GetEffectiveStacks(state, CrewRole.Repairer, CrewPerk.RapidRepair) * 0.22f);
        public static float GetRepairAmountMultiplier(GameState state)
            => 1f + GetEffectiveStacks(state, CrewRole.Repairer, CrewPerk.ReinforcedPatch) * 0.25f;
        public static float GetConditionCureIntervalMultiplier(GameState state)
            => 1f / (1f + GetEffectiveStacks(state, CrewRole.Repairer, CrewPerk.ConditionSpecialist) * 0.30f);

        public static PerkRank GetHighestEquippedRank(GameState state, CrewRole role, CrewPerk perk)
        {
            for (int rank = PerkRankModel.RankCount - 1; rank >= 0; rank--)
                if (state.GetEquippedPerkCount(role, perk, (PerkRank)rank) > 0) return (PerkRank)rank;
            return PerkRank.I;
        }

        public static PerkRank GetHighestEquippedRank(GameState state, CrewRole role)
        {
            for (int rank = PerkRankModel.RankCount - 1; rank >= 0; rank--)
            for (int perk = 1; perk < PerkCount; perk++)
                if (state.GetEquippedPerkCount(role, (CrewPerk)perk, (PerkRank)rank) > 0) return (PerkRank)rank;
            return PerkRank.I;
        }

        private static float GetEffectiveStacks(GameState state, CrewRole role, CrewPerk perk)
        {
            float total = 0f;
            for (int rank = 0; rank < PerkRankModel.RankCount; rank++)
                total += state.GetEquippedPerkCount(role, perk, (PerkRank)rank) * PerkRankModel.GetPower((PerkRank)rank);
            return total * Mathf.Clamp(state.CrewPerformanceMultiplier, 0f, 1f);
        }

        public static string GetPerkName(CrewPerk perk) => perk switch
        {
            CrewPerk.PowderExpert => "POWDER EXPERT",
            CrewPerk.FastHands => "FAST HANDS",
            CrewPerk.Firebrand => "FIREBRAND",
            CrewPerk.Helmsman => "HELMSMAN",
            CrewPerk.TideReader => "TIDE READER",
            CrewPerk.WindWhisperer => "WIND WHISPERER",
            CrewPerk.ReefingMaster => "REEFING MASTER",
            CrewPerk.AnchorMaster => "ANCHOR MASTER",
            CrewPerk.Quartermaster => "QUARTERMASTER",
            CrewPerk.FieldSurgeon => "FIELD SURGEON",
            CrewPerk.Lookout => "LOOKOUT",
            CrewPerk.Salvager => "SALVAGER",
            CrewPerk.Stormwise => "STORMWISE",
            CrewPerk.RapidRepair => "RAPID REPAIR",
            CrewPerk.ReinforcedPatch => "REINFORCED PATCH",
            CrewPerk.ConditionSpecialist => "CONDITION SPECIALIST",
            CrewPerk.VenomShot => "VENOM SHOT",
            CrewPerk.FrostShot => "FROST SHOT",
            CrewPerk.TarShot => "TAR SHOT",
            _ => "NO PERK"
        };

        public static bool IsCompatible(CrewRole role, CrewPerk perk)
        {
            if (perk == CrewPerk.None) return true;
            return role switch
            {
                CrewRole.Cannons => perk == CrewPerk.PowderExpert || perk == CrewPerk.FastHands || perk == CrewPerk.Firebrand || perk == CrewPerk.VenomShot || perk == CrewPerk.FrostShot || perk == CrewPerk.TarShot,
                CrewRole.Helm => perk == CrewPerk.Helmsman || perk == CrewPerk.TideReader || perk == CrewPerk.Stormwise,
                CrewRole.Sails => perk == CrewPerk.WindWhisperer || perk == CrewPerk.ReefingMaster || perk == CrewPerk.Lookout,
                CrewRole.Anchor => perk == CrewPerk.AnchorMaster || perk == CrewPerk.Quartermaster || perk == CrewPerk.FieldSurgeon || perk == CrewPerk.Salvager,
                CrewRole.Repairer => perk == CrewPerk.RapidRepair || perk == CrewPerk.ReinforcedPatch || perk == CrewPerk.ConditionSpecialist,
                _ => false
            };
        }
    }

    public static class ProvisionModel
    {
        public const float ConsumptionInterval = 120f;
        public static int GetUnitsPerInterval(int crew) => Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(1, crew) / 4f));
    }
}
