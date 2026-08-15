using System;
using UnityEngine;
using UnityEngine.UI;

namespace Desktopirates
{
    public sealed class InventoryController : MonoBehaviour
    {
        private static readonly Color Brass = new Color(0.92f, 0.64f, 0.22f, 1f);
        private static readonly Color Mint = new Color(0.23f, 0.88f, 0.78f, 1f);
        private GameState state;
        private GameObject root;
        private GameObject launcherButton;
        private GameObject launcherHint;
        private Text totalText;
        private readonly Text[] counts = new Text[SalvageInventory.PartKindCount];
        private readonly Image[] slotFrames = new Image[SalvageInventory.PartKindCount];
        private SalvagePartKind selected;
        private Font font;
        private bool launcherVisible;

        public bool IsOpen => root != null && root.activeSelf;

        public void Initialize(RectTransform canvas, GameState gameState)
        {
            state = gameState;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildBagButton(canvas);
            BuildInventory(canvas);
            root.SetActive(false);
            if (Array.Exists(Environment.GetCommandLineArgs(), argument => argument == "--inventory-preview")) Open();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab) && (launcherVisible || IsOpen)) Toggle();
            if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) Close();
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public void Open()
        {
            Refresh();
            root.SetActive(true);
        }

        public void Close()
        {
            if (root != null) root.SetActive(false);
        }

        public void SetLauncherVisible(bool visible)
        {
            launcherVisible = visible;
            if (launcherButton != null) launcherButton.SetActive(visible);
            if (launcherHint != null) launcherHint.SetActive(visible);
        }

        public void Refresh()
        {
            if (state == null || totalText == null) return;
            totalText.text = $"CARGO  {state.TotalSalvageCount}";
            for (int i = 0; i < counts.Length; i++)
                counts[i].text = state.GetPartCount((SalvagePartKind)i).ToString();
            Select(selected);
        }

        private void BuildBagButton(RectTransform canvas)
        {
            RectTransform buttonRect = CreateRect("Cargo Bag Button", canvas, new Vector2(302f, -696f), new Vector2(54f, 54f));
            launcherButton = buttonRect.gameObject;
            RawImage image = buttonRect.gameObject.AddComponent<RawImage>();
            image.texture = UiTextureFactory.LoadMenuButton(MenuGlyph.Inventory, 80);
            Button button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(Toggle);

            RectTransform hintPill = CreateRect("Cargo Tab Hint Pill", canvas, new Vector2(302f, -731f), new Vector2(44f, 18f));
            launcherHint = hintPill.gameObject;
            Image hintBackground = hintPill.gameObject.AddComponent<Image>();
            hintBackground.sprite = UiTextureFactory.LoadPillSprite();
            hintBackground.type = Image.Type.Sliced;
            hintBackground.raycastTarget = false;
            Text hint = CreateText("Cargo Tab Hint", hintPill, "TAB", 11, Vector2.zero, new Vector2(42f, 18f));
            hint.color = new Color(1f, 0.77f, 0.31f, 0.95f);
        }

        private void BuildInventory(RectTransform canvas)
        {
            RectTransform panel = CreateRect("Scrollable Cargo Manifest", canvas, new Vector2(0f, -398f), new Vector2(500f, 520f));
            root = panel.gameObject;
            Image background = root.AddComponent<Image>();
            background.sprite = UiTextureFactory.LoadConceptSprite("Chrome", "cargo_panel_frame", 34f);
            background.type = Image.Type.Sliced;
            background.color = new Color(1f, 1f, 1f, 0.985f);
            AddAuthoredPanelFill(panel, new Vector2(450f, 468f));
            AddPanelFrameOverlay(panel, new Vector2(500f, 520f));

            totalText = CreateText("Cargo Title", panel, "CARGO", 21, new Vector2(0f, 205f), new Vector2(270f, 30f));
            totalText.color = Brass;

            RectTransform viewport = CreateRect("Cargo Scroll Viewport", panel, new Vector2(0f, -18f), new Vector2(420f, 384f));
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            // The mask must be fully rectangular. Decorative sprites have transparent
            // corners and would clip the first/last manifest row.
            viewportImage.sprite = null;
            viewportImage.color = new Color(0.015f, 0.045f, 0.070f, 0.96f);
            Mask mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            RectTransform content = CreateRect("Cargo Scroll Content", viewport, Vector2.zero,
                new Vector2(400f, SalvageInventory.PartKindCount * 76f + 12f));
            content.anchorMin = new Vector2(0.5f, 1f);
            content.anchorMax = new Vector2(0.5f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = new Vector2(0f, -6f);

            for (int i = 0; i < SalvageInventory.PartKindCount; i++)
            {
                SalvagePartKind kind = (SalvagePartKind)i;
                BuildRow(content, kind, new Vector2(0f, -38f - i * 76f));
            }

            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 32f;
            scroll.inertia = true;
            scroll.decelerationRate = 0.12f;

            Text scrollHint = CreateText("Cargo Scroll Hint", panel, "SCROLL  /  DRAG", 12, new Vector2(0f, -218f), new Vector2(200f, 22f));
            scrollHint.color = new Color(0.72f, 0.76f, 0.70f, 0.9f);

            RectTransform backRect = CreateRect("Close Cargo", panel, new Vector2(-205f, -220f), new Vector2(58f, 58f));
            RawImage backImage = backRect.gameObject.AddComponent<RawImage>();
            backImage.texture = UiTextureFactory.LoadMenuButton(MenuGlyph.Back, 80);
            Button back = backRect.gameObject.AddComponent<Button>();
            back.targetGraphic = backImage;
            back.onClick.AddListener(Close);
            Select(SalvagePartKind.Timber);
        }

        private void BuildRow(RectTransform parent, SalvagePartKind kind, Vector2 position)
        {
            int index = (int)kind;
            RectTransform slot = CreateRect($"{kind} Cargo Row", parent, position, new Vector2(386f, 68f));
            slot.anchorMin = new Vector2(0.5f, 1f);
            slot.anchorMax = new Vector2(0.5f, 1f);
            slot.pivot = new Vector2(0.5f, 0.5f);
            Image frame = slot.gameObject.AddComponent<Image>();
            frame.sprite = UiTextureFactory.LoadConceptSprite("Chrome", "service_button", 22f);
            frame.type = Image.Type.Sliced;
            frame.color = Color.white;
            slotFrames[index] = frame;
            Button button = slot.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;
            button.onClick.AddListener(() => Select(kind));

            RectTransform iconFrame = CreateRect($"{kind} Icon Frame", slot, new Vector2(-156f, 0f), new Vector2(56f, 56f));
            Image roundFrame = iconFrame.gameObject.AddComponent<Image>();
            roundFrame.sprite = UiTextureFactory.LoadConceptSprite("Chrome", "item_slot_frame");
            roundFrame.raycastTarget = false;
            RectTransform iconRect = CreateRect($"{kind} Icon", iconFrame, Vector2.zero, Vector2.one * UiLayoutMetrics.PrimaryIcon);
            RawImage icon = iconRect.gameObject.AddComponent<RawImage>();
            icon.texture = UiTextureFactory.LoadInventoryIcon(kind);
            icon.raycastTarget = false;

            // The authored service frame reserves its left notch for the round icon.
            // Keep every glyph to the right of that notch and the quantity inside
            // the brass end-cap so rows remain aligned at every window scale.
            Text name = CreateText($"{kind} Name", slot, SalvageInventory.GetDisplayName(kind), 16, new Vector2(15f, 13f), new Vector2(190f, 24f));
            name.alignment = TextAnchor.MiddleLeft;
            name.color = new Color(1f, 0.78f, 0.34f, 1f);
            Text description = CreateText($"{kind} Description", slot, SalvageInventory.GetDescription(kind), 12, new Vector2(15f, -14f), new Vector2(190f, 28f));
            description.alignment = TextAnchor.MiddleLeft;
            description.color = new Color(0.80f, 0.85f, 0.80f, 1f);
            description.resizeTextForBestFit = true;
            description.resizeTextMinSize = 10;
            description.resizeTextMaxSize = 12;
            counts[index] = CreateText($"{kind} Count", slot, "0", 20, new Vector2(140f, 0f), new Vector2(42f, 30f));
            counts[index].color = Color.white;
        }

        private void Select(SalvagePartKind kind)
        {
            selected = kind;
            for (int i = 0; i < slotFrames.Length; i++)
                if (slotFrames[i] != null) slotFrames[i].color = i == (int)kind ? Mint : Color.white;
        }

        private Text CreateText(string name, Transform parent, string value, int size, Vector2 position, Vector2 dimensions)
        {
            RectTransform rect = CreateRect(name, parent, position, dimensions);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = value;
            text.raycastTarget = false;
            if (size >= 13) UiTheme.StyleText(text, size);
            return text;
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)gameObject.transform;
            Vector2 anchor = parent.GetComponent<Canvas>() != null ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0.5f);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static void AddAuthoredPanelFill(Transform parent, Vector2 size)
        {
            RectTransform fill = CreateRect("Authored Cargo Fill", parent, Vector2.zero, size);
            RawImage image = fill.gameObject.AddComponent<RawImage>();
            image.texture = UiTextureFactory.LoadConceptTexture("Chrome", "panel_fill");
            image.raycastTarget = false;
            fill.SetAsFirstSibling();
        }

        private static void AddPanelFrameOverlay(Transform parent, Vector2 size)
        {
            RectTransform frame = CreateRect("Authored Cargo Frame Overlay", parent, Vector2.zero, size);
            RawImage image = frame.gameObject.AddComponent<RawImage>();
            image.texture = UiTextureFactory.LoadConceptTexture("Chrome", "cargo_panel_frame");
            image.raycastTarget = false;
        }
    }
}
