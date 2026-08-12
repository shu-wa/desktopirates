using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Desktopirates.Editor
{
    public static class DesktopiratesUiTextureBaker
    {
        private const string Root = "Assets/Desktopirates/Resources/Textures/UI";

        [MenuItem("desktopirates/Art/Bake UI Textures")]
        public static void BakeAll()
        {
            foreach (MenuGlyph glyph in Enum.GetValues(typeof(MenuGlyph)))
            {
                Save($"Menu/menu_{glyph.ToString().ToLowerInvariant()}.png", UiTextureFactory.CreateMenuButton(glyph, 80));
                Save($"Glyphs/glyph_{glyph.ToString().ToLowerInvariant()}.png", UiTextureFactory.CreateGlyph(glyph, 32));
            }

            foreach (PoiKind kind in Enum.GetValues(typeof(PoiKind)))
                Save($"Markers/marker_{kind.ToString().ToLowerInvariant()}.png", UiTextureFactory.CreatePoiBadge(kind, 64));

            foreach (SalvagePartKind kind in Enum.GetValues(typeof(SalvagePartKind)))
                Save($"Inventory/item_{kind.ToString().ToLowerInvariant()}.png", UiTextureFactory.CreateInventoryIcon(kind, 64));

            Save("Navigation/speed_segment_active.png", UiTextureFactory.CreateSpeedSegment(true));
            Save("Navigation/speed_segment_inactive.png", UiTextureFactory.CreateSpeedSegment(false));
            Save("Navigation/speed_needle.png", UiTextureFactory.CreateSpeedNeedle());
            Save("Navigation/compass_arrow.png", UiTextureFactory.CreateCompassArrow());

            Sprite panel = UiTextureFactory.CreatePanelSprite(128);
            Save("Chrome/panel_circle.png", panel.texture);
            UnityEngine.Object.DestroyImmediate(panel);
            Sprite pill = UiTextureFactory.CreatePillSprite();
            Save("Chrome/pill_9slice.png", pill.texture);
            UnityEngine.Object.DestroyImmediate(pill);
            Sprite diamond = UiTextureFactory.CreateDiamondSprite(24);
            Save("Chrome/slider_diamond.png", diamond.texture);
            UnityEngine.Object.DestroyImmediate(diamond);

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("desktopirates UI texture library baked.");
        }

        public static void BakeAllBatch()
        {
            try
            {
                BakeAll();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void Save(string relativePath, Texture2D texture)
        {
            string assetPath = $"{Root}/{relativePath}";
            string directory = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllBytes(assetPath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(assetPath) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Point;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 256;
                importer.SaveAndReimport();
            }
        }
    }
}
