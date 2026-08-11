using UnityEditor;

namespace Desktopirates.Editor
{
    public sealed class DesktopiratesArtImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Desktopirates/")) return;
            var importer = (TextureImporter)assetImporter;
            if (assetPath.Contains("/Textures/Environment/") || assetPath.Contains("/Textures/Materials/"))
            {
                importer.textureType = TextureImporterType.Default;
                importer.filterMode = UnityEngine.FilterMode.Point;
                importer.wrapMode = UnityEngine.TextureWrapMode.Repeat;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = assetPath.Contains("/Textures/Materials/") ? 512 : 2048;
            }
            else if (assetPath.Contains("/Textures/UI/"))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.filterMode = UnityEngine.FilterMode.Point;
                importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
            }
        }
    }
}
