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
        public int Hull = 10;
        public int MaxHull = 10;
        public int Supplies = 8;
        public int EngineLevel;
        public int CannonLevel;
        public int Crew = 3;
        public int CapacityLevel;
        public int ArmorLevel;
        public int TurningLevel;
        public int CannonMountMask = (1 << (int)CannonSlot.PortFore) | (1 << (int)CannonSlot.StarboardFore);
        public int SpareCannons;
        public readonly HashSet<long> ExploredChunks = new HashSet<long>();
        public readonly HashSet<ulong> ResolvedEvents = new HashSet<ulong>();
        private readonly int[] salvageParts = new int[SalvageInventory.PartKindCount];

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

        public static long PackChunk(int x, int y) => ((long)x << 32) | (uint)y;

        public static void UnpackChunk(long value, out int x, out int y)
        {
            x = (int)(value >> 32);
            y = (int)value;
        }
    }
}
