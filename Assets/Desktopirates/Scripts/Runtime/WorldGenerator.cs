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
        public readonly EnemyArchetype EnemyArchetype;

        public GeneratedEventData(ulong id, PoiKind kind, Vector2 position, int reward, BossKind boss = BossKind.None, EnemyArchetype enemyArchetype = EnemyArchetype.Corsair)
        {
            Id = id;
            Kind = kind;
            Position = position;
            Reward = reward;
            Boss = boss;
            EnemyArchetype = enemyArchetype;
        }
    }

    public static class WorldGenerator
    {
        public const float ChunkSize = 18f;
        // Bosses are intentionally much rarer than ordinary landmarks. One repeatable
        // anchor per 28x28 chunks makes blind discovery possible, but exceptional; the
        // purchasable boss compass is the dependable way to hunt them.
        public const int BossSectorSize = 28;
        public const int MinimumInfiniteBossRange = 8;

        public static void GenerateChunk(int seed, int chunkX, int chunkY, List<GeneratedEventData> output)
        {
            output.Clear();
            ulong root = Hash(seed, chunkX, chunkY, 0);

            // The origin always offers a safe first harbor and an obvious recovery point.
            if (chunkX == 0 && chunkY == 0)
                output.Add(new GeneratedEventData(Hash(seed, 0, 0, 99), PoiKind.Port, new Vector2(3.0f, 3.5f), 0));

            int densityRoll = (int)(root & 15UL);
            // Autopilot makes long passages practical. A quieter distribution gives
            // discoveries breathing room: 37.5% empty, 50% single, 12.5% double.
            int count = densityRoll < 6 ? 0 : densityRoll < 14 ? 1 : 2;
            for (int i = 0; i < count; i++)
            {
                ulong h = Hash(seed, chunkX, chunkY, i + 1);
                float localX = 2.3f + Unit(h) * (ChunkSize - 4.6f);
                float localY = 2.3f + Unit(h >> 17) * (ChunkSize - 4.6f);
                Vector2 position = new Vector2(chunkX * ChunkSize + localX, chunkY * ChunkSize + localY);
                int roll = (int)((h >> 34) % 100UL);
                PoiKind kind = roll < 33 ? PoiKind.Enemy : roll < 59 ? PoiKind.Wreck : roll < 83 ? PoiKind.Treasure : PoiKind.Port;
                int reward = 18 + (int)((h >> 45) % 48UL);
                EnemyArchetype archetype = EnemyArchetypeModel.Roll(h, position);
                if (kind == PoiKind.Enemy)
                {
                    SeaRegionKind region = SeaRegionModel.At(seed, position).Kind;
                    int regionalRoll = (int)((h >> 22) % 100UL);
                    if (region == SeaRegionKind.Miasma && regionalRoll < 52) archetype = EnemyArchetype.PlagueRaider;
                    else if (region == SeaRegionKind.Frostwake && regionalRoll < 52) archetype = EnemyArchetype.FrostCutter;
                    else if (region == SeaRegionKind.EmberCurrent && regionalRoll < 52) archetype = EnemyArchetype.FireRaider;
                    else if (region == SeaRegionKind.TarSea && regionalRoll < 45) archetype = EnemyArchetype.Ironclad;
                    else if (region == SeaRegionKind.Tempest && regionalRoll < 45) archetype = EnemyArchetype.Skirmisher;
                }
                output.Add(new GeneratedEventData(Hash(seed, chunkX, chunkY, i + 11), kind, position, reward, BossKind.None, archetype));
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

            // Every distant infinite sector owns one deterministic boss anchor, so bosses
            // remain farmable without becoming common roadside encounters.
            int sectorX = FloorDiv(chunkX, BossSectorSize);
            int sectorY = FloorDiv(chunkY, BossSectorSize);
            GetSectorBossChunk(seed, sectorX, sectorY, out int sectorBossX, out int sectorBossY, out BossKind sectorBoss);
            int range = Mathf.Max(Mathf.Abs(chunkX), Mathf.Abs(chunkY));
            if (!hasBoss
                && !SectorContainsGuaranteedBoss(seed, sectorX, sectorY)
                && range >= MinimumInfiniteBossRange
                && chunkX == sectorBossX && chunkY == sectorBossY)
            {
                ulong sectorHash = Hash(seed, sectorX, sectorY, 6401);
                AddBoss(seed, chunkX, chunkY, sectorBoss, 6500 + (int)(sectorHash & 1023UL), output);
            }
        }

        public static void GetGuaranteedBossChunk(int seed, BossKind boss, out int chunkX, out int chunkY)
        {
            int[] rings = { 0, 10, 18, 28, 40 };
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

        public static bool TryFindNearestBoss(int seed, Vector2 origin, ISet<ulong> resolvedEvents, out GeneratedEventData nearest)
        {
            nearest = default;
            GeneratedEventData best = default;
            float bestDistanceSquared = float.MaxValue;
            bool found = false;

            bool Consider(GeneratedEventData candidate)
            {
                if (resolvedEvents != null && resolvedEvents.Contains(candidate.Id)) return false;
                float distanceSquared = (candidate.Position - origin).sqrMagnitude;
                if (distanceSquared < bestDistanceSquared)
                {
                    bestDistanceSquared = distanceSquared;
                    best = candidate;
                    found = true;
                }
                return true;
            }

            for (int value = (int)BossKind.GangAdmiral; value <= (int)BossKind.Poseidon; value++)
            {
                GetGuaranteedBossChunk(seed, (BossKind)value, out int x, out int y);
                Consider(CreateBossEvent(seed, x, y, (BossKind)value, 700 + value));
            }

            Vector2Int centerChunk = WorldStreamingModel.GetCenterChunk(origin);
            int centerSectorX = FloorDiv(centerChunk.x, BossSectorSize);
            int centerSectorY = FloorDiv(centerChunk.y, BossSectorSize);
            int firstUnresolvedSectorRing = -1;
            // Expand until an undefeated repeatable boss is found, then inspect two more
            // rings so a sector-corner captain still receives the genuinely nearest signal.
            for (int ring = 0; ring <= 128 && (firstUnresolvedSectorRing < 0 || ring <= firstUnresolvedSectorRing + 2); ring++)
            {
                for (int sectorY = centerSectorY - ring; sectorY <= centerSectorY + ring; sectorY++)
                for (int sectorX = centerSectorX - ring; sectorX <= centerSectorX + ring; sectorX++)
                {
                    if (Mathf.Max(Mathf.Abs(sectorX - centerSectorX), Mathf.Abs(sectorY - centerSectorY)) != ring) continue;
                    if (SectorContainsGuaranteedBoss(seed, sectorX, sectorY)) continue;
                    GetSectorBossChunk(seed, sectorX, sectorY, out int x, out int y, out BossKind boss);
                    if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) < MinimumInfiniteBossRange) continue;
                    ulong sectorHash = Hash(seed, sectorX, sectorY, 6401);
                    if (Consider(CreateBossEvent(seed, x, y, boss, 6500 + (int)(sectorHash & 1023UL)))
                        && firstUnresolvedSectorRing < 0)
                        firstUnresolvedSectorRing = ring;
                }
            }
            nearest = best;
            return found;
        }

        public static void GetSectorBossChunk(int seed, int sectorX, int sectorY, out int chunkX, out int chunkY, out BossKind boss)
        {
            ulong sectorHash = Hash(seed, sectorX, sectorY, 6401);
            chunkX = sectorX * BossSectorSize + (int)(sectorHash % (ulong)BossSectorSize);
            chunkY = sectorY * BossSectorSize + (int)((sectorHash >> 12) % (ulong)BossSectorSize);
            boss = (BossKind)(1 + (int)((sectorHash >> 24) % 4UL));
        }

        private static bool SectorContainsGuaranteedBoss(int seed, int sectorX, int sectorY)
        {
            for (int value = (int)BossKind.GangAdmiral; value <= (int)BossKind.Poseidon; value++)
            {
                GetGuaranteedBossChunk(seed, (BossKind)value, out int x, out int y);
                if (FloorDiv(x, BossSectorSize) == sectorX && FloorDiv(y, BossSectorSize) == sectorY) return true;
            }
            return false;
        }

        private static void AddBoss(int seed, int chunkX, int chunkY, BossKind boss, int salt, List<GeneratedEventData> output)
            => output.Add(CreateBossEvent(seed, chunkX, chunkY, boss, salt));

        private static GeneratedEventData CreateBossEvent(int seed, int chunkX, int chunkY, BossKind boss, int salt)
        {
            ulong h = Hash(seed, chunkX, chunkY, salt);
            Vector2 position = new Vector2(
                chunkX * ChunkSize + 4f + Unit(h) * (ChunkSize - 8f),
                chunkY * ChunkSize + 4f + Unit(h >> 19) * (ChunkSize - 8f));
            return new GeneratedEventData(Hash(seed, chunkX, chunkY, salt + 1), PoiKind.Enemy, position, 120 + (int)boss * 70, boss);
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
