using System.Collections.Generic;
using UnityEngine;

namespace Desktopirates
{
    public enum MenuGlyph { Volume, Size, Map, Inventory, Save, Log, Back, Exit }

    public static class UiTextureFactory
    {
        private const string ConceptRoot = "Textures/UI/ConceptV02";
        private static readonly Dictionary<string, Sprite> ConceptSprites = new Dictionary<string, Sprite>();
        private static readonly Color32 Clear = new Color32(0, 0, 0, 0);
        private static readonly Color32 Navy = new Color32(4, 15, 27, 255);
        private static readonly Color32 NavyLight = new Color32(9, 34, 48, 255);
        private static readonly Color32 BrassDark = new Color32(104, 55, 10, 255);
        private static readonly Color32 Brass = new Color32(218, 139, 32, 255);
        private static readonly Color32 Gold = new Color32(255, 190, 61, 255);

        public static Texture2D LoadMenuButton(MenuGlyph glyph, int size = 80)
        {
            Texture2D concept = LoadConceptTexture("Icons", $"menu_{glyph.ToString().ToLowerInvariant()}");
            if (concept != null) return concept;
            return Resources.Load<Texture2D>($"Textures/UI/Menu/menu_{glyph.ToString().ToLowerInvariant()}")
                ?? CreateMenuButton(glyph, size);
        }

        public static Texture2D LoadGlyph(MenuGlyph glyph, int size = 32)
        {
            Texture2D concept = LoadConceptTexture("Icons", $"menu_{glyph.ToString().ToLowerInvariant()}");
            if (concept != null) return concept;
            return Resources.Load<Texture2D>($"Textures/UI/Glyphs/glyph_{glyph.ToString().ToLowerInvariant()}")
                ?? CreateGlyph(glyph, size);
        }

        /// <summary>Loads transparent glyph art without the circular launcher frame.</summary>
        public static Texture2D LoadFramelessMenuGlyph(MenuGlyph glyph, int size = 48)
        {
            return Resources.Load<Texture2D>($"Textures/UI/Glyphs/glyph_{glyph.ToString().ToLowerInvariant()}")
                ?? CreateGlyph(glyph, size);
        }

        public static Texture2D LoadPoiBadge(PoiKind kind, int size = 64)
        {
            Texture2D concept = LoadConceptTexture("Icons", $"marker_{kind.ToString().ToLowerInvariant()}");
            if (concept != null) return concept;
            return Resources.Load<Texture2D>($"Textures/UI/Markers/marker_{kind.ToString().ToLowerInvariant()}")
                ?? CreatePoiBadge(kind, size);
        }

        public static Texture2D LoadMenuCircleFrame() => LoadConceptTexture("Chrome", "menu_circle_frame");

        public static Texture2D LoadTimeFace(float hour)
        {
            hour = Mathf.Repeat(hour, 24f);
            string phase = hour >= 5f && hour < 10f ? "morning"
                : hour >= 10f && hour < 17f ? "noon"
                : hour >= 17f && hour < 20f ? "evening"
                : "night";
            return LoadConceptTexture("Navigation", $"time_{phase}");
        }

        public static Texture2D LoadGeneratedTelegraph()
            => Resources.Load<Texture2D>("Textures/UI/GeneratedPixel/ui_telegraph_pixel_v01");

        public static Texture2D LoadInventoryIcon(SalvagePartKind kind, int size = 64)
        {
            Texture2D concept = LoadConceptTexture("Icons", $"item_{kind.ToString().ToLowerInvariant()}");
            if (concept != null) return concept;
            return Resources.Load<Texture2D>($"Textures/UI/Inventory/item_{kind.ToString().ToLowerInvariant()}")
                ?? CreateInventoryIcon(kind, size);
        }

