using System;
using UnityEngine;
using UnityEngine.UI;

namespace Desktopirates
{
    public sealed class InventoryController : MonoBehaviour
    {
        private static readonly Color Brass = new Color(0.92f, 0.64f, 0.22f, 1f);
        private static readonly Color RowNormal = new Color(0.025f, 0.080f, 0.105f, 1f);
        private static readonly Color RowAlternate = new Color(0.030f, 0.095f, 0.120f, 1f);
        private static readonly Color RowSelected = new Color(0.035f, 0.155f, 0.165f, 1f);
        private GameState state;
        private GameObject root;
        private GameObject launcherButton;
        private GameObject launcherHint;
        private Text totalText;
        private readonly Text[] counts = new Text[InventoryManifestModel.EntryCount];
        private readonly Image[] slotFrames = new Image[InventoryManifestModel.EntryCount];
        private readonly Image[] rarityStrips = new Image[InventoryManifestModel.EntryCount];
        private InventoryItemKind selected;
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
            totalText.text = $"{InventoryManifestModel.EntryCount} TYPES   •   {InventoryManifestModel.GetTotalCount(state)} ITEMS";
            for (int i = 0; i < counts.Length; i++)
                counts[i].text = $"x{InventoryManifestModel.GetCount(state, (InventoryItemKind)i)}";
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
            // 720x760 reference canvas: the panel leaves 12 px at the right,
            // 10 px at the bottom, and 8 px between the title and menu circle.
            RectTransform panel = CreateRect("Concept Inventory Manifest", canvas,
                new Vector2(InventoryManifestModel.PanelCenterX, -InventoryManifestModel.PanelTopOffset),
                new Vector2(InventoryManifestModel.PanelWidth, InventoryManifestModel.PanelHeight));
            root = panel.gameObject;
            Image background = root.AddComponent<Image>();
            background.sprite = null;
            background.color = Color.clear;
            AddAuthoredPanelFill(panel, new Vector2(
                InventoryManifestModel.PanelWidth - InventoryManifestModel.PanelFillInset * 2f,
                InventoryManifestModel.PanelHeight - InventoryManifestModel.PanelFillInset * 2f));
            AddPanelFrameOverlay(panel, new Vector2(516f, 646f));

            RectTransform titlePlate = CreateRect("Inventory Title Cartouche", panel,
                new Vector2(0f, InventoryManifestModel.TitleCenterY), new Vector2(320f, InventoryManifestModel.TitleHeight));
            RawImage titlePlateImage = titlePlate.gameObject.AddComponent<RawImage>();
            titlePlateImage.texture = UiTextureFactory.LoadInventoryChrome("title");
            titlePlateImage.color = Color.white;
            titlePlateImage.raycastTarget = false;
            Text title = CreateText("Inventory Title", titlePlate, "INVENTORY", 24, Vector2.zero, new Vector2(288f, 48f));
            title.color = new Color(1f, 0.86f, 0.57f, 1f);
            totalText = CreateText("Inventory Summary", panel, "10 TYPES", 12,
                new Vector2(58f, InventoryManifestModel.SummaryCenterY), new Vector2(300f, 22f));
            totalText.alignment = TextAnchor.MiddleRight;
            totalText.color = new Color(0.67f, 0.75f, 0.75f, 1f);

            RectTransform backRect = CreateRect("Close Inventory", panel,
                new Vector2(-211f, InventoryManifestModel.TitleCenterY), new Vector2(76f, 28f));
            RawImage backImage = backRect.gameObject.AddComponent<RawImage>();
            backImage.texture = UiTextureFactory.LoadInventoryChrome("title");
            backImage.color = new Color(0.78f, 0.78f, 0.78f, 1f);
            Button back = backRect.gameObject.AddComponent<Button>();
            back.targetGraphic = backImage;
            back.onClick.AddListener(Close);
            Text backLabel = CreateText("Close Inventory Label", backRect, "BACK", 11, Vector2.zero, new Vector2(66f, 22f));
            backLabel.color = Brass;

