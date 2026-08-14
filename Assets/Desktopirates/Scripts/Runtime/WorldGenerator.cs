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

    public enum BossKind : byte { None, GangAdmiral, GhostShip, Kraken, Poseidon }

    public readonly struct GeneratedEventData
    {
        public readonly ulong Id;
        public readonly PoiKind Kind;
        public readonly Vector2 Position;
        public readonly int Reward;
        public readonly BossKind Boss;

        public GeneratedEventData(ulong id, PoiKind kind, Vector2 position, int reward, BossKind boss = BossKind.None)
        {
            Id = id;
            Kind = kind;
            Position = position;
            Reward = reward;
            Boss = boss;
        }
    }

    public static class WorldGenerator
    {
        public const float ChunkSize = 18f;
        private const int BossSectorSize = 12;

        public static void GenerateChunk(int seed, int chunkX, int chunkY, List<GeneratedEventData> output)
        {
            output.Clear();
            ulong root = Hash(seed, chunkX, chunkY, 0);

            // The origin always offers a safe first harbor and an obvious recovery point.
            if (chunkX == 0 && chunkY == 0)
                output.Add(new GeneratedEventData(Hash(seed, 0, 0, 99), PoiKind.Port, new Vector2(3.0f, 3.5f), 0));

            int densityRoll = (int)(root & 15UL);
            int count = densityRoll < 3 ? 0 : densityRoll < 12 ? 1 : densityRoll < 15 ? 2 : 3;
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

            bool hasBoss = false;
            for (int value = (int)BossKind.GangAdmiral; value <= (int)BossKind.Poseidon; value++)
            {
                BossKind boss = (BossKind)value;
                GetGuaranteedBossChunk(seed, boss, out int bossX, out int bossY);
                if (chunkX != bossX || chunkY != bossY) continue;
                AddBoss(seed, chunkX, chunkY, boss, 700 + value, output);
                hasBoss = true;
                break;
            }

            // Every infinite 12x12 sector owns one deterministic boss anchor, so bosses
            // remain farmable after the four introductory encounters are resolved.
            int sectorX = FloorDiv(chunkX, BossSectorSize);
            int sectorY = FloorDiv(chunkY, BossSectorSize);
            ulong sectorHash = Hash(seed, sectorX, sectorY, 6401);
            int sectorBossX = sectorX * BossSectorSize + (int)(sectorHash % BossSectorSize);
            int sectorBossY = sectorY * BossSectorSize + (int)((sectorHash >> 12) % BossSectorSize);
            int range = Mathf.Max(Mathf.Abs(chunkX), Mathf.Abs(chunkY));
            if (!hasBoss && range >= 4 && chunkX == sectorBossX && chunkY == sectorBossY)
            {
                BossKind boss = (BossKind)(1 + (int)((sectorHash >> 24) % 4UL));
                AddBoss(seed, chunkX, chunkY, boss, 6500 + (int)(sectorHash & 1023UL), output);
            }
        }

        public static void GetGuaranteedBossChunk(int seed, BossKind boss, out int chunkX, out int chunkY)
        {
            int[] rings = { 0, 4, 7, 11, 16 };
            int ring = rings[Mathf.Clamp((int)boss, 1, 4)];
            ulong hash = Hash(seed, (int)boss, ring, 5011);
            Vector2Int[] directions =
            {
                new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(0, 1), new Vector2Int(-1, 1),
                new Vector2Int(-1, 0), new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1)
            };
            Vector2Int direction = directions[(int)(hash % (ulong)directions.Length)];
            int jitter = (int)((hash >> 8) % 3UL) - 1;
            Vector2Int tangent = new Vector2Int(-direction.y, direction.x);
            Vector2Int coordinate = direction * ring + tangent * jitter;
            chunkX = coordinate.x;
            chunkY = coordinate.y;
        }

        private static void AddBoss(int seed, int chunkX, int chunkY, BossKind boss, int salt, List<GeneratedEventData> output)
        {
            ulong h = Hash(seed, chunkX, chunkY, salt);
            Vector2 position = new Vector2(
                chunkX * ChunkSize + 4f + Unit(h) * (ChunkSize - 8f),
                chunkY * ChunkSize + 4f + Unit(h >> 19) * (ChunkSize - 8f));
            output.Add(new GeneratedEventData(Hash(seed, chunkX, chunkY, salt + 1), PoiKind.Enemy, position, 120 + (int)boss * 70, boss));
        }

        private static int FloorDiv(int value, int divisor) => Mathf.FloorToInt(value / (float)divisor);

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
