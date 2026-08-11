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
        public readonly HashSet<long> ExploredChunks = new HashSet<long>();
        public readonly HashSet<ulong> ResolvedEvents = new HashSet<ulong>();

        public static long PackChunk(int x, int y) => ((long)x << 32) | (uint)y;

        public static void UnpackChunk(long value, out int x, out int y)
        {
            x = (int)(value >> 32);
            y = (int)value;
        }
    }
}