        public static Texture2D LoadInventoryIcon(InventoryItemKind kind)
        {
            Texture2D texture = Resources.Load<Texture2D>(InventoryManifestModel.GetTextureResource(kind));
            if (texture != null) return texture;
            return kind switch
            {
                InventoryItemKind.Timber => LoadInventoryIcon(SalvagePartKind.Timber),
                InventoryItemKind.Canvas => LoadInventoryIcon(SalvagePartKind.Canvas),
                InventoryItemKind.Iron => LoadInventoryIcon(SalvagePartKind.Iron),
                InventoryItemKind.Gear => LoadInventoryIcon(SalvagePartKind.Gear),
                InventoryItemKind.Chart => LoadInventoryIcon(SalvagePartKind.Chart),
                InventoryItemKind.Relic => LoadInventoryIcon(SalvagePartKind.Relic),
                _ => CreateGlyph(MenuGlyph.Inventory, 64)
            };
        }

        public static Texture2D LoadInventoryChrome(string part)
            => Resources.Load<Texture2D>($"Textures/UI/ConceptV04/Inventory/inventory_{part}_v04");

        public static Texture2D LoadSpeedSegment(bool active)
        {
            Texture2D concept = LoadConceptTexture("Navigation", $"speed_{(active ? "active" : "inactive")}");
            if (concept != null) return concept;
            return Resources.Load<Texture2D>($"Textures/UI/Navigation/speed_segment_{(active ? "active" : "inactive")}")
                ?? CreateSpeedSegment(active);
        }

        public static Texture2D LoadSpeedNeedle()
        {
            Texture2D concept = LoadConceptTexture("Navigation", "speed_needle");
            if (concept != null) return concept;
            return Resources.Load<Texture2D>("Textures/UI/Navigation/speed_needle") ?? CreateSpeedNeedle();
        }

        public static Texture2D LoadCompassArrow()
        {
            Texture2D concept = LoadConceptTexture("Navigation", "compass_north");
            if (concept != null) return concept;
            return Resources.Load<Texture2D>("Textures/UI/Navigation/compass_arrow") ?? CreateCompassArrow();
        }

        public static Texture2D LoadHudSurface()
        {
            return Resources.Load<Texture2D>("Textures/UI/Surfaces/hud_chartwood_navy_v01");
        }

        public static Sprite LoadPanelSprite(int size = 128)
        {
            Sprite concept = LoadConceptSprite("Chrome", "cargo_panel_frame", 0f);
            if (concept != null) return concept;
            Texture2D texture = Resources.Load<Texture2D>("Textures/UI/Chrome/panel_circle");
            return texture != null ? Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f, texture.width) : CreatePanelSprite(size);
        }

