using System;
using UnityEngine;
using UnityEngine.UI;

namespace Desktopirates
{
    public sealed class InventoryController : MonoBehaviour
    {
        private static readonly Color Brass = new Color(0.92f, 0.64f, 0.22f, 1f);
        private static readonly Color Mint = new Color(0.23f, 0.88f, 0.78f, 1f);
        private static readonly Color RowNormal = new Color(0.025f, 0.080f, 0.105f, 1f);
        private static readonly Color RowAlternate = new Color(0.030f, 0.095f, 0.120f, 1f);
        private static readonly Color RowSelected = new Color(0.035f, 0.155f, 0.165f, 1f);
        private GameState state;
        private GameObject root;
        private GameObject launcherButton;
        private GameObject launcherHint;
        private Text totalText;
        private readonly Text[] counts = new Text[SalvageInventory.PartKindCount];
        private readonly Image[] slotFrames = new Image[SalvageInventory.PartKindCount];
        private readonly GameObject[] selectionMarkers = new GameObject[SalvageInventory.PartKindCount];
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
            background.sprite = null;
            background.color = Color.clear;
            AddAuthoredPanelFill(panel, new Vector2(450f, 468f));
            AddPanelFrameOverlay(panel, new Vector2(500f, 520f));

            totalText = CreateText("Cargo Title", panel, "CARGO", 21, new Vector2(0f, 205f), new Vector2(270f, 30f));
            totalText.color = Brass;

            RectTransform viewport = CreateRect("Cargo Scroll Viewport", panel, new Vector2(0f, -18f), new Vector2(420f, 384f));
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            // The mask must be fully rectangular. Decorative sprites have transparent
            // corners and would clip the first/last manifest row.
            viewportImage.sprite = null;
            viewportImage.color = new Color(0.015f, 0.045f, 0.070f, 1f);
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
            RectTransform scrollbarRect = CreateRect("Cargo Scrollbar", panel, new Vector2(214f, -18f), new Vector2(4f, 384f));
            Image scrollbarTrack = scrollbarRect.gameObject.AddComponent<Image>(); scrollbarTrack.color = new Color(0.04f, 0.13f, 0.16f, 1f); scrollbarTrack.raycastTarget = true;
            Scrollbar scrollbar = scrollbarRect.gameObject.AddComponent<Scrollbar>();
            RectTransform slidingArea = CreateRect("Cargo Scrollbar Sliding Area", scrollbarRect, Vector2.zero, Vector2.zero);
            slidingArea.anchorMin = Vector2.zero; slidingArea.anchorMax = Vector2.one; slidingArea.offsetMin = new Vector2(1f, 2f); slidingArea.offsetMax = new Vector2(-1f, -2f);
            RectTransform handle = CreateRect("Cargo Scrollbar Handle", slidingArea, Vector2.zero, Vector2.zero);
            handle.anchorMin = Vector2.zero; handle.anchorMax = Vector2.one; handle.offsetMin = Vector2.zero; handle.offsetMax = Vector2.zero;
            Image handleImage = handle.gameObject.AddComponent<Image>(); handleImage.color = new Color(0.18f, 0.50f, 0.50f, 1f);
            scrollbar.handleRect = handle; scrollbar.targetGraphic = handleImage; scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = scrollbar; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

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
            frame.sprite = null;
            frame.color = (index & 1) == 0 ? RowNormal : RowAlternate;
            slotFrames[index] = frame;
            Button button = slot.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.10f, 1.10f, 1.10f, 1f);
            colors.pressedColor = new Color(0.78f, 0.90f, 0.90f, 1f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.onClick.AddListener(() => Select(kind));

            RectTransform selectedBar = CreateRect($"{kind} Selection Marker", slot, new Vector2(-191f, 0f), new Vector2(4f, 56f));
            Image selectedBarImage = selectedBar.gameObject.AddComponent<Image>(); selectedBarImage.color = Mint; selectedBarImage.raycastTarget = false;
            selectionMarkers[index] = selectedBar.gameObject;
            RectTransform divider = CreateRect($"{kind} Row Divider", slot, new Vector2(0f, -33.5f), new Vector2(366f, 1f));
            Image dividerImage = divider.gameObject.AddComponent<Image>(); dividerImage.color = new Color(0.28f, 0.58f, 0.61f, 0.34f); dividerImage.raycastTarget = false;

            RectTransform iconRect = CreateRect($"{kind} Icon", slot, new Vector2(-158f, 0f), Vector2.one * 44f);
            RawImage icon = iconRect.gameObject.AddComponent<RawImage>();
            icon.texture = UiTextureFactory.LoadInventoryIcon(kind);
            icon.raycastTarget = false;

            Text name = CreateText($"{kind} Name", slot, SalvageInventory.GetDisplayName(kind), 16, new Vector2(4f, 13f), new Vector2(214f, 24f));
            name.alignment = TextAnchor.MiddleLeft;
            name.color = new Color(0.94f, 0.98f, 0.96f, 1f);
            Text description = CreateText($"{kind} Description", slot, SalvageInventory.GetDescription(kind), 12, new Vector2(4f, -14f), new Vector2(214f, 28f));
            description.alignment = TextAnchor.MiddleLeft;
            description.color = new Color(0.80f, 0.85f, 0.80f, 1f);
            description.resizeTextForBestFit = true;
            description.resizeTextMinSize = 10;
            description.resizeTextMaxSize = 12;
            counts[index] = CreateText($"{kind} Count", slot, "0", 20, new Vector2(158f, 0f), new Vector2(48f, 30f));
            counts[index].color = Mint;
        }

        private void Select(SalvagePartKind kind)
        {
            selected = kind;
            for (int i = 0; i < slotFrames.Length; i++)
            {
                bool isSelected = i == (int)kind;
                if (slotFrames[i] != null) slotFrames[i].color = isSelected ? RowSelected : (i & 1) == 0 ? RowNormal : RowAlternate;
                if (selectionMarkers[i] != null) selectionMarkers[i].SetActive(isSelected);
            }
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
