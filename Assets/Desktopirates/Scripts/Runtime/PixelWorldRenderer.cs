using UnityEngine;

namespace Desktopirates
{
    /// <summary>Pixelates only the 3D camera image; ScreenSpaceOverlay UI stays crisp.</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class PixelWorldRenderer : MonoBehaviour
    {
        public const int WorldPixelScale = 3;

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            int width = Mathf.Max(160, source.width / WorldPixelScale);
            int height = Mathf.Max(160, source.height / WorldPixelScale);
            RenderTexture lowResolution = RenderTexture.GetTemporary(width, height, 0, source.format, RenderTextureReadWrite.sRGB);
            lowResolution.filterMode = FilterMode.Point;
            lowResolution.wrapMode = TextureWrapMode.Clamp;
            Graphics.Blit(source, lowResolution);
            Graphics.Blit(lowResolution, destination);
            RenderTexture.ReleaseTemporary(lowResolution);
        }
    }
}