            RectTransform viewport = CreateRect("Inventory Scroll Viewport", panel,
                new Vector2(-8f, InventoryManifestModel.ViewportCenterY), new Vector2(452f, InventoryManifestModel.ViewportHeight));
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            // The mask must be fully rectangular. Decorative sprites have transparent
            // corners and would clip the first/last manifest row.
            viewportImage.sprite = null;
            viewportImage.color = new Color(0.015f, 0.045f, 0.070f, 1f);
            Mask mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            RectTransform content = CreateRect("Inventory Scroll Content", viewport, Vector2.zero,
                new Vector2(432f, InventoryManifestModel.ContentHeight));
            content.anchorMin = new Vector2(0.5f, 1f);
            content.anchorMax = new Vector2(0.5f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = new Vector2(0f, -6f);

            for (int i = 0; i < InventoryManifestModel.EntryCount; i++)
            {
                InventoryItemKind kind = (InventoryItemKind)i;
                BuildRow(content, kind, new Vector2(0f, -33f - i * InventoryManifestModel.RowStride));
            }

            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 38f;
            scroll.inertia = true;
            scroll.decelerationRate = 0.12f;
            RectTransform scrollbarRect = CreateRect("Inventory Brass Scrollbar", panel,
                new Vector2(232f, InventoryManifestModel.ViewportCenterY), new Vector2(10f, InventoryManifestModel.ViewportHeight));
            RawImage scrollbarTrack = scrollbarRect.gameObject.AddComponent<RawImage>(); scrollbarTrack.texture = UiTextureFactory.LoadInventoryChrome("scrollbar_track"); scrollbarTrack.color = Color.white; scrollbarTrack.raycastTarget = true;
            Scrollbar scrollbar = scrollbarRect.gameObject.AddComponent<Scrollbar>();
            RectTransform slidingArea = CreateRect("Cargo Scrollbar Sliding Area", scrollbarRect, Vector2.zero, Vector2.zero);
            slidingArea.anchorMin = Vector2.zero; slidingArea.anchorMax = Vector2.one; slidingArea.offsetMin = new Vector2(1f, 2f); slidingArea.offsetMax = new Vector2(-1f, -2f);
            RectTransform handle = CreateRect("Cargo Scrollbar Handle", slidingArea, Vector2.zero, Vector2.zero);
            handle.anchorMin = Vector2.zero; handle.anchorMax = Vector2.one; handle.offsetMin = Vector2.zero; handle.offsetMax = Vector2.zero;
            RawImage handleImage = handle.gameObject.AddComponent<RawImage>(); handleImage.texture = UiTextureFactory.LoadInventoryChrome("scrollbar_handle"); handleImage.color = Color.white;
            scrollbar.handleRect = handle; scrollbar.targetGraphic = handleImage; scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = scrollbar; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            Select(InventoryItemKind.Food);
        }

        private void BuildRow(RectTransform parent, InventoryItemKind kind, Vector2 position)
        {
            int index = (int)kind;
            InventoryRarity rarity = InventoryManifestModel.GetRarity(kind);
            Color rarityColor = InventoryManifestModel.GetRarityColor(rarity);
            RectTransform slot = CreateRect($"{kind} Inventory Row", parent, position, new Vector2(428f, InventoryManifestModel.RowHeight));
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

            RectTransform rarityBar = CreateRect($"{kind} Rarity Strip", slot, new Vector2(-211f, 0f), new Vector2(4f, 56f));
            Image rarityBarImage = rarityBar.gameObject.AddComponent<Image>(); rarityBarImage.color = rarityColor; rarityBarImage.raycastTarget = false;
            rarityStrips[index] = rarityBarImage;
            RectTransform divider = CreateRect($"{kind} Row Divider", slot, new Vector2(0f, -30.5f), new Vector2(414f, 1f));
            Image dividerImage = divider.gameObject.AddComponent<Image>(); dividerImage.color = new Color(0.24f, 0.38f, 0.42f, 0.65f); dividerImage.raycastTarget = false;

            RectTransform iconRect = CreateRect($"{kind} Frameless Icon", slot, new Vector2(InventoryManifestModel.IconCenterX, 0f), Vector2.one * InventoryManifestModel.IconSize);
            RawImage icon = iconRect.gameObject.AddComponent<RawImage>();
            icon.texture = UiTextureFactory.LoadInventoryIcon(kind);
            icon.raycastTarget = false;

            // Text starts 11 px after the 54 px icon and ends 12 px before the
            // quantity column, so long descriptions never overlap either one.
            Text name = CreateText($"{kind} Name", slot, InventoryManifestModel.GetName(kind), 16,
                new Vector2(InventoryManifestModel.TextCenterX, 17f), new Vector2(InventoryManifestModel.TextWidth, 22f));
            name.alignment = TextAnchor.MiddleLeft;
            name.color = new Color(0.94f, 0.98f, 0.96f, 1f);
            Text rarityLabel = CreateText($"{kind} Rarity", slot, InventoryManifestModel.GetRarityName(rarity), 10,
                new Vector2(InventoryManifestModel.TextCenterX, 0f), new Vector2(InventoryManifestModel.TextWidth, 16f));
            rarityLabel.alignment = TextAnchor.MiddleLeft;
            rarityLabel.color = rarityColor;
            Text description = CreateText($"{kind} Description", slot, InventoryManifestModel.GetDescription(kind), 11,
                new Vector2(InventoryManifestModel.TextCenterX, -18f), new Vector2(InventoryManifestModel.TextWidth, 18f));
            description.alignment = TextAnchor.MiddleLeft;
            description.color = new Color(0.80f, 0.85f, 0.80f, 1f);
            description.resizeTextForBestFit = true;
            description.resizeTextMinSize = 9;
            description.resizeTextMaxSize = 11;
            counts[index] = CreateText($"{kind} Count", slot, "x0", 17,
                new Vector2(InventoryManifestModel.CountCenterX, 15f), new Vector2(InventoryManifestModel.CountWidth, 24f));
            counts[index].alignment = TextAnchor.MiddleRight;
            counts[index].color = new Color(0.94f, 0.98f, 0.96f, 1f);
        }

        private void Select(InventoryItemKind kind)
        {
            selected = kind;
            for (int i = 0; i < slotFrames.Length; i++)
            {
                bool isSelected = i == (int)kind;
                if (slotFrames[i] != null) slotFrames[i].color = isSelected ? RowSelected : (i & 1) == 0 ? RowNormal : RowAlternate;
                if (rarityStrips[i] != null) rarityStrips[i].color = InventoryManifestModel.GetRarityColor(InventoryManifestModel.GetRarity((InventoryItemKind)i));
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
            image.texture = UiTextureFactory.LoadInventoryChrome("frame");
            image.raycastTarget = false;
        }
    }
}
