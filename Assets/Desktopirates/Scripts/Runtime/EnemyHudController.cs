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
            public Image Background;
            public Text Identity;
            public Text HealthValue;
            public RectTransform HealthBar;
            public Image Health;
        }

        private const float HealthBarWidth = 188f;

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
                    ? worldCamera.WorldToScreenPoint(enemy.Visual.position + Vector3.up * EnemyHudModel.GetVerticalOffset(enemy.Boss != BossKind.None))
                    : Vector3.back;
                // If the mast reaches under the top dashboard, keep the plate pinned just
                // below that dashboard instead of hiding it as an off-screen element.
                visible &= screen.z > 0f && screen.x >= 0f && screen.x <= Screen.width && screen.y >= 0f;
                plate.Root.SetActive(visible);
                if (!visible) continue;

                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, screen, null, out Vector2 local);
                RectTransform rect = (RectTransform)plate.Root.transform;
                float halfWidth = UiLayoutMetrics.EnemyPlateWidth * 0.5f;
                local.x = Mathf.Clamp(local.x, canvas.rect.xMin + halfWidth, canvas.rect.xMax - halfWidth);
                local.y = Mathf.Clamp(local.y, canvas.rect.yMin + 30f,
                    canvas.rect.yMax - UiLayoutMetrics.EnemyHudTopSafeInset);
                rect.anchoredPosition = local;

                plate.Identity.text = $"LV {enemy.Level:00}  {enemy.DisplayName}";
                plate.Identity.color = enemy.Boss == BossKind.None ? UiTheme.PrimaryText : UiTheme.Warning;
                plate.HealthValue.text = EnemyHudModel.GetHealthLabel(enemy.Health, enemy.MaxHealth);
                float ratio = EnemyHudModel.GetHealthRatio(enemy.Health, enemy.MaxHealth);
                plate.HealthBar.sizeDelta = new Vector2(EnemyHudModel.GetBarWidth(HealthBarWidth, enemy.Health, enemy.MaxHealth), 7f);
                plate.Health.color = ratio <= 0.25f ? UiTheme.Danger : enemy.Boss == BossKind.None ? UiTheme.Mint : UiTheme.Warning;
                plate.Background.color = enemy.Boss == BossKind.None
                    ? new Color(0.008f, 0.035f, 0.052f, 0.90f)
                    : new Color(0.075f, 0.035f, 0.020f, 0.92f);
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
            Image background = rootObject.GetComponent<Image>();
            // Enemy information needs contrast, not another ornamental brass frame.
            // One quiet translucent backing keeps the name and HP legible over white foam.
            background.sprite = null;
            background.color = new Color(0.008f, 0.035f, 0.052f, 0.90f);
            background.raycastTarget = false;

            Text identity = CreateText(root, "Enemy Identity", new Vector2(-38f, 10f), new Vector2(126f, 21f), 13, TextAnchor.MiddleLeft);
            Text healthValue = CreateText(root, "Enemy Health Value", new Vector2(63f, 10f), new Vector2(78f, 21f), 11, TextAnchor.MiddleRight);
            healthValue.color = UiTheme.SecondaryText;
            RectTransform trackRect = CreateRect("Enemy Health Track", root, new Vector2(0f, -14f), new Vector2(194f, 11f));
            Image track = trackRect.gameObject.AddComponent<Image>();
            track.sprite = null;
            track.color = new Color(0f, 0.012f, 0.018f, 0.96f);
            track.raycastTarget = false;
            RectTransform fillRect = CreateRect("Enemy Health Fill", trackRect, Vector2.zero, new Vector2(HealthBarWidth, 7f));
            fillRect.anchorMin = fillRect.anchorMax = new Vector2(0f, 0.5f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = new Vector2(3f, 0f);
            Image fill = fillRect.gameObject.AddComponent<Image>();
            fill.sprite = null;
            fill.type = Image.Type.Simple;
            fillRect.sizeDelta = new Vector2(EnemyHudModel.GetBarWidth(HealthBarWidth, enemy.Health, enemy.MaxHealth), 7f);
            fill.color = UiTheme.Mint;
            fill.raycastTarget = false;

            return new Plate { Root = rootObject, Background = background, Identity = identity, HealthValue = healthValue, HealthBar = fillRect, Health = fill };
        }

        private Text CreateText(Transform parent, string name, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment)
        {
            RectTransform rect = CreateRect(name, parent, position, size);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
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
