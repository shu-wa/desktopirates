using UnityEngine;
using UnityEngine.UI;

namespace Desktopirates
{
    /// <summary>Central high-contrast visual language for the compact 720p overlay.</summary>
    public static class UiTheme
    {
        public static readonly Color Ink = new Color(0.018f, 0.050f, 0.075f, 0.98f);
        public static readonly Color InkOpaque = new Color(0.012f, 0.035f, 0.052f, 1f);
        public static readonly Color PrimaryText = new Color(0.96f, 0.98f, 0.94f, 1f);
        public static readonly Color SecondaryText = new Color(0.67f, 0.84f, 0.83f, 1f);
        public static readonly Color Brass = new Color(1f, 0.72f, 0.23f, 1f);
        public static readonly Color Mint = new Color(0.28f, 0.94f, 0.82f, 1f);
        public static readonly Color Danger = new Color(1f, 0.28f, 0.18f, 1f);
        public static readonly Color Warning = new Color(1f, 0.62f, 0.16f, 1f);

        public static void StyleText(Text text, int minimumSize = 15, bool bold = true)
        {
            if (text == null) return;
            text.fontSize = Mathf.Max(text.fontSize, minimumSize);
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            Outline outline = text.GetComponent<Outline>() ?? text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0.025f, 0.04f, 0.96f);
            outline.effectDistance = new Vector2(1.25f, -1.25f);
            outline.useGraphicAlpha = true;
        }

        public static RawImage AddChartWoodSurface(Transform parent, Vector2 size, float alpha = 0.48f, Sprite maskSprite = null)
        {
            Transform surfaceParent = parent;
            if (maskSprite != null)
            {
                var maskObject = new GameObject("Chartwood Circular Mask", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
                maskObject.transform.SetParent(parent, false);
                RectTransform maskRect = (RectTransform)maskObject.transform;
                maskRect.anchorMin = maskRect.anchorMax = maskRect.pivot = new Vector2(0.5f, 0.5f);
                maskRect.sizeDelta = size;
                Image maskImage = maskObject.GetComponent<Image>(); maskImage.sprite = maskSprite; maskImage.color = Color.white; maskImage.raycastTarget = false;
                maskObject.GetComponent<Mask>().showMaskGraphic = false;
                maskObject.transform.SetAsFirstSibling();
                surfaceParent = maskObject.transform;
            }
            var surfaceObject = new GameObject("Navy Chartwood Surface", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            surfaceObject.transform.SetParent(surfaceParent, false);
            RectTransform rect = (RectTransform)surfaceObject.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            RawImage image = surfaceObject.GetComponent<RawImage>();
            image.texture = UiTextureFactory.LoadHudSurface();
            image.color = new Color(1f, 1f, 1f, alpha);
            image.raycastTarget = false;
            image.uvRect = new Rect(0f, 0f, Mathf.Max(1f, size.x / 256f), Mathf.Max(1f, size.y / 256f));
            if (maskSprite == null) surfaceObject.transform.SetAsFirstSibling();
            return image;
        }

        public static float ContrastRatio(Color foreground, Color background)
        {
            float a = RelativeLuminance(foreground);
            float b = RelativeLuminance(background);
            return (Mathf.Max(a, b) + 0.05f) / (Mathf.Min(a, b) + 0.05f);
        }

        private static float RelativeLuminance(Color value)
            => 0.2126f * Linear(value.r) + 0.7152f * Linear(value.g) + 0.0722f * Linear(value.b);

        private static float Linear(float value)
            => value <= 0.04045f ? value / 12.92f : Mathf.Pow((value + 0.055f) / 1.055f, 2.4f);
    }
}
