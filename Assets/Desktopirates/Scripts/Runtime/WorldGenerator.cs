using System;
using System.Collections.Generic;
using UnityEngine;

namespace Desktopirates
{
    public enum PoiKind
    {
        Enemy,
        Wreck,
        Treasure,
        Port
    }

    public readonly struct GeneratedEventData
    {
        public readonly ulong Id;
        public readonly PoiKind Kind;
        public readonly Vector2 Position;
        public readonly int Reward;

        public GeneratedEventData(ulong id, PoiKind kind, Vector2 position, int reward)
        {
            Id = id;
            Kind = kind;
            Position = position;
            Reward = reward;
        }
    }

    public static class WorldGenerator
    {
        public const float ChunkSize = 18f;

        public static void GenerateChunk(int seed, int chunkX, int chunkY, List<GeneratedEventData> output)
        {
            output.Clear();
            ulong root = Hash(seed, chunkX, chunkY, 0);

            // The origin always offers a safe first harbor and an obvious recovery point.
            if (chunkX == 0 && chunkY == 0)
                output.Add(new GeneratedEventData(Hash(seed, 0, 0, 99), PoiKind.Port, new Vector2(3.0f, 3.5f), 0));

            int count = (root & 7UL) < 2UL ? 0 : ((root >> 3) & 7UL) == 0UL ? 2 : 1;
            for (int i = 0; i < count; i++)
            {
                ulong h = Hash(seed, chunkX, chunkY, i + 1);
                float localX = 2.3f + Unit(h) * (ChunkSize - 4.6f);
                float localY = 2.3f + Unit(h >> 17) * (ChunkSize - 4.6f);
                Vector2 position = new Vector2(chunkX * ChunkSize + localX, chunkY * ChunkSize + localY);
                int roll = (int)((h >> 34) % 100UL);
                PoiKind kind = roll < 33 ? PoiKind.Enemy : roll < 59 ? PoiKind.Wreck : roll < 83 ? PoiKind.Treasure : PoiKind.Port;
                int reward = 18 + (int)((h >> 45) % 48UL);
                output.Add(new GeneratedEventData(Hash(seed, chunkX, chunkY, i + 11), kind, position, reward));
            }
        }

        public static ulong Hash(int seed, int x, int y, int salt)
        {
            unchecked
            {
                ulong z = (uint)seed;
                z ^= (ulong)(uint)x * 0x9E3779B185EBCA87UL;
                z ^= (ulong)(uint)y * 0xC2B2AE3D27D4EB4FUL;
                z ^= (ulong)(uint)salt * 0x165667B19E3779F9UL;
                z += 0x9E3779B97F4A7C15UL;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        private static float Unit(ulong value) => (value & 0xFFFFUL) / 65535f;
    }
}
