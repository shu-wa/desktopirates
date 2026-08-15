using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Desktopirates
{
    /// <summary>Screen-space identity and health plates for currently visible enemies.</summary>
    public sealed class EnemyHudController : MonoBehaviour
    {
        private sealed class Plate
        {
            public GameObject Root;
            public Text Identity;
            public Image Health;
        }

        private readonly Dictionary<ulong, Plate> plates = new Dictionary<ulong, Plate>();
        private readonly HashSet<ulong> seen = new HashSet<ulong>();
        private readonly List<ulong> stale = new List<ulong>();
        private RectTransform canvas;
        private RectTransform layer;
        private Camera worldCamera;
        private PoiSystem pois;
        private Font font;

        public void Initialize(RectTransform canvasRoot, Camera camera, PoiSystem poiSystem)
        {
            canvas = canvasRoot;
            worldCamera = camera;
            pois = poiSystem;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var layerObject = new GameObject("Enemy HUD Layer", typeof(RectTransform));
            layerObject.transform.SetParent(canvas, false);
            layer = (RectTransform)layerObject.transform;
            layer.anchorMin = Vector2.zero;
            layer.anchorMax = Vector2.one;
            layer.offsetMin = Vector2.zero;
            layer.offsetMax = Vector2.zero;
            layer.pivot = new Vector2(0.5f, 0.5f);
        }

        private void LateUpdate()
        {
            if (canvas == null || worldCamera == null || pois == null) return;
            seen.Clear();

            foreach (PoiRecord enemy in pois.Items)
            {
                if (enemy.Kind != PoiKind.Enemy || enemy.Resolved || enemy.Visual == null) continue;
                seen.Add(enemy.Id);
                if (!plates.TryGetValue(enemy.Id, out Plate plate))
                {
                    plate = CreatePlate(enemy);
                    plates.Add(enemy.Id, plate);
                }

                bool visible = enemy.Visual.gameObject.activeInHierarchy;
                Vector3 screen = visible
                    ? worldCamera.WorldToScreenPoint(enemy.Visual.position + Vector3.up * (enemy.Boss == BossKind.None ? 1.0f : 1.35f))
                    : Vector3.back;
                visible &= screen.z > 0f && screen.x >= 0f && screen.x <= Screen.width && screen.y >= 0f && screen.y <= Screen.height;
                plate.Root.SetActive(visible);
                if (!visible) continue;

                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, screen, null, out Vector2 local);
                RectTransform rect = (RectTransform)plate.Root.transform;
                float halfWidth = UiLayoutMetrics.EnemyPlateWidth * 0.5f;
                local.x = Mathf.Clamp(local.x, canvas.rect.xMin + halfWidth, canvas.rect.xMax - halfWidth);
                local.y = Mathf.Clamp(local.y, canvas.rect.yMin + 30f, canvas.rect.yMax - 30f);
                rect.anchoredPosition = local;

                plate.Identity.text = $"LV {enemy.Level:00}  {enemy.DisplayName}";
                plate.Identity.color = enemy.Boss == BossKind.None ? UiTheme.PrimaryText : UiTheme.Warning;
                float ratio = enemy.Health / (float)Mathf.Max(1, enemy.MaxHealth);
                plate.Health.fillAmount = Mathf.Clamp01(ratio);
                plate.Health.color = ratio <= 0.25f ? UiTheme.Danger : enemy.Boss == BossKind.None ? UiTheme.Mint : UiTheme.Warning;
            }

            stale.Clear();
            foreach (KeyValuePair<ulong, Plate> entry in plates)
                if (!seen.Contains(entry.Key)) stale.Add(entry.Key);
            foreach (ulong id in stale)
            {
                Destroy(plates[id].Root);
                plates.Remove(id);
            }
        }

        private Plate CreatePlate(PoiRecord enemy)
        {
            var rootObject = new GameObject($"Enemy HUD {enemy.Id}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            rootObject.transform.SetParent(layer, false);
            RectTransform root = (RectTransform)rootObject.transform;
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(UiLayoutMetrics.EnemyPlateWidth, UiLayoutMetrics.EnemyPlateHeight);
            Image frame = rootObject.GetComponent<Image>();
            frame.sprite = UiTextureFactory.LoadConceptSprite("Chrome", "tooltip_card", 18f);
            frame.type = Image.Type.Sliced;
            frame.color = Color.white;
            frame.raycastTarget = false;

            Text identity = CreateText(root, "Enemy Identity", new Vector2(0f, 9f), new Vector2(164f, 20f), 12);
            RectTransform trackRect = CreateRect("Enemy Health Track", root, new Vector2(0f, -14f), new Vector2(150f, 9f));
            Image track = trackRect.gameObject.AddComponent<Image>();
            track.sprite = UiTextureFactory.LoadConceptSprite("Chrome", "slider_rail", 14f);
            track.type = Image.Type.Sliced;
            track.color = Color.white;
            track.raycastTarget = false;
            RectTransform fillRect = CreateRect("Enemy Health Fill", trackRect, Vector2.zero, new Vector2(142f, 5f));
            Image fill = fillRect.gameObject.AddComponent<Image>();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.color = UiTheme.Mint;
            fill.raycastTarget = false;

            return new Plate { Root = rootObject, Identity = identity, Health = fill };
        }

        private Text CreateText(Transform parent, string name, Vector2 position, Vector2 size, int fontSize)
        {
            RectTransform rect = CreateRect(name, parent, position, size);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 9;
            text.resizeTextMaxSize = fontSize;
            UiTheme.StyleText(text, 9);
            return text;
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var item = new GameObject(name, typeof(RectTransform));
            item.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)item.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }
    }
}
