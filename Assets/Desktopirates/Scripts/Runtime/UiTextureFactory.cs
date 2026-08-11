using UnityEngine;

namespace Desktopirates
{
    public enum MenuGlyph { Volume, Size, Map, Save, Exit }

    public static class UiTextureFactory
    {
        private static readonly Color32 Clear = new Color32(0, 0, 0, 0);
        private static readonly Color32 Navy = new Color32(4, 15, 27, 255);
        private static readonly Color32 NavyLight = new Color32(9, 34, 48, 255);
        private static readonly Color32 BrassDark = new Color32(104, 55, 10, 255);
        private static readonly Color32 Brass = new Color32(218, 139, 32, 255);
        private static readonly Color32 Gold = new Color32(255, 190, 61, 255);

        public static Texture2D CreateMenuButton(MenuGlyph glyph, int size = 80)
        {
            var pixels = NewPixels(size, size);
            float c = (size - 1) * 0.5f;
            float outer = size * 0.49f;
            float inner = size * 0.445f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - c, dy = y - c;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                if (r > outer) continue;
                if (r > inner + size * 0.022f)
                {
                    // Three-value, dithered bevel: bright upper-left rim and dark lower-right rim.
                    float light = Mathf.Clamp01((dy - dx) / size + 0.5f);
                    bool dither = ((x + y) & 3) != 0;
                    pixels[y * size + x] = light > 0.58f && dither ? Gold : light > 0.32f ? Brass : BrassDark;
                }
                else if (r > inner)
                {
                    pixels[y * size + x] = BrassDark;
                }
                else
                {
                    float vertical = Mathf.InverseLerp(-inner, inner, dy);
                    Color32 baseColor = Color32.Lerp(Navy, NavyLight, (byte)Mathf.RoundToInt(vertical * 120f));
                    if (r > inner - size * 0.035f) baseColor = new Color32(2, 9, 18, 255);
                    pixels[y * size + x] = baseColor;
                }
            }
            DrawGlyph(pixels, size, glyph, Gold);
            return MakeTexture(pixels, size, size, $"{glyph} Brass Button");
        }

        public static Texture2D CreateGlyph(MenuGlyph glyph, int size = 32)
        {
            var pixels = NewPixels(size, size);
            DrawGlyph(pixels, size, glyph, Gold);
            return MakeTexture(pixels, size, size, $"{glyph} Glyph");
        }

        public static Sprite CreatePanelSprite(int size = 128)
        {
            Texture2D texture = CreateMenuButton(MenuGlyph.Map, size);
            // Remove the icon while retaining the hand-dithered brass bezel.
            Color32[] pixels = texture.GetPixels32();
            float c = (size - 1) * 0.5f;
            float erase = size * 0.36f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                if ((x - c) * (x - c) + (y - c) * (y - c) < erase * erase)
                    pixels[y * size + x] = Navy;
            texture.SetPixels32(pixels); texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        }

