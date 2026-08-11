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
    }
}
