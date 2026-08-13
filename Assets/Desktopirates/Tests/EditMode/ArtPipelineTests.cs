using NUnit.Framework;
using UnityEngine;

namespace Desktopirates.Tests
{
    public sealed class ArtPipelineTests
    {
        [Test]
        public void MenuButton_HasTransparentCornersAndOpaqueCenter()
        {
            Texture2D texture = UiTextureFactory.CreateMenuButton(MenuGlyph.Map, 80);
            Assert.That(texture.GetPixel(0, 0).a, Is.EqualTo(0f).Within(0.01f));
            Assert.That(texture.GetPixel(40, 40).a, Is.GreaterThan(0.99f));
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void PoiBadges_KeepTransparentCorners()
        {
            foreach (PoiKind kind in System.Enum.GetValues(typeof(PoiKind)))
            {
                Texture2D texture = UiTextureFactory.CreatePoiBadge(kind, 48);
                Assert.That(texture.GetPixel(0, 0).a, Is.EqualTo(0f).Within(0.01f), kind.ToString());
                Assert.That(texture.GetPixel(24, 24).a, Is.GreaterThan(0.99f), kind.ToString());
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void OriginPort_IsVisibleFromTheStartingArea()
        {
            var events = new System.Collections.Generic.List<GeneratedEventData>();
            WorldGenerator.GenerateChunk(GameState.DefaultWorldSeed, 0, 0, events);
            GeneratedEventData port = events.Find(item => item.Kind == PoiKind.Port);
            Assert.That(Vector2.Distance(Vector2.zero, port.Position), Is.LessThan(6.8f));
        }

        [TestCase("Textures/Materials/WoodPlanks_SaltWorn_v01")]
        [TestCase("Textures/Materials/SailCanvas_Aged_v01")]
        [TestCase("Textures/Materials/HarborStone_Damp_v01")]
        public void MaterialTextures_ArePixelFilteredAndMemoryBounded(string resourcePath)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            Assert.That(texture, Is.Not.Null, resourcePath);
            Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(texture.wrapMode, Is.EqualTo(TextureWrapMode.Repeat));
            Assert.That(Mathf.Max(texture.width, texture.height), Is.LessThanOrEqualTo(512));
        }

        [TestCase("Textures/UI/Menu/menu_inventory")]
        [TestCase("Textures/UI/Markers/marker_wreck")]
        [TestCase("Textures/UI/Inventory/item_timber")]
        [TestCase("Textures/UI/Navigation/speed_segment_active")]
        [TestCase("Textures/UI/Navigation/compass_arrow")]
        public void BakedUiTextures_AreReadablePixelAssets(string resourcePath)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            Assert.That(texture, Is.Not.Null, resourcePath);
            Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(texture.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(Mathf.Max(texture.width, texture.height), Is.LessThanOrEqualTo(256));
        }

        [TestCase("Textures/UI/GeneratedPixel/ui_menu_circle_pixel_v01", 128, 128)]
        [TestCase("Textures/UI/GeneratedPixel/ui_marker_enemy_pixel_v01", 96, 96)]
        [TestCase("Textures/UI/GeneratedPixel/ui_marker_wreck_pixel_v01", 96, 96)]
        [TestCase("Textures/UI/GeneratedPixel/ui_marker_treasure_pixel_v01", 96, 96)]
        [TestCase("Textures/UI/GeneratedPixel/ui_marker_port_pixel_v01", 96, 96)]
        [TestCase("Textures/UI/GeneratedPixel/ui_button_inventory_pixel_v01", 80, 80)]
        [TestCase("Textures/UI/GeneratedPixel/ui_button_back_pixel_v01", 80, 80)]
        [TestCase("Textures/UI/GeneratedPixel/ui_telegraph_pixel_v01", 512, 128)]
        public void GeneratedPixelUiTextures_KeepAuthoredDimensionsAndFiltering(string resourcePath, int width, int height)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            Assert.That(texture, Is.Not.Null, resourcePath);
            Assert.That(texture.width, Is.EqualTo(width));
            Assert.That(texture.height, Is.EqualTo(height));
            Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(texture.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
        }

        [Test]
        public void HudSurface_IsDarkTileableAndMemoryBounded()
        {
            Texture2D texture = Resources.Load<Texture2D>("Textures/UI/Surfaces/hud_chartwood_navy_v01");
            Assert.That(texture, Is.Not.Null);
            Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(texture.wrapMode, Is.EqualTo(TextureWrapMode.Repeat));
            Assert.That(Mathf.Max(texture.width, texture.height), Is.LessThanOrEqualTo(2048));
        }

        [Test]
        public void DefaultHudPaletteMeetsSmallTextContrastGuideline()
        {
            Assert.That(UiTheme.ContrastRatio(UiTheme.PrimaryText, UiTheme.InkOpaque), Is.GreaterThanOrEqualTo(4.5f));
            Assert.That(UiTheme.ContrastRatio(UiTheme.Brass, UiTheme.InkOpaque), Is.GreaterThanOrEqualTo(4.5f));
            Assert.That(UiTheme.ContrastRatio(UiTheme.Mint, UiTheme.InkOpaque), Is.GreaterThanOrEqualTo(4.5f));
        }
    }
}
