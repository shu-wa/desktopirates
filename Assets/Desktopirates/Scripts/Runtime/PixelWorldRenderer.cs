using UnityEngine;

namespace Desktopirates
{
    /// <summary>Pixelates only the 3D camera image; ScreenSpaceOverlay UI stays crisp.</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class PixelWorldRenderer : MonoBehaviour
    {
        public const int WorldPixelScale = 3;
        private static readonly Color TransparentKey = new Color32(255, 0, 255, 255);

        private Camera worldCamera;
        private Material oceanMaskMaterial;

        private void Awake()
        {
            worldCamera = GetComponent<Camera>();
            Shader maskShader = Resources.Load<Shader>("Shaders/PixelWorldMask")
                ?? Shader.Find("Hidden/Desktopirates/PixelWorldMask");
            if (maskShader != null) oceanMaskMaterial = new Material(maskShader) { name = "Projected Ocean Pixel Mask" };
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            int width = Mathf.Max(160, source.width / WorldPixelScale);
            int height = Mathf.Max(160, source.height / WorldPixelScale);
            RenderTexture lowResolution = RenderTexture.GetTemporary(width, height, 0, source.format, RenderTextureReadWrite.sRGB);
            lowResolution.filterMode = FilterMode.Point;
            lowResolution.wrapMode = TextureWrapMode.Clamp;
            Graphics.Blit(source, lowResolution);
            if (oceanMaskMaterial != null && worldCamera != null)
            {
                Vector3 center = worldCamera.WorldToViewportPoint(Vector3.zero);
                Vector3 right = worldCamera.transform.right;
                right.y = 0f;
                right.Normalize();
                Vector3 depth = Vector3.Cross(Vector3.up, right).normalized;
                Vector3 horizontalEdge = worldCamera.WorldToViewportPoint(right * OceanDisc.Radius);
                Vector3 verticalEdge = worldCamera.WorldToViewportPoint(depth * OceanDisc.Radius);
                Vector2 radius = new Vector2(
                    Mathf.Max(0.001f, Mathf.Abs(horizontalEdge.x - center.x)),
                    Mathf.Max(0.001f, Mathf.Abs(verticalEdge.y - center.y)));
                oceanMaskMaterial.SetVector("_MaskCenter", new Vector4(center.x, center.y, 0f, 0f));
                oceanMaskMaterial.SetVector("_MaskRadius", new Vector4(radius.x, radius.y, 0f, 0f));
                oceanMaskMaterial.SetColor("_KeyColor", TransparentKey);
                Graphics.Blit(lowResolution, destination, oceanMaskMaterial);
            }
            else Graphics.Blit(lowResolution, destination);
            RenderTexture.ReleaseTemporary(lowResolution);
        }

        private void OnDestroy()
        {
            if (oceanMaskMaterial != null) Destroy(oceanMaskMaterial);
        }
    }
}
