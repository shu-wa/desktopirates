using UnityEngine;

namespace Desktopirates
{
    /// <summary>Shared, pure rules for the runtime window around the infinite world.</summary>
    public static class WorldStreamingModel
    {
        // Local chart radius is 4.5 chunks. Loading four full chunks around the player
        // keeps every near-chart landmark ready before it reaches the circular sea view.
        public const int LoadRadius = 4;

        public static Vector2Int GetCenterChunk(Vector2 worldPosition)
            => new Vector2Int(
                Mathf.FloorToInt(worldPosition.x / WorldGenerator.ChunkSize),
                Mathf.FloorToInt(worldPosition.y / WorldGenerator.ChunkSize));

        public static bool Contains(Vector2Int center, int chunkX, int chunkY)
            => Mathf.Abs(chunkX - center.x) <= LoadRadius
                && Mathf.Abs(chunkY - center.y) <= LoadRadius;
    }
}