        public static Sprite CreatePillSprite()
        {
            const int width = 64, height = 32;
            var pixels = NewPixels(width, height);
            float radius = height * 0.5f - 1f;
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float px = Mathf.Clamp(x, radius, width - radius - 1f);
                float dx = x - px, dy = y - (height - 1) * 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d <= radius) pixels[y * width + x] = d > radius - 2f ? BrassDark : Navy;
            }
            Texture2D texture = MakeTexture(pixels, width, height, "Navy Brass Pill");
            return Sprite.Create(texture, new Rect(0, 0, width, height), Vector2.one * 0.5f, 32f, 0, SpriteMeshType.FullRect, new Vector4(15, 15, 15, 15));
        }

        public static Sprite CreateDiamondSprite(int size = 24)
        {
            var pixels = NewPixels(size, size);
            int c = size / 2;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int distance = Mathf.Abs(x - c) + Mathf.Abs(y - c);
                if (distance <= c - 1) pixels[y * size + x] = distance >= c - 3 ? BrassDark : Gold;
            }
            Texture2D texture = MakeTexture(pixels, size, size, "Brass Diamond Handle");
            return Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        }

        public static Texture2D CreatePoiBadge(PoiKind kind, int size)
        {
            size = Mathf.Max(24, size);
            var pixels = NewPixels(size, size);
            Color32 accent = kind == PoiKind.Enemy ? new Color32(238, 67, 44, 255)
                : kind == PoiKind.Wreck ? new Color32(83, 222, 139, 255)
                : kind == PoiKind.Port ? new Color32(62, 206, 218, 255)
                : Gold;
            float c = (size - 1) * 0.5f, outer = size * 0.49f, inner = size * 0.40f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float r = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                if (r > outer) continue;
                pixels[y * size + x] = r > inner ? accent : Navy;
            }
            int center = size / 2;
            if (kind == PoiKind.Enemy)
            {
                FillRect(pixels, size, center - 6, center - 5, 12, 9, accent);
                DrawLine(pixels, size, center - 8, center + 8, center + 8, center - 8, accent, 2);
                DrawLine(pixels, size, center + 8, center + 8, center - 8, center - 8, accent, 2);
            }
            else if (kind == PoiKind.Wreck)
            {
                DrawLine(pixels, size, center - 8, center - 8, center + 8, center + 8, accent, 3);
                DrawLine(pixels, size, center + 8, center - 8, center - 8, center + 8, accent, 3);
            }
            else if (kind == PoiKind.Port)
            {
                FillRect(pixels, size, center - 9, center - 8, 18, 4, accent);
                FillRect(pixels, size, center - 7, center - 4, 4, 12, accent);
                FillRect(pixels, size, center + 3, center - 4, 4, 12, accent);
                DrawLine(pixels, size, center, center - 7, center, center + 9, Gold, 2);
            }
            else
            {
                FillRect(pixels, size, center - 9, center - 6, 18, 13, accent);
                FillRect(pixels, size, center - 2, center - 8, 4, 16, new Color32(255, 225, 103, 255));
                DrawLine(pixels, size, center - 9, center + 1, center + 9, center + 1, BrassDark, 2);
            }
            return MakeTexture(pixels, size, size, $"{kind} Rim Badge");
        }

        private static void DrawGlyph(Color32[] pixels, int size, MenuGlyph glyph, Color32 color)
        {
            int c = size / 2;
            int unit = Mathf.Max(1, size / 26);
            if (glyph == MenuGlyph.Volume)
            {
                FillRect(pixels, size, c - 11 * unit / 2, c - 3 * unit, 4 * unit, 6 * unit, color);
                FillTriangle(pixels, size, new Vector2Int(c - 2 * unit, c), new Vector2Int(c + 3 * unit, c + 5 * unit), new Vector2Int(c + 3 * unit, c - 5 * unit), color);
                DrawArc(pixels, size, c + 2 * unit, c, 6 * unit, -55, 55, color, unit);
                DrawArc(pixels, size, c + 2 * unit, c, 9 * unit, -48, 48, color, unit);
            }
            else if (glyph == MenuGlyph.Size)
            {
                int d = 7 * unit;
                DrawLine(pixels, size, c - d, c - d, c - 2 * unit, c - d, color, unit + 1);
                DrawLine(pixels, size, c - d, c - d, c - d, c - 2 * unit, color, unit + 1);
                DrawLine(pixels, size, c + d, c + d, c + 2 * unit, c + d, color, unit + 1);
                DrawLine(pixels, size, c + d, c + d, c + d, c + 2 * unit, color, unit + 1);
                DrawLine(pixels, size, c - d, c + d, c - 2 * unit, c + d, color, unit + 1);
                DrawLine(pixels, size, c - d, c + d, c - d, c + 2 * unit, color, unit + 1);
                DrawLine(pixels, size, c + d, c - d, c + 2 * unit, c - d, color, unit + 1);
                DrawLine(pixels, size, c + d, c - d, c + d, c - 2 * unit, color, unit + 1);
            }
            else if (glyph == MenuGlyph.Map)
            {
                DrawLine(pixels, size, c - 8 * unit, c - 7 * unit, c - 3 * unit, c - 4 * unit, color, unit + 1);
                DrawLine(pixels, size, c - 3 * unit, c - 4 * unit, c + 3 * unit, c - 7 * unit, color, unit + 1);
                DrawLine(pixels, size, c + 3 * unit, c - 7 * unit, c + 8 * unit, c - 4 * unit, color, unit + 1);
                DrawLine(pixels, size, c - 8 * unit, c + 7 * unit, c - 3 * unit, c + 4 * unit, color, unit + 1);
                DrawLine(pixels, size, c - 3 * unit, c + 4 * unit, c + 3 * unit, c + 7 * unit, color, unit + 1);
                DrawLine(pixels, size, c + 3 * unit, c + 7 * unit, c + 8 * unit, c + 4 * unit, color, unit + 1);
                DrawLine(pixels, size, c - 8 * unit, c - 7 * unit, c - 8 * unit, c + 7 * unit, color, unit);
                DrawLine(pixels, size, c - 3 * unit, c - 4 * unit, c - 3 * unit, c + 4 * unit, color, unit);
                DrawLine(pixels, size, c + 3 * unit, c - 7 * unit, c + 3 * unit, c + 7 * unit, color, unit);
                DrawLine(pixels, size, c + 8 * unit, c - 4 * unit, c + 8 * unit, c + 4 * unit, color, unit);
            }
            else if (glyph == MenuGlyph.Save)
            {
                FillRect(pixels, size, c - 8 * unit, c - 8 * unit, 16 * unit, 16 * unit, color);
                FillRect(pixels, size, c - 4 * unit, c + 1 * unit, 8 * unit, 6 * unit, Navy);
                FillRect(pixels, size, c - 5 * unit, c - 6 * unit, 10 * unit, 5 * unit, Navy);
                FillRect(pixels, size, c + 3 * unit, c + 2 * unit, 2 * unit, 4 * unit, BrassDark);
            }
            else
            {
                DrawArc(pixels, size, c, c, 8 * unit, -52, 232, color, unit + 1);
                DrawLine(pixels, size, c, c + 10 * unit, c, c - 1 * unit, color, unit + 1);
            }
        }

        private static Color32[] NewPixels(int width, int height)
        {
            var pixels = new Color32[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Clear;
            return pixels;
        }

        private static Texture2D MakeTexture(Color32[] pixels, int width, int height, string name)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { name = name, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            texture.SetPixels32(pixels); texture.Apply(false, false); return texture;
        }

        private static void FillRect(Color32[] pixels, int size, int x, int y, int width, int height, Color32 color)
        {
            for (int yy = y; yy < y + height; yy++) for (int xx = x; xx < x + width; xx++) SetPixel(pixels, size, xx, yy, color);
        }

        private static void DrawLine(Color32[] pixels, int size, int x0, int y0, int x1, int y1, Color32 color, int thickness)
        {
            int dx = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0), sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1, err = dx - dy;
            while (true)
            {
                for (int oy = -thickness / 2; oy <= thickness / 2; oy++) for (int ox = -thickness / 2; ox <= thickness / 2; ox++) SetPixel(pixels, size, x0 + ox, y0 + oy, color);
                if (x0 == x1 && y0 == y1) break;
                int e2 = err * 2; if (e2 > -dy) { err -= dy; x0 += sx; } if (e2 < dx) { err += dx; y0 += sy; }
            }
        }

        private static void DrawArc(Color32[] pixels, int size, int cx, int cy, int radius, int fromDeg, int toDeg, Color32 color, int thickness)
        {
            for (int degrees = fromDeg; degrees <= toDeg; degrees += 3)
            {
                float r = degrees * Mathf.Deg2Rad;
                int x = Mathf.RoundToInt(cx + Mathf.Cos(r) * radius), y = Mathf.RoundToInt(cy + Mathf.Sin(r) * radius);
                for (int oy = -thickness / 2; oy <= thickness / 2; oy++) for (int ox = -thickness / 2; ox <= thickness / 2; ox++) SetPixel(pixels, size, x + ox, y + oy, color);
            }
        }

        private static void FillTriangle(Color32[] pixels, int size, Vector2Int a, Vector2Int b, Vector2Int c, Color32 color)
        {
            int minX = Mathf.Min(a.x, Mathf.Min(b.x, c.x)), maxX = Mathf.Max(a.x, Mathf.Max(b.x, c.x));
            int minY = Mathf.Min(a.y, Mathf.Min(b.y, c.y)), maxY = Mathf.Max(a.y, Mathf.Max(b.y, c.y));
            float Sign(Vector2 p1, Vector2 p2, Vector2 p3) => (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
            for (int y = minY; y <= maxY; y++) for (int x = minX; x <= maxX; x++)
            {
                var p = new Vector2(x, y); bool n1 = Sign(p, a, b) < 0, n2 = Sign(p, b, c) < 0, n3 = Sign(p, c, a) < 0;
                if (n1 == n2 && n2 == n3) SetPixel(pixels, size, x, y, color);
            }
        }

        private static void SetPixel(Color32[] pixels, int size, int x, int y, Color32 color)
        {
            if (x >= 0 && y >= 0 && x < size && y < size) pixels[y * size + x] = color;
        }
    }
}
