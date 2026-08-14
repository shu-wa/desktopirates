using System.Collections.Generic;
using UnityEngine;

namespace Desktopirates
{
    public sealed class GameState
    {
        public const int DefaultWorldSeed = 0x44505253;

        public int WorldSeed = DefaultWorldSeed;
        public Vector2 PlayerPosition;
        public float HeadingDegrees;
        public int Gold = 120;
        public int Hull = 6;
        public int MaxHull = 6;
        public int Supplies = 8;
        public int Food = 20;
        public int Water = 20;
        public float ProvisionClock;
        public int ShipLevel;
        public int EngineLevel;
        public int CannonLevel;
        public int Crew = 2;
        public int CapacityLevel;
        public int ArmorLevel;
        public int TurningLevel;
        public int CannonMountMask;
        public int SpareCannons;
        public readonly HashSet<long> ExploredChunks = new HashSet<long>();
        public readonly HashSet<ulong> ResolvedEvents = new HashSet<ulong>();
        private readonly int[] salvageParts = new int[SalvageInventory.PartKindCount];
        private readonly int[] crewByRole = new int[CrewManagementModel.RoleCount];
        private readonly int[] perkInventory = new int[CrewManagementModel.PerkCount];
        private readonly CrewPerk[] equippedPerks = new CrewPerk[CrewManagementModel.RoleCount];

        public GameState()
        {
            crewByRole[(int)CrewRole.Sails] = 1;
            crewByRole[(int)CrewRole.Anchor] = 1;
        }

        public int TotalSalvageCount
        {
            get
            {
                int total = 0;
                for (int i = 0; i < salvageParts.Length; i++) total += salvageParts[i];
                return total;
            }
        }

        public int GetPartCount(SalvagePartKind kind) => salvageParts[(int)kind];

        public void SetPartCount(SalvagePartKind kind, int amount)
        {
            salvageParts[(int)kind] = Mathf.Max(0, amount);
        }

        public void AddPart(SalvagePartKind kind, int amount)
        {
            if (amount <= 0) return;
            salvageParts[(int)kind] = Mathf.Max(0, salvageParts[(int)kind] + amount);
        }

        public int GetRoleCrew(CrewRole role) => crewByRole[(int)role];
        public void SetRoleCrew(CrewRole role, int amount) => crewByRole[(int)role] = Mathf.Max(0, amount);
        public int GetPerkCount(CrewPerk perk) => perkInventory[(int)perk];
        public void SetPerkCount(CrewPerk perk, int amount) => perkInventory[(int)perk] = Mathf.Max(0, amount);
        public void AddPerk(CrewPerk perk, int amount = 1)
        {
            if (perk == CrewPerk.None || amount <= 0) return;
            perkInventory[(int)perk] += amount;
        }
        public CrewPerk GetEquippedPerk(CrewRole role) => equippedPerks[(int)role];
        public bool EquipPerk(CrewRole role, CrewPerk perk)
        {
            if (perk != CrewPerk.None && GetPerkCount(perk) <= 0) return false;
            equippedPerks[(int)role] = perk;
            return true;
        }

        public static long PackChunk(int x, int y) => ((long)x << 32) | (uint)y;

        public static void UnpackChunk(long value, out int x, out int y)
        {
            x = (int)(value >> 32);
            y = (int)value;
        }
    }
}
