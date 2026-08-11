using UnityEngine;

namespace Desktopirates
{
    public enum PoiKind
    {
        Enemy,
        Wreck,
        Treasure
    }

    public static class PixelTextureFactory
    {
        public static Texture2D CreateTimeOrb(float hour, int size = 96)
        {
            size = Mathf.Max(32, size);
            DayCycleState state = DayCycle.Evaluate(hour);
            var pixels = new Color32[size * size];
            float center = (size - 1) * 0.5f;
            float innerRadius = size * 0.39f;
            float outerRadius = size * 0.49f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float radius = Mathf.Sqrt(dx * dx + dy * dy);
                    Color color = Color.clear;

                    if (radius <= outerRadius)
                    {
                        if (radius > innerRadius)
                        {
                            float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                            if (angle < 0f) angle += 360f;
                            color = RimColor(angle);
                        }
                        else
                        {
                            float vertical = Mathf.InverseLerp(center - innerRadius, center + innerRadius, y);
                            color = Color.Lerp(state.SkyBottom, state.SkyTop, vertical);
                            if (y < center - innerRadius * 0.28f)
                            {
                                float seaT = Mathf.InverseLerp(center - innerRadius, center - innerRadius * 0.28f, y);
                                color = Color.Lerp(state.Water * 0.68f, state.Water, seaT);
                            }
                        }
                    }

                    pixels[y * size + x] = color;
                }
            }

            hour = DayCycle.WrapHour(hour);
            if (hour >= 5f && hour < 20f)
            {
                float daylight = Mathf.InverseLerp(5f, 20f, hour);
                int sunX = Mathf.RoundToInt(Mathf.Lerp(size * 0.26f, size * 0.74f, daylight));
                int sunY = Mathf.RoundToInt(size * (0.43f + Mathf.Sin(daylight * Mathf.PI) * 0.28f));
                DrawCircle(pixels, size, sunX, sunY, Mathf.Max(3, size / 15), new Color(1f, 0.82f, 0.28f));
            }
            else
            {
                int moonX = Mathf.RoundToInt(size * 0.60f);
                int moonY = Mathf.RoundToInt(size * 0.65f);
                int moonRadius = Mathf.Max(4, size / 12);
                DrawCircle(pixels, size, moonX, moonY, moonRadius, new Color(1f, 0.83f, 0.35f));
                DrawCircle(pixels, size, moonX + moonRadius / 2, moonY + moonRadius / 4, moonRadius, state.SkyTop);
                DrawPixelStar(pixels, size, Mathf.RoundToInt(size * 0.31f), Mathf.RoundToInt(size * 0.67f));
                DrawPixelStar(pixels, size, Mathf.RoundToInt(size * 0.73f), Mathf.RoundToInt(size * 0.52f));
                DrawPixelStar(pixels, size, Mathf.RoundToInt(size * 0.42f), Mathf.RoundToInt(size * 0.78f));
            }

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Time Orb",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        public static Texture2D CreatePoiIcon(PoiKind kind, int size = 24)
        {
            size = Mathf.Max(16, size);
            var pixels = new Color32[size * size];
            Color color = kind == PoiKind.Enemy
                ? new Color(1f, 0.34f, 0.16f)
                : kind == PoiKind.Wreck
                    ? new Color(0.48f, 0.88f, 0.56f)
                    : new Color(1f, 0.72f, 0.16f);

            int c = size / 2;
            if (kind == PoiKind.Enemy)
            {
                DrawCircle(pixels, size, c, c + 2, size / 4, color);
                DrawCircle(pixels, size, c - size / 10, c + 3, 1, Color.black);
                DrawCircle(pixels, size, c + size / 10, c + 3, 1, Color.black);
                DrawLine(pixels, size, c - 5, c - 4, c + 5, c + 5, color, 2);
                DrawLine(pixels, size, c + 5, c - 4, c - 5, c + 5, color, 2);
            }
            else if (kind == PoiKind.Wreck)
            {
                DrawLine(pixels, size, 4, 5, size - 5, size - 5, color, 3);
                DrawLine(pixels, size, size - 5, 5, 4, size - 5, color, 3);
            }
            else
            {
                FillRect(pixels, size, 4, 5, size - 8, size / 2, color);
                FillRect(pixels, size, 6, c + 1, size - 12, size / 4, color * 0.78f);
                FillRect(pixels, size, c - 1, 4, 3, size / 2, new Color(1f, 0.92f, 0.44f));
            }

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = $"{kind} Tag",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static Color RimColor(float angle)
        {
            Color dawn = new Color(1f, 0.52f, 0.31f);
            Color day = new Color(0.28f, 0.85f, 0.91f);
            Color evening = new Color(1f, 0.58f, 0.13f);
            Color night = new Color(0.24f, 0.31f, 0.68f);
            if (angle < 90f) return Color.Lerp(dawn, day, angle / 90f);
            if (angle < 180f) return Color.Lerp(day, evening, (angle - 90f) / 90f);
            if (angle < 270f) return Color.Lerp(evening, night, (angle - 180f) / 90f);
            return Color.Lerp(night, dawn, (angle - 270f) / 90f);
        }

        private static void DrawPixelStar(Color32[] pixels, int size, int x, int y)
        {
            Color color = new Color(1f, 0.88f, 0.48f);
            SetPixel(pixels, size, x, y, color);
            SetPixel(pixels, size, x - 1, y, color);
            SetPixel(pixels, size, x + 1, y, color);
            SetPixel(pixels, size, x, y - 1, color);
            SetPixel(pixels, size, x, y + 1, color);
        }

        private static void DrawCircle(Color32[] pixels, int size, int cx, int cy, int radius, Color color)
        {
            int radiusSq = radius * radius;
            for (int y = cy - radius; y <= cy + radius; y++)
            for (int x = cx - radius; x <= cx + radius; x++)
                if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= radiusSq)
                    SetPixel(pixels, size, x, y, color);
        }

        private static void FillRect(Color32[] pixels, int size, int x, int y, int width, int height, Color color)
        {
            for (int yy = y; yy < y + height; yy++)
            for (int xx = x; xx < x + width; xx++)
                SetPixel(pixels, size, xx, yy, color);
        }

        private static void DrawLine(Color32[] pixels, int size, int x0, int y0, int x1, int y1, Color color, int thickness)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            while (true)
            {
                DrawCircle(pixels, size, x0, y0, Mathf.Max(0, thickness / 2), color);
                if (x0 == x1 && y0 == y1) break;
                int e2 = err * 2;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }

        private static void SetPixel(Color32[] pixels, int size, int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= size || y >= size) return;
            pixels[y * size + x] = color;
        }
    }
}
