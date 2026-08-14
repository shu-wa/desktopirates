using UnityEngine;

namespace Desktopirates
{
    public enum CrewRole : byte { Cannons, Helm, Sails, Anchor }
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
        Stormwise
    }

    public static class CrewManagementModel
    {
        public const int RoleCount = 4;
        public const int PerkCount = 14;

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
            return true;
        }

        public static float GetCannonDamageMultiplier(GameState state)
            => state.GetEquippedPerk(CrewRole.Cannons) == CrewPerk.PowderExpert ? 1.25f : 1f;
        public static float GetReloadMultiplier(GameState state)
            => state.GetEquippedPerk(CrewRole.Cannons) == CrewPerk.FastHands ? 0.78f : 1f;
        public static bool HasIncendiaryRounds(GameState state)
            => state.GetEquippedPerk(CrewRole.Cannons) == CrewPerk.Firebrand;
        public static float GetTurningMultiplier(GameState state)
            => state.GetEquippedPerk(CrewRole.Helm) == CrewPerk.Helmsman ? 1.18f : 1f;
        public static float GetSailSpeedMultiplier(GameState state)
            => state.GetEquippedPerk(CrewRole.Sails) == CrewPerk.WindWhisperer ? 1.15f : 1f;
        public static float GetAnchorBrakingMultiplier(GameState state)
        {
            float staffed = state.GetRoleCrew(CrewRole.Anchor) > 0 ? 1.28f : 1f;
            return state.GetEquippedPerk(CrewRole.Anchor) == CrewPerk.AnchorMaster ? staffed * 1.35f : staffed;
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
            _ => "NO PERK"
        };

        public static bool IsCompatible(CrewRole role, CrewPerk perk)
        {
            if (perk == CrewPerk.None) return true;
            return role switch
            {
                CrewRole.Cannons => perk == CrewPerk.PowderExpert || perk == CrewPerk.FastHands || perk == CrewPerk.Firebrand,
                CrewRole.Helm => perk == CrewPerk.Helmsman || perk == CrewPerk.TideReader || perk == CrewPerk.Stormwise,
                CrewRole.Sails => perk == CrewPerk.WindWhisperer || perk == CrewPerk.ReefingMaster || perk == CrewPerk.Lookout,
                CrewRole.Anchor => perk == CrewPerk.AnchorMaster || perk == CrewPerk.Quartermaster || perk == CrewPerk.FieldSurgeon || perk == CrewPerk.Salvager,
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