        public static Sprite LoadPillSprite()
        {
            Sprite concept = LoadConceptSprite("Chrome", "notification_pill", 18f);
            if (concept != null) return concept;
            Texture2D texture = Resources.Load<Texture2D>("Textures/UI/Chrome/pill_9slice");
            return texture != null
                ? Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f, 32f, 0, SpriteMeshType.FullRect, new Vector4(15, 15, 15, 15))
                : CreatePillSprite();
        }

        public static Sprite LoadDiamondSprite(int size = 24)
        {
            Sprite concept = LoadConceptSprite("Chrome", "slider_knob", 0f);
            if (concept != null) return concept;
            Texture2D texture = Resources.Load<Texture2D>("Textures/UI/Chrome/slider_diamond");
            return texture != null ? Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f, texture.width) : CreateDiamondSprite(size);
        }

        public static Texture2D LoadConceptTexture(string folder, string name)
            => Resources.Load<Texture2D>($"{ConceptRoot}/{folder}/{name}_v02");

        public static Sprite LoadConceptSprite(string folder, string name, float border = 0f, bool trimTransparentPadding = false)
        {
            string key = $"{folder}/{name}:{border:0.##}:{trimTransparentPadding}";
            if (ConceptSprites.TryGetValue(key, out Sprite sprite)) return sprite;
            Texture2D texture = LoadConceptTexture(folder, name);
            if (texture == null) return null;
            Vector4 borders = border > 0f ? Vector4.one * border : Vector4.zero;
            Rect spriteRect = trimTransparentPadding ? GetTrimmedConceptRect(texture, name) : new Rect(0f, 0f, texture.width, texture.height);
            sprite = Sprite.Create(texture, spriteRect, Vector2.one * 0.5f, spriteRect.width, 0, SpriteMeshType.FullRect, borders);
            ConceptSprites[key] = sprite;
            return sprite;
        }

        private static Rect GetTrimmedConceptRect(Texture2D texture, string name)
        {
            // Authored chrome keeps presentation padding around the visible frame. These measured
            // opaque bounds make sliced frames occupy their RectTransform while preserving the PNGs.
            if (name == "service_button" && texture.width == 512 && texture.height == 112) return new Rect(121f, 4f, 270f, 104f);
            if (name == "tooltip_card" && texture.width == 384 && texture.height == 256) return new Rect(50f, 4f, 284f, 248f);
            if (name == "tab_frame" && texture.width == 320 && texture.height == 80) return new Rect(92f, 4f, 136f, 72f);
            return new Rect(0f, 0f, texture.width, texture.height);
        }

        public static Texture2D LoadPortIcon(string name) => LoadConceptTexture("Port", name);

        public static Texture2D LoadFramelessPortIcon(string name)
        {
            if (name == "boss_compass") return LoadCompassArrow();
            Texture2D texture = LoadConceptTexture("PortFrameless", name);
            return texture != null ? texture : LoadPortIcon(name);
        }

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
            int u = Mathf.Max(1, size / 32);
            if (kind == PoiKind.Enemy)
            {
                DrawLine(pixels, size, center - 9 * u, center - 5 * u, center + 9 * u, center - 5 * u, accent, 2 * u);
                DrawLine(pixels, size, center - 9 * u, center - 5 * u, center - 5 * u, center - 10 * u, accent, 2 * u);
                DrawLine(pixels, size, center + 9 * u, center - 5 * u, center + 5 * u, center - 10 * u, accent, 2 * u);
                DrawLine(pixels, size, center, center - 4 * u, center, center + 10 * u, Gold, u);
                FillTriangle(pixels, size, new Vector2Int(center, center + 9 * u), new Vector2Int(center, center + 3 * u), new Vector2Int(center + 7 * u, center + 6 * u), accent);
            }
            else if (kind == PoiKind.Wreck)
            {
                DrawLine(pixels, size, center - 10 * u, center - 7 * u, center - 2 * u, center - 10 * u, accent, 2 * u);
                DrawLine(pixels, size, center + 1 * u, center - 9 * u, center + 9 * u, center - 5 * u, accent, 2 * u);
                DrawLine(pixels, size, center - 2 * u, center - 8 * u, center + 4 * u, center + 10 * u, accent, u);
                DrawLine(pixels, size, center + 3 * u, center + 3 * u, center + 9 * u, center + 6 * u, accent, u);
                FillRect(pixels, size, center - 11 * u, center - 13 * u, 3 * u, 2 * u, Gold);
                FillRect(pixels, size, center + 8 * u, center - 11 * u, 2 * u, 2 * u, Gold);
            }
            else if (kind == PoiKind.Port)
            {
                FillRect(pixels, size, center - 7 * u, center - 10 * u, 14 * u, 3 * u, accent);
                FillRect(pixels, size, center - 5 * u, center - 7 * u, 10 * u, 14 * u, accent);
                FillTriangle(pixels, size, new Vector2Int(center, center + 12 * u), new Vector2Int(center - 8 * u, center + 7 * u), new Vector2Int(center + 8 * u, center + 7 * u), Gold);
                FillRect(pixels, size, center - 2 * u, center + 2 * u, 4 * u, 4 * u, Navy);
                DrawLine(pixels, size, center - 10 * u, center - 12 * u, center + 10 * u, center - 12 * u, Gold, u);
            }
            else
            {
                FillRect(pixels, size, center - 10 * u, center - 8 * u, 20 * u, 15 * u, accent);
                DrawArc(pixels, size, center, center + 4 * u, 8 * u, 20, 160, Gold, 2 * u);
                FillRect(pixels, size, center - 2 * u, center - 9 * u, 4 * u, 17 * u, new Color32(255, 225, 103, 255));
                DrawLine(pixels, size, center - 10 * u, center, center + 10 * u, center, BrassDark, 2 * u);
            }
            return MakeTexture(pixels, size, size, $"{kind} Rim Badge");
        }

        public static Texture2D CreateInventoryIcon(SalvagePartKind kind, int size = 64)
        {
            size = Mathf.Max(32, size);
            var pixels = NewPixels(size, size);
            int c = size / 2;
            int u = Mathf.Max(1, size / 32);
            Color32 steel = new Color32(168, 185, 177, 255);
            Color32 canvas = new Color32(218, 202, 154, 255);
            Color32 wood = new Color32(153, 82, 27, 255);
            Color32 teal = new Color32(48, 187, 181, 255);

            if (kind == SalvagePartKind.Timber)
            {
                for (int i = -1; i <= 1; i++)
                {
                    FillRect(pixels, size, c - 10 * u + i * 3 * u, c - 6 * u + i * 4 * u, 18 * u, 5 * u, wood);
                    DrawLine(pixels, size, c - 8 * u + i * 3 * u, c - 4 * u + i * 4 * u, c + 6 * u + i * 3 * u, c - 4 * u + i * 4 * u, Gold, u);
                }
            }
            else if (kind == SalvagePartKind.Canvas)
            {
                FillRect(pixels, size, c - 9 * u, c - 8 * u, 18 * u, 16 * u, canvas);
                DrawLine(pixels, size, c - 7 * u, c - 5 * u, c + 7 * u, c + 5 * u, BrassDark, u);
                DrawLine(pixels, size, c - 7 * u, c + 3 * u, c + 4 * u, c - 6 * u, Brass, u);
            }
            else if (kind == SalvagePartKind.Iron)
            {
                DrawArc(pixels, size, c - 4 * u, c + 2 * u, 6 * u, 35, 315, steel, 3 * u);
                DrawArc(pixels, size, c + 5 * u, c - 3 * u, 6 * u, -145, 135, steel, 3 * u);
                DrawLine(pixels, size, c - 1 * u, c - 2 * u, c + 2 * u, c + 1 * u, Gold, 2 * u);
            }
            else if (kind == SalvagePartKind.Gear)
            {
                DrawArc(pixels, size, c, c, 8 * u, 0, 360, Brass, 3 * u);
                DrawArc(pixels, size, c, c, 3 * u, 0, 360, BrassDark, 2 * u);
                for (int i = 0; i < 8; i++)
                {
                    float a = i * Mathf.PI * 0.25f;
                    int x = c + Mathf.RoundToInt(Mathf.Cos(a) * 10 * u);
                    int y = c + Mathf.RoundToInt(Mathf.Sin(a) * 10 * u);
                    FillRect(pixels, size, x - 2 * u, y - 2 * u, 4 * u, 4 * u, Gold);
                }
            }
            else if (kind == SalvagePartKind.Chart)
            {
                FillRect(pixels, size, c - 9 * u, c - 8 * u, 18 * u, 16 * u, canvas);
                DrawLine(pixels, size, c - 7 * u, c - 5 * u, c - 2 * u, c + 5 * u, teal, u);
                DrawLine(pixels, size, c - 2 * u, c + 5 * u, c + 6 * u, c - 3 * u, teal, u);
                FillRect(pixels, size, c + 3 * u, c + 2 * u, 3 * u, 3 * u, Gold);
            }
            else
            {
                DrawArc(pixels, size, c, c, 9 * u, 0, 360, Brass, 2 * u);
                DrawArc(pixels, size, c, c, 5 * u, 0, 360, teal, 3 * u);
                DrawLine(pixels, size, c, c + 9 * u, c + 5 * u, c + 13 * u, Gold, u);
            }
            return MakeTexture(pixels, size, size, $"Inventory {kind}");
        }

        public static Texture2D CreateSpeedSegment(bool active, int width = 20, int height = 42)
        {
            var pixels = NewPixels(width, height);
            Color32 fill = active ? new Color32(52, 217, 207, 255) : new Color32(18, 50, 61, 255);
            for (int y = 2; y < height - 2; y++)
            for (int x = 2; x < width - 2; x++)
            {
                float taper = Mathf.InverseLerp(0f, height, y) * 3f;
                if (x < 2 + taper || x >= width - 2 - taper) continue;
                bool rim = x <= 3 + taper || x >= width - 4 - taper || y <= 3 || y >= height - 4;
                pixels[y * width + x] = rim ? BrassDark : fill;
            }
            return MakeTexture(pixels, width, height, active ? "Active Speed Segment" : "Inactive Speed Segment");
        }

        public static Texture2D CreateSpeedNeedle(int width = 16, int height = 62)
        {
            var pixels = NewPixels(width, height);
            int c = width / 2;
            for (int y = 4; y < height - 12; y++)
                for (int x = c - 1; x <= c + 1; x++) pixels[y * width + x] = Gold;
            for (int y = height - 14; y < height - 2; y++)
            {
                int half = Mathf.Max(1, (height - 2 - y) / 2);
                for (int x = c - half; x <= c + half; x++) pixels[y * width + x] = Gold;
            }
            return MakeTexture(pixels, width, height, "Brass Speed Needle");
        }

        public static Texture2D CreateCompassArrow(int size = 32)
        {
            var pixels = NewPixels(size, size);
            int c = size / 2;
            FillTriangle(pixels, size, new Vector2Int(c, size - 3), new Vector2Int(4, 6), new Vector2Int(size - 5, 6), Gold);
            FillTriangle(pixels, size, new Vector2Int(c, size - 8), new Vector2Int(9, 8), new Vector2Int(size - 10, 8), Navy);
            return MakeTexture(pixels, size, size, "Compass Arrowhead");
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
            else if (glyph == MenuGlyph.Inventory)
            {
                FillRect(pixels, size, c - 8 * unit, c - 4 * unit, 16 * unit, 12 * unit, color);
                DrawArc(pixels, size, c, c + 5 * unit, 6 * unit, 35, 145, color, unit + 1);
                FillRect(pixels, size, c - 6 * unit, c + 1 * unit, 12 * unit, 3 * unit, Navy);
                FillRect(pixels, size, c - 5 * unit, c - 1 * unit, 3 * unit, 3 * unit, BrassDark);
                FillRect(pixels, size, c + 2 * unit, c - 1 * unit, 3 * unit, 3 * unit, BrassDark);
            }
            else if (glyph == MenuGlyph.Save)
            {
                FillRect(pixels, size, c - 8 * unit, c - 8 * unit, 16 * unit, 16 * unit, color);
                FillRect(pixels, size, c - 4 * unit, c + 1 * unit, 8 * unit, 6 * unit, Navy);
                FillRect(pixels, size, c - 5 * unit, c - 6 * unit, 10 * unit, 5 * unit, Navy);
                FillRect(pixels, size, c + 3 * unit, c + 2 * unit, 2 * unit, 4 * unit, BrassDark);
            }
            else if (glyph == MenuGlyph.Log)
            {
                FillRect(pixels, size, c - 9 * unit, c - 8 * unit, 8 * unit, 16 * unit, color);
                FillRect(pixels, size, c + 1 * unit, c - 8 * unit, 8 * unit, 16 * unit, color);
                FillRect(pixels, size, c - 7 * unit, c - 5 * unit, 5 * unit, 1 * unit, Navy);
                FillRect(pixels, size, c + 2 * unit, c - 5 * unit, 5 * unit, 1 * unit, Navy);
                DrawLine(pixels, size, c, c - 8 * unit, c, c + 8 * unit, BrassDark, unit);
            }
            else if (glyph == MenuGlyph.Back)
            {
                DrawLine(pixels, size, c - 9 * unit, c, c + 8 * unit, c, color, unit + 1);
                DrawLine(pixels, size, c - 9 * unit, c, c - 2 * unit, c + 7 * unit, color, unit + 1);
                DrawLine(pixels, size, c - 9 * unit, c, c - 2 * unit, c - 7 * unit, color, unit + 1);
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
