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
        public bool BossCompassOwned;
        public bool AutoVoyageEnabled;
        public bool AutoCollectWrecks = true;
        public bool AutoCollectTreasures = true;
        public AutoEncounterPolicy AutoEncounterPolicy = AutoEncounterPolicy.Avoid;
        public AutoDestinationMode AutoDestinationMode = AutoDestinationMode.NearestLandmark;
        public readonly HashSet<long> ExploredChunks = new HashSet<long>();
        public readonly HashSet<ulong> ResolvedEvents = new HashSet<ulong>();
        private readonly int[] salvageParts = new int[SalvageInventory.PartKindCount];
        private readonly int[] crewByRole = new int[CrewManagementModel.RoleCount];
        private readonly byte[] cannonDamageLevels = new byte[ShipCustomizationModel.CannonSlotCount];
        private readonly byte[] cannonReloadLevels = new byte[ShipCustomizationModel.CannonSlotCount];
        private readonly byte[] cannonRangeLevels = new byte[ShipCustomizationModel.CannonSlotCount];
        private readonly byte[] cannonRounds = new byte[ShipCustomizationModel.CannonSlotCount];
        private readonly byte[] cannonCrewAssignments = new byte[ShipCustomizationModel.CannonSlotCount];
        private readonly byte[] cannonCrewPerks = new byte[ShipCustomizationModel.CannonSlotCount];
        private readonly byte[] cannonCrewPerkRanks = new byte[ShipCustomizationModel.CannonSlotCount];
        private readonly int[] perkInventory = new int[CrewManagementModel.PerkCount * PerkRankModel.RankCount];
        private readonly int[] equippedPerkStacks = new int[CrewManagementModel.RoleCount * CrewManagementModel.PerkCount * PerkRankModel.RankCount];
        private bool cannonCrewLayoutConfigured;
        public readonly CaptainRecord Captain = new CaptainRecord();
        public float CrewPerformanceMultiplier { get; set; } = 1f;
        public float SpeedStatusMultiplier { get; set; } = 1f;
        public float TurnStatusMultiplier { get; set; } = 1f;
        public float RegionSpeedMultiplier { get; set; } = 1f;
        public float RegionTurnMultiplier { get; set; } = 1f;

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

        public int GetCannonDamageLevel(CannonSlot slot) => cannonDamageLevels[(int)slot];
        public int GetCannonReloadLevel(CannonSlot slot) => cannonReloadLevels[(int)slot];
        public int GetCannonRangeLevel(CannonSlot slot) => cannonRangeLevels[(int)slot];
        public CannonRoundKind GetCannonRound(CannonSlot slot) => (CannonRoundKind)Mathf.Clamp(cannonRounds[(int)slot], 0, 2);
        public void SetCannonDamageLevel(CannonSlot slot, int level) => cannonDamageLevels[(int)slot] = (byte)Mathf.Clamp(level, 0, CannonUpgradeModel.MaxSlotUpgrade);
        public void SetCannonReloadLevel(CannonSlot slot, int level) => cannonReloadLevels[(int)slot] = (byte)Mathf.Clamp(level, 0, CannonUpgradeModel.MaxSlotUpgrade);
        public void SetCannonRangeLevel(CannonSlot slot, int level) => cannonRangeLevels[(int)slot] = (byte)Mathf.Clamp(level, 0, CannonUpgradeModel.MaxSlotUpgrade);
        public void SetCannonRound(CannonSlot slot, CannonRoundKind round) => cannonRounds[(int)slot] = (byte)Mathf.Clamp((int)round, 0, 2);

        public bool HasExplicitCannonCrewLayout => cannonCrewLayoutConfigured;
        public bool IsCannonCrewAssigned(CannonSlot slot) => cannonCrewAssignments[(int)slot] != 0;
        public CrewPerk GetCannonCrewPerk(CannonSlot slot) => (CrewPerk)Mathf.Clamp(cannonCrewPerks[(int)slot], 0, CrewManagementModel.PerkCount - 1);
        public PerkRank GetCannonCrewPerkRank(CannonSlot slot) => (PerkRank)Mathf.Clamp(cannonCrewPerkRanks[(int)slot], 0, PerkRankModel.RankCount - 1);

        public void EnsureCannonCrewLayout()
        {
            if (cannonCrewLayoutConfigured) return;
            int requested = Mathf.Max(0, GetRoleCrew(CrewRole.Cannons));
            var legacyPerks = new List<(CrewPerk perk, PerkRank rank)>();
            for (int rank = PerkRankModel.RankCount - 1; rank >= 0; rank--)
            for (int perk = 1; perk < CrewManagementModel.PerkCount; perk++)
            for (int count = GetEquippedPerkCount(CrewRole.Cannons, (CrewPerk)perk, (PerkRank)rank); count > 0; count--)
                legacyPerks.Add(((CrewPerk)perk, (PerkRank)rank));

            int assigned = 0;
            for (int i = 0; i < ShipCustomizationModel.CannonSlotCount && assigned < requested; i++)
            {
                CannonSlot slot = (CannonSlot)i;
                if (!ShipCustomizationModel.HasCannon(this, slot)) continue;
                cannonCrewAssignments[i] = 1;
                if (assigned < legacyPerks.Count)
                {
                    cannonCrewPerks[i] = (byte)legacyPerks[assigned].perk;
                    cannonCrewPerkRanks[i] = (byte)legacyPerks[assigned].rank;
                }
                assigned++;
            }
            cannonCrewLayoutConfigured = true;
            NormalizeCannonCrewLayout();
        }

        public bool TryAssignCannonCrew(CannonSlot slot)
        {
            EnsureCannonCrewLayout();
            int index = (int)slot;
            if (cannonCrewAssignments[index] != 0) return true;
            if (!ShipCustomizationModel.HasCannon(this, slot) || CrewManagementModel.GetUnassigned(this) <= 0) return false;
            cannonCrewAssignments[index] = 1;
            RebuildCannonCrewRoleAndPerks();
            return true;
        }

        public bool UnassignCannonCrew(CannonSlot slot)
        {
            EnsureCannonCrewLayout();
            int index = (int)slot;
            if (cannonCrewAssignments[index] == 0) return false;
            cannonCrewAssignments[index] = 0;
            cannonCrewPerks[index] = (byte)CrewPerk.None;
            cannonCrewPerkRanks[index] = (byte)PerkRank.I;
            RebuildCannonCrewRoleAndPerks();
            return true;
        }

        public bool TryCycleCannonCrewPerk(CannonSlot slot, out CrewPerk selectedPerk, out PerkRank selectedRank)
        {
            EnsureCannonCrewLayout();
            int index = (int)slot;
            selectedPerk = CrewPerk.None;
            selectedRank = PerkRank.I;
            if (cannonCrewAssignments[index] == 0) return false;

            int tokenCount = 1 + (CrewManagementModel.PerkCount - 1) * PerkRankModel.RankCount;
            int currentToken = GetCannonCrewPerk(slot) == CrewPerk.None
                ? 0
                : 1 + ((int)GetCannonCrewPerk(slot) - 1) * PerkRankModel.RankCount
                    + (PerkRankModel.RankCount - 1 - (int)GetCannonCrewPerkRank(slot));
            for (int step = 1; step <= tokenCount; step++)
            {
                int token = (currentToken + step) % tokenCount;
                if (token == 0)
                {
                    SetCannonCrewPerk(index, CrewPerk.None, PerkRank.I);
                    selectedPerk = CrewPerk.None;
                    return true;
                }
                int encoded = token - 1;
                CrewPerk perk = (CrewPerk)(encoded / PerkRankModel.RankCount + 1);
                PerkRank rank = (PerkRank)(PerkRankModel.RankCount - 1 - encoded % PerkRankModel.RankCount);
                if (!CrewManagementModel.IsCompatible(CrewRole.Cannons, perk)) continue;
                if (CountAssignedCannonPerks(perk, rank, index) >= GetPerkCount(perk, rank)) continue;
                SetCannonCrewPerk(index, perk, rank);
                selectedPerk = perk;
                selectedRank = rank;
                return true;
            }
            return false;
        }

        internal void LoadCannonCrewSlot(CannonSlot slot, bool assigned, CrewPerk perk, PerkRank rank)
        {
            int index = (int)slot;
            cannonCrewAssignments[index] = assigned ? (byte)1 : (byte)0;
            cannonCrewPerks[index] = assigned ? (byte)Mathf.Clamp((int)perk, 0, CrewManagementModel.PerkCount - 1) : (byte)CrewPerk.None;
            cannonCrewPerkRanks[index] = (byte)Mathf.Clamp((int)rank, 0, PerkRankModel.RankCount - 1);
            cannonCrewLayoutConfigured = true;
        }

        internal void NormalizeCannonCrewLayout()
        {
            if (!cannonCrewLayoutConfigured) return;
            int nonCannonCrew = 0;
            for (int i = 1; i < CrewManagementModel.RoleCount; i++) nonCannonCrew += GetRoleCrew((CrewRole)i);
            int remainingCrew = Mathf.Max(0, Crew - nonCannonCrew);
            var usedPerks = new int[CrewManagementModel.PerkCount * PerkRankModel.RankCount];
            int assigned = 0;
            for (int i = 0; i < ShipCustomizationModel.CannonSlotCount; i++)
            {
                CannonSlot slot = (CannonSlot)i;
                if (cannonCrewAssignments[i] == 0 || assigned >= remainingCrew || !ShipCustomizationModel.HasCannon(this, slot))
                {
                    cannonCrewAssignments[i] = 0;
                    cannonCrewPerks[i] = (byte)CrewPerk.None;
                    cannonCrewPerkRanks[i] = (byte)PerkRank.I;
                    continue;
                }
                assigned++;
                CrewPerk perk = GetCannonCrewPerk(slot);
                PerkRank rank = GetCannonCrewPerkRank(slot);
                int perkIndex = PerkIndex(perk, rank);
                if (perk == CrewPerk.None || !CrewManagementModel.IsCompatible(CrewRole.Cannons, perk)
                    || usedPerks[perkIndex] >= GetPerkCount(perk, rank))
                {
                    cannonCrewPerks[i] = (byte)CrewPerk.None;
                    cannonCrewPerkRanks[i] = (byte)PerkRank.I;
                    continue;
                }
                usedPerks[perkIndex]++;
            }
            RebuildCannonCrewRoleAndPerks();
        }

        private void SetCannonCrewPerk(int slotIndex, CrewPerk perk, PerkRank rank)
        {
            cannonCrewPerks[slotIndex] = (byte)perk;
            cannonCrewPerkRanks[slotIndex] = (byte)rank;
            RebuildCannonCrewRoleAndPerks();
        }

        private int CountAssignedCannonPerks(CrewPerk perk, PerkRank rank, int excludedSlot)
        {
            int count = 0;
            for (int i = 0; i < ShipCustomizationModel.CannonSlotCount; i++)
                if (i != excludedSlot && cannonCrewAssignments[i] != 0 && cannonCrewPerks[i] == (byte)perk && cannonCrewPerkRanks[i] == (byte)rank) count++;
            return count;
        }

        private void RebuildCannonCrewRoleAndPerks()
        {
            ClearEquippedPerks(CrewRole.Cannons);
            int assigned = 0;
            var equipped = new int[CrewManagementModel.PerkCount * PerkRankModel.RankCount];
            for (int i = 0; i < ShipCustomizationModel.CannonSlotCount; i++)
            {
                if (cannonCrewAssignments[i] == 0) continue;
                assigned++;
                CrewPerk perk = (CrewPerk)cannonCrewPerks[i];
                PerkRank rank = (PerkRank)cannonCrewPerkRanks[i];
                if (perk != CrewPerk.None) equipped[PerkIndex(perk, rank)]++;
            }
            crewByRole[(int)CrewRole.Cannons] = assigned;
            for (int perk = 1; perk < CrewManagementModel.PerkCount; perk++)
            for (int rank = 0; rank < PerkRankModel.RankCount; rank++)
                SetEquippedPerkCount(CrewRole.Cannons, (CrewPerk)perk, (PerkRank)rank, equipped[PerkIndex((CrewPerk)perk, (PerkRank)rank)]);
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
