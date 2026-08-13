using UnityEngine;

namespace Desktopirates
{
    public enum MapZoom
    {
        Local,
        Wide
    }

    /// <summary>Pure chart projection and exploration-surface rendering rules.</summary>
    public static class MapChartModel
    {
        public const int TextureSize = 384;
        public const float UiRadius = 190f;

        public static float GetWorldRadius(MapZoom zoom)
            => WorldGenerator.ChunkSize * (zoom == MapZoom.Local ? 4.5f : 9.5f);

        public static Vector2 WorldToMap(Vector2 worldPosition, Vector2 center, MapZoom zoom)
            => (worldPosition - center) / GetWorldRadius(zoom) * UiRadius;

        public static bool IsInside(Vector2 mapPosition)
            => mapPosition.sqrMagnitude <= UiRadius * UiRadius;

        public static bool ShouldRecenter(Vector2 worldPosition, Vector2 center, MapZoom zoom, float threshold = 0.72f)
            => WorldToMap(worldPosition, center, zoom).magnitude >= UiRadius * Mathf.Clamp01(threshold);

        public static Texture2D CreateTexture(GameState state, Vector2 center, MapZoom zoom, int size = TextureSize)
        {
            int textureSize = Mathf.Max(64, size);
            var pixels = new Color32[textureSize * textureSize];
            float pixelCenter = (textureSize - 1) * 0.5f;
            float pixelRadius = textureSize * 0.485f;
            float worldRadius = GetWorldRadius(zoom);
            float worldPerPixel = worldRadius / pixelRadius;
            Color32 outside = new Color32(0, 0, 0, 0);
            Color32 unknownA = new Color32(5, 18, 28, 255);
            Color32 unknownB = new Color32(7, 25, 36, 255);
            Color32 knownA = new Color32(15, 66, 76, 255);
            Color32 knownB = new Color32(18, 79, 88, 255);
            Color32 grid = new Color32(47, 130, 135, 255);
            Color32 rangeRing = new Color32(117, 124, 101, 180);
            Color32 axes = new Color32(191, 133, 45, 210);

            for (int y = 0; y < textureSize; y++)
            for (int x = 0; x < textureSize; x++)
            {
                float dx = x - pixelCenter;
                float dy = y - pixelCenter;
                float radius01 = Mathf.Sqrt(dx * dx + dy * dy) / pixelRadius;
                int index = y * textureSize + x;
                if (radius01 > 1f) { pixels[index] = outside; continue; }

                Vector2 world = center + new Vector2(dx, dy) * worldPerPixel;
                int chunkX = Mathf.FloorToInt(world.x / WorldGenerator.ChunkSize);
                int chunkY = Mathf.FloorToInt(world.y / WorldGenerator.ChunkSize);
                bool known = state.ExploredChunks.Contains(GameState.PackChunk(chunkX, chunkY));
                bool checker = ((chunkX + chunkY) & 1) == 0;
                Color32 color = known ? (checker ? knownA : knownB) : (checker ? unknownA : unknownB);

                float localX = Mathf.Repeat(world.x, WorldGenerator.ChunkSize);
                float localY = Mathf.Repeat(world.y, WorldGenerator.ChunkSize);
                float gridWidth = Mathf.Max(worldPerPixel * 0.80f, 0.30f);
                if (known && (localX < gridWidth || localY < gridWidth)) color = grid;
                if (Mathf.Abs(radius01 - 0.5f) < 0.006f || Mathf.Abs(radius01 - 0.75f) < 0.006f) color = rangeRing;
                if (Mathf.Abs(dx) < 0.75f || Mathf.Abs(dy) < 0.75f) color = axes;
                pixels[index] = color;
            }

            var texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
            {
                name = $"Exploration Chart {zoom}",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }
    }
}
