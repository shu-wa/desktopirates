using System;
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
        public int Hull = 100;
        public int MaxHull = 100;
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
        private readonly int[] perkInventory = new int[CrewManagementModel.PerkCount * PerkRankModel.RankCount];
        private readonly int[] equippedPerkStacks = new int[CrewManagementModel.RoleCount * CrewManagementModel.PerkCount * PerkRankModel.RankCount];
        public float CrewPerformanceMultiplier { get; set; } = 1f;
        public float SpeedStatusMultiplier { get; set; } = 1f;
        public float TurnStatusMultiplier { get; set; } = 1f;

        public GameState()
        {
            crewByRole[(int)CrewRole.Sails] = 1;
            crewByRole[(int)CrewRole.Anchor] = 1;
        }

        public static GameState CreateRandomVoyage()
        {
            int seed = Guid.NewGuid().GetHashCode() ^ unchecked((int)DateTime.UtcNow.Ticks);
            if (seed == 0 || seed == DefaultWorldSeed) seed ^= 0x51A7C0DE;
            return new GameState { WorldSeed = seed };
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
        public int GetPerkCount(CrewPerk perk)
        {
            int total = 0;
            for (int rank = 0; rank < PerkRankModel.RankCount; rank++) total += GetPerkCount(perk, (PerkRank)rank);
            return total;
        }

        public int GetPerkCount(CrewPerk perk, PerkRank rank)
            => perkInventory[PerkIndex(perk, rank)];

        public void SetPerkCount(CrewPerk perk, int amount)
        {
            for (int rank = 0; rank < PerkRankModel.RankCount; rank++) perkInventory[PerkIndex(perk, (PerkRank)rank)] = 0;
            SetPerkCount(perk, PerkRank.I, amount);
        }

        public void SetPerkCount(CrewPerk perk, PerkRank rank, int amount)
            => perkInventory[PerkIndex(perk, rank)] = Mathf.Max(0, amount);

        public void AddPerk(CrewPerk perk, int amount = 1)
            => AddPerk(perk, PerkRank.I, amount);

        public void AddPerk(CrewPerk perk, PerkRank rank, int amount = 1)
        {
            if (perk == CrewPerk.None || amount <= 0) return;
            int index = PerkIndex(perk, rank);
            perkInventory[index] += amount;
        }
        public CrewPerk GetEquippedPerk(CrewRole role)
        {
            for (int i = 1; i < CrewManagementModel.PerkCount; i++)
                if (GetEquippedPerkCount(role, (CrewPerk)i) > 0) return (CrewPerk)i;
            return CrewPerk.None;
        }

        public int GetEquippedPerkCount(CrewRole role, CrewPerk perk)
        {
            int total = 0;
            for (int rank = 0; rank < PerkRankModel.RankCount; rank++) total += GetEquippedPerkCount(role, perk, (PerkRank)rank);
            return total;
        }

        public int GetEquippedPerkCount(CrewRole role, CrewPerk perk, PerkRank rank)
            => equippedPerkStacks[EquippedIndex(role, perk, rank)];

        public int GetEquippedPerkTotal(CrewRole role)
        {
            int total = 0;
            for (int i = 1; i < CrewManagementModel.PerkCount; i++) total += GetEquippedPerkCount(role, (CrewPerk)i);
            return total;
        }

        public void SetEquippedPerkCount(CrewRole role, CrewPerk perk, int amount)
        {
            if (perk == CrewPerk.None) return;
            for (int rank = 0; rank < PerkRankModel.RankCount; rank++) equippedPerkStacks[EquippedIndex(role, perk, (PerkRank)rank)] = 0;
            SetEquippedPerkCount(role, perk, PerkRank.I, amount);
        }

        public void SetEquippedPerkCount(CrewRole role, CrewPerk perk, PerkRank rank, int amount)
        {
            if (perk == CrewPerk.None) return;
            equippedPerkStacks[EquippedIndex(role, perk, rank)] = Mathf.Clamp(amount, 0, GetPerkCount(perk, rank));
        }

        public bool TryEquipPerkStack(CrewRole role, CrewPerk perk)
            => TryEquipPerkStack(role, perk, out _);

        public bool TryEquipPerkStack(CrewRole role, CrewPerk perk, out PerkRank equippedRank)
        {
            equippedRank = PerkRank.I;
            if (perk == CrewPerk.None || GetEquippedPerkTotal(role) >= GetRoleCrew(role)) return false;
            for (int rank = PerkRankModel.RankCount - 1; rank >= 0; rank--)
            {
                var candidate = (PerkRank)rank;
                int equipped = GetEquippedPerkCount(role, perk, candidate);
                if (equipped >= GetPerkCount(perk, candidate)) continue;
                SetEquippedPerkCount(role, perk, candidate, equipped + 1);
                equippedRank = candidate;
                return true;
            }
            return false;
        }

        public void ClearEquippedPerks(CrewRole role)
        {
            for (int i = 1; i < CrewManagementModel.PerkCount; i++)
            for (int rank = 0; rank < PerkRankModel.RankCount; rank++)
                equippedPerkStacks[EquippedIndex(role, (CrewPerk)i, (PerkRank)rank)] = 0;
        }

        public void TrimEquippedPerks(CrewRole role)
        {
            int excess = GetEquippedPerkTotal(role) - GetRoleCrew(role);
            for (int rank = 0; rank < PerkRankModel.RankCount && excess > 0; rank++)
            for (int i = CrewManagementModel.PerkCount - 1; i >= 1 && excess > 0; i--)
            {
                int index = EquippedIndex(role, (CrewPerk)i, (PerkRank)rank);
                int remove = Mathf.Min(excess, equippedPerkStacks[index]);
                equippedPerkStacks[index] -= remove;
                excess -= remove;
            }
        }

        public bool EquipPerk(CrewRole role, CrewPerk perk)
        {
            if (perk != CrewPerk.None && GetPerkCount(perk) <= 0) return false;
            ClearEquippedPerks(role);
            if (perk != CrewPerk.None) SetEquippedPerkCount(role, perk, 1);
            return true;
        }

        private static int PerkIndex(CrewPerk perk, PerkRank rank)
            => (int)perk * PerkRankModel.RankCount + (int)rank;

        private static int EquippedIndex(CrewRole role, CrewPerk perk, PerkRank rank)
            => ((int)role * CrewManagementModel.PerkCount + (int)perk) * PerkRankModel.RankCount + (int)rank;

        public static long PackChunk(int x, int y) => ((long)x << 32) | (uint)y;

        public static void UnpackChunk(long value, out int x, out int y)
        {
            x = (int)(value >> 32);
            y = (int)value;
        }
    }
}
