using UnityEngine;

namespace Desktopirates
{
    public enum SeaRegionKind : byte { Calm, Tempest, Miasma, Frostwake, TarSea, EmberCurrent }

    public readonly struct SeaRegionProfile
    {
        public readonly SeaRegionKind Kind;
        public readonly string Name;
        public readonly string EffectLabel;
        public readonly float SpeedMultiplier;
        public readonly float TurnMultiplier;
        public readonly float EnemySpeedMultiplier;
        public readonly float EnemyReloadMultiplier;
        public readonly Color OceanTint;

        public SeaRegionProfile(SeaRegionKind kind, string name, string effect, float speed, float turn, float enemySpeed, float enemyReload, Color tint)
        {
            Kind = kind;
            Name = name;
            EffectLabel = effect;
            SpeedMultiplier = speed;
            TurnMultiplier = turn;
            EnemySpeedMultiplier = enemySpeed;
            EnemyReloadMultiplier = enemyReload;
            OceanTint = tint;
        }
    }

    public static class SeaRegionModel
    {
        public const int Count = 6;
        public const int RegionChunkSpan = 6;

        public static SeaRegionProfile At(int seed, Vector2 position)
        {
            int chunkX = Mathf.FloorToInt(position.x / WorldGenerator.ChunkSize);
            int chunkY = Mathf.FloorToInt(position.y / WorldGenerator.ChunkSize);
            int regionX = Mathf.FloorToInt((chunkX + RegionChunkSpan * 0.5f) / RegionChunkSpan);
            int regionY = Mathf.FloorToInt((chunkY + RegionChunkSpan * 0.5f) / RegionChunkSpan);
            if (regionX == 0 && regionY == 0) return Get(SeaRegionKind.Calm);
            ulong hash = WorldGenerator.Hash(seed, regionX, regionY, 6203);
            int roll = (int)(hash % 100UL);
            SeaRegionKind kind = roll < 18 ? SeaRegionKind.Calm : (SeaRegionKind)(1 + (int)((hash >> 9) % (ulong)(Count - 1)));
            return Get(kind);
        }

        public static SeaRegionProfile Get(SeaRegionKind kind) => kind switch
        {
            SeaRegionKind.Tempest => new SeaRegionProfile(kind, "TEMPEST BELT", "TURN -18%  ENEMY HASTE", 0.96f, 0.82f, 1.18f, 0.82f, new Color(0.10f, 0.31f, 0.43f)),
            SeaRegionKind.Miasma => new SeaRegionProfile(kind, "MIASMA SHOALS", "SPD -8%  TOXIC RAIDERS", 0.92f, 0.94f, 1.04f, 0.92f, new Color(0.16f, 0.38f, 0.27f)),
            SeaRegionKind.Frostwake => new SeaRegionProfile(kind, "FROSTWAKE", "SPD -20%  LONG RANGE", 0.80f, 0.91f, 0.94f, 0.90f, new Color(0.20f, 0.48f, 0.58f)),
            SeaRegionKind.TarSea => new SeaRegionProfile(kind, "TAR SEA", "TURN -32%  HEAVY HULLS", 0.86f, 0.68f, 0.82f, 1.08f, new Color(0.11f, 0.22f, 0.20f)),
            SeaRegionKind.EmberCurrent => new SeaRegionProfile(kind, "EMBER CURRENT", "SPD +12%  FIRE RISK", 1.12f, 1.04f, 1.12f, 0.90f, new Color(0.35f, 0.25f, 0.18f)),
            _ => new SeaRegionProfile(SeaRegionKind.Calm, "CALM WATERS", "NO MODIFIERS", 1f, 1f, 1f, 1f, new Color(0.05f, 0.36f, 0.42f))
        };
    }
}
