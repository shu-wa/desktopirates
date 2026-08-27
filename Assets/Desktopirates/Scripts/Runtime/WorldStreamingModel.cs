using UnityEngine;

namespace Desktopirates
{
    /// <summary>Shared, pure rules for the runtime window around the infinite world.</summary>
    public static class WorldStreamingModel
    {
        // The chart queries WorldGenerator directly and does not need instantiated POIs.
        // One adjacent chunk in every direction is enough to cover the 6.25-unit sea disc
        // even while the player crosses an 18-unit chunk boundary: 9 live chunks instead
        // of 81, without changing the deterministic infinite map.
        public const int LoadRadius = 1;

        public static Vector2Int GetCenterChunk(Vector2 worldPosition)
            => new Vector2Int(
                Mathf.FloorToInt(worldPosition.x / WorldGenerator.ChunkSize),
                Mathf.FloorToInt(worldPosition.y / WorldGenerator.ChunkSize));

        public static bool Contains(Vector2Int center, int chunkX, int chunkY)
            => Mathf.Abs(chunkX - center.x) <= LoadRadius
                && Mathf.Abs(chunkY - center.y) <= LoadRadius;
    }
}
