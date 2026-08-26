using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Desktopirates
{
    /// <summary>Spatial combat numbers plus a Windows-like lower-right voyage feed.</summary>
    public sealed class NotificationController : MonoBehaviour
    {
        private sealed class FeedItem
        {
            public RectTransform Rect;
            public CanvasGroup Group;
            public float CreatedAt;
        }

        private sealed class DamageItem
        {
            public RectTransform Rect;
            public CanvasGroup Group;
            public Vector2 Origin;
            public float CreatedAt;
        }

        private const int MaxFeedItems = 7;
        private readonly List<FeedItem> feed = new List<FeedItem>();
        private readonly List<DamageItem> damageItems = new List<DamageItem>();
        private RectTransform canvas;
        private RectTransform feedLayer;
        private RectTransform damageLayer;
        private Camera worldCamera;
        private MenuController menu;
        private Font font;

        public void Initialize(RectTransform canvasRoot, Camera camera, PoiSystem pois, MenuController menuController)
        {
            canvas = canvasRoot;
            worldCamera = camera;
            menu = menuController;
            font = GameLocalization.IsJapanese
                ? Font.CreateDynamicFontFromOSFont(new[] { "Yu Gothic UI", "Meiryo UI", "Meiryo", "Arial" }, 15)
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            feedLayer = CreateLayer("Voyage Notification Feed");
            damageLayer = CreateLayer("Spatial Damage Numbers");

            if (pois != null)
            {
                pois.EnemyDamaged += ShowEnemyDamage;
                pois.PlayerDamaged += ShowPlayerDamage;
                pois.LootCollected += ShowLoot;
            }
            if (menu != null) menu.Message += ShowSystem;
            if (Array.Exists(Environment.GetCommandLineArgs(), argument => argument == "--notifications-preview"))
                StartCoroutine(PreviewRoutine());
        }

        private IEnumerator PreviewRoutine()
        {
            yield return new WaitForSecondsRealtime(3.15f);
            ShowLoot(new LootNotice(PoiKind.Wreck, 46, 2, new[]
            {
                new SalvageDrop(SalvagePartKind.Timber, 3),
                new SalvageDrop(SalvagePartKind.Gear, 1)
            }));
            yield return new WaitForSecondsRealtime(1.25f);
            ShowPlayerDamage(new PlayerDamageNotice(128, ShipStatus.Burning));
            ShowEnemyDamage(new EnemyDamageNotice(new Vector3(1.25f, 0.62f, 0.55f), 384, false, "BURNING"));
        }

        private RectTransform CreateLayer(string name)
        {
            var item = new GameObject(name, typeof(RectTransform));
            item.transform.SetParent(canvas, false);
            RectTransform rect = (RectTransform)item.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            for (int index = feed.Count - 1; index >= 0; index--)
            {
                FeedItem item = feed[index];
                float age = now - item.CreatedAt;
                if (age >= NotificationUiModel.FeedLifetime)
                {
                    Destroy(item.Rect.gameObject);
                    feed.RemoveAt(index);
                    continue;
                }
                item.Group.alpha = NotificationUiModel.GetFeedAlpha(age);
            }

            for (int index = 0; index < feed.Count; index++)
            {
                FeedItem item = feed[index];
                int fromNewest = feed.Count - 1 - index;
                float targetY = NotificationUiModel.FeedBaseY + NotificationUiModel.GetStackY(fromNewest);
                Vector2 target = new Vector2(-14f, targetY);
                item.Rect.anchoredPosition = Vector2.Lerp(item.Rect.anchoredPosition, target,
                    1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
            }

            for (int index = damageItems.Count - 1; index >= 0; index--)
            {
                DamageItem item = damageItems[index];
                float age = now - item.CreatedAt;
                if (age >= NotificationUiModel.DamageLifetime)
                {
                    Destroy(item.Rect.gameObject);
                    damageItems.RemoveAt(index);
                    continue;
                }
                float progress = Mathf.Clamp01(age / NotificationUiModel.DamageLifetime);
                item.Rect.anchoredPosition = item.Origin + Vector2.up * Mathf.Lerp(0f, 30f, progress);
                item.Group.alpha = NotificationUiModel.GetDamageAlpha(age);
            }
        }

        private void ShowEnemyDamage(EnemyDamageNotice notice)
        {
            if (worldCamera == null) return;
            Vector3 screen = worldCamera.WorldToScreenPoint(notice.WorldPosition);
            if (screen.z <= 0f) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, screen, null, out Vector2 local);
            string suffix = notice.Sinking
                ? GameLocalization.Choose("  SINKING!", "  撃沈!")
                : string.IsNullOrEmpty(notice.Statuses) ? string.Empty : $"  {notice.Statuses}";
            CreateDamagePopup(local, $"−{notice.Damage}{suffix}", notice.Sinking ? UiTheme.Warning : new Color(1f, 0.77f, 0.32f));
        }

        private void ShowPlayerDamage(PlayerDamageNotice notice)
        {
            Vector2 local = new Vector2(-267f, -212f);
            if (menu != null && menu.HullStatusCard != null)
            {
                // Both rects live on the same overlay canvas. Converting through screen space
                // made the popup drift outside the left edge on scaled/transparent windows.
                Vector3 cardCenter = canvas.InverseTransformPoint(menu.HullStatusCard.TransformPoint(Vector3.zero));
                local = new Vector2(cardCenter.x, cardCenter.y - 58f);
            }
            string status = notice.Status == ShipStatus.None ? string.Empty : $"  {ShipStatusRuntime.GetName(notice.Status)}";
            CreateDamagePopup(local, $"HULL −{notice.Damage}{status}", UiTheme.Danger);
        }

        private void CreateDamagePopup(Vector2 local, string value, Color color)
        {
            const float popupWidth = 230f;
            const float popupHeight = 38f;
            if (canvas != null)
            {
                local.x = Mathf.Clamp(local.x, canvas.rect.xMin + popupWidth * 0.5f + 8f,
                    canvas.rect.xMax - popupWidth * 0.5f - 8f);
                local.y = Mathf.Clamp(local.y, canvas.rect.yMin + popupHeight * 0.5f + 8f,
                    canvas.rect.yMax - popupHeight * 0.5f - 8f);
            }
            var rootObject = new GameObject("Damage Popup", typeof(RectTransform), typeof(CanvasGroup), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
            rootObject.transform.SetParent(damageLayer, false);
            RectTransform rect = (RectTransform)rootObject.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = local;
            rect.sizeDelta = new Vector2(popupWidth, popupHeight);
            Text text = rootObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = 18;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 12;
            text.resizeTextMaxSize = 18;
            text.raycastTarget = false;
            text.text = value;
            Outline outline = rootObject.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0.02f, 0.03f, 0.95f);
            outline.effectDistance = new Vector2(1f, -1f);
            damageItems.Add(new DamageItem
            {
                Rect = rect,
                Group = rootObject.GetComponent<CanvasGroup>(),
                Origin = local,
                CreatedAt = Time.unscaledTime
            });
        }

        private void ShowLoot(LootNotice notice)
        {
            Texture2D sourceIcon = UiTextureFactory.LoadPoiBadge(notice.Source);
            string title = notice.Source == PoiKind.Wreck
                ? GameLocalization.Choose($"WRECK  +{notice.Gold}G", $"残骸回収  +{notice.Gold}G")
                : GameLocalization.Choose($"TREASURE  +{notice.Gold}G", $"宝物発見  +{notice.Gold}G");
            AddFeed(sourceIcon, title, UiTheme.Brass);

            if (notice.Supplies > 0)
                AddFeed(UiTextureFactory.LoadInventoryIcon(InventoryItemKind.Supplies),
                    GameLocalization.Choose($"SUPPLIES  +{notice.Supplies}", $"航海物資  +{notice.Supplies}"), UiTheme.Mint);

            foreach (SalvageDrop drop in notice.Drops)
            {
                string name = GameLocalization.IsJapanese
                    ? InventoryManifestModel.GetLocalizedName((InventoryItemKind)((int)InventoryItemKind.Timber + (int)drop.Kind))
                    : SalvageInventory.GetDisplayName(drop.Kind);
                AddFeed(UiTextureFactory.LoadInventoryIcon(drop.Kind), $"{name}  +{drop.Amount}", UiTheme.PrimaryText);
            }
        }

        private void ShowSystem(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            AddFeed(UiTextureFactory.LoadFramelessMenuGlyph(MenuGlyph.Log), message, UiTheme.SecondaryText);
        }

        private void AddFeed(Texture2D iconTexture, string value, Color accentColor)
        {
            while (feed.Count >= MaxFeedItems)
            {
                Destroy(feed[0].Rect.gameObject);
                feed.RemoveAt(0);
            }

            var rootObject = new GameObject("Voyage Feed Item", typeof(RectTransform), typeof(CanvasGroup), typeof(CanvasRenderer), typeof(Image));
            rootObject.transform.SetParent(feedLayer, false);
            RectTransform root = (RectTransform)rootObject.transform;
            root.anchorMin = root.anchorMax = new Vector2(1f, 0f);
            root.pivot = new Vector2(1f, 0.5f);
            root.anchoredPosition = new Vector2(-14f, NotificationUiModel.FeedBaseY);
            root.sizeDelta = new Vector2(304f, 42f);
            Image background = rootObject.GetComponent<Image>();
            background.sprite = null;
            background.color = new Color(0.008f, 0.032f, 0.048f, 0.94f);
            background.raycastTarget = false;

            RectTransform accent = CreateRect("Notice Accent", root, new Vector2(-149f, 0f), new Vector2(4f, 42f));
            Image accentImage = accent.gameObject.AddComponent<Image>();
            accentImage.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.94f);
            accentImage.raycastTarget = false;

            RectTransform iconRect = CreateRect("Notice Icon", root, new Vector2(-124f, 0f), new Vector2(31f, 31f));
            RawImage icon = iconRect.gameObject.AddComponent<RawImage>();
            icon.texture = iconTexture;
            icon.color = Color.white;
            icon.raycastTarget = false;

            RectTransform textRect = CreateRect("Notice Text", root, new Vector2(18f, 0f), new Vector2(246f, 34f));
            Text text = textRect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = 14;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = accentColor;
            text.text = value;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 10;
            text.resizeTextMaxSize = 14;
            text.raycastTarget = false;

            feed.Add(new FeedItem
            {
                Rect = root,
                Group = rootObject.GetComponent<CanvasGroup>(),
                CreatedAt = Time.unscaledTime
            });
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
