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
        private Text detailName;
        private Text detailText;
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
            RectTransform buttonRect = CreateRect("Cargo Bag Button", canvas, new Vector2(296f, -505f), new Vector2(62f, 62f));
            launcherButton = buttonRect.gameObject;
            RawImage image = buttonRect.gameObject.AddComponent<RawImage>();
            image.texture = UiTextureFactory.LoadMenuButton(MenuGlyph.Inventory, 80);
            Button button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(Toggle);

            RectTransform hintPill = CreateRect("Cargo Tab Hint Pill", canvas, new Vector2(296f, -548f), new Vector2(46f, 20f));
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
            RectTransform panel = CreateRect("Circular Cargo Hold", canvas, new Vector2(0f, -370f), new Vector2(400f, 400f));
            root = panel.gameObject;
            Image background = root.AddComponent<Image>();
            background.sprite = UiTextureFactory.LoadPanelSprite(128);
            background.color = new Color(1f, 1f, 1f, 0.985f);
            UiTheme.AddChartWoodSurface(panel, new Vector2(350f, 350f), 0.52f, UiTextureFactory.LoadPanelSprite(128));

            totalText = CreateText("Cargo Title", panel, "CARGO", 20, new Vector2(0f, 176f), new Vector2(190f, 28f));
            totalText.color = Brass;

            for (int i = 0; i < SalvageInventory.PartKindCount; i++)
            {
                SalvagePartKind kind = (SalvagePartKind)i;
                float degrees = 90f - i * 60f;
                float radians = degrees * Mathf.Deg2Rad;
                Vector2 position = new Vector2(Mathf.Cos(radians) * 112f, Mathf.Sin(radians) * 112f + 2f);
                BuildSlot(panel, kind, position);
            }

            RectTransform center = CreateRect("Cargo Emblem", panel, Vector2.zero, new Vector2(86f, 86f));
            RawImage emblem = center.gameObject.AddComponent<RawImage>();
            emblem.texture = UiTextureFactory.LoadMenuButton(MenuGlyph.Inventory, 80);
            emblem.raycastTarget = false;

            detailName = CreateText("Selected Cargo Name", panel, "TIMBER", 17, new Vector2(0f, -142f), new Vector2(230f, 24f));
            detailName.color = Mint;
            detailText = CreateText("Selected Cargo Detail", panel, string.Empty, 15, new Vector2(0f, -166f), new Vector2(310f, 38f));
            detailText.color = new Color(0.82f, 0.87f, 0.82f, 1f);

            RectTransform backRect = CreateRect("Close Cargo", panel, new Vector2(-166f, -158f), new Vector2(54f, 54f));
            RawImage backImage = backRect.gameObject.AddComponent<RawImage>();
            backImage.texture = UiTextureFactory.LoadMenuButton(MenuGlyph.Back, 80);
            Button back = backRect.gameObject.AddComponent<Button>();
            back.targetGraphic = backImage;
            back.onClick.AddListener(Close);
            Select(SalvagePartKind.Timber);
        }

        private void BuildSlot(RectTransform parent, SalvagePartKind kind, Vector2 position)
        {
            int index = (int)kind;
            RectTransform slot = CreateRect($"{kind} Cargo Slot", parent, position, new Vector2(74f, 74f));
            Image frame = slot.gameObject.AddComponent<Image>();
            frame.sprite = UiTextureFactory.LoadPanelSprite(80);
            frame.color = Color.white;
            slotFrames[index] = frame;
            Button button = slot.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;
            button.onClick.AddListener(() => Select(kind));

            RectTransform iconRect = CreateRect($"{kind} Icon", slot, new Vector2(0f, 5f), new Vector2(42f, 42f));
            RawImage icon = iconRect.gameObject.AddComponent<RawImage>();
            icon.texture = UiTextureFactory.LoadInventoryIcon(kind);
            icon.raycastTarget = false;

            Text name = CreateText($"{kind} Name", slot, SalvageInventory.GetDisplayName(kind), 13, new Vector2(0f, -24f), new Vector2(72f, 17f));
            name.color = new Color(1f, 0.78f, 0.34f, 1f);
            counts[index] = CreateText($"{kind} Count", slot, "0", 15, new Vector2(23f, 22f), new Vector2(30f, 18f));
            counts[index].color = Color.white;
        }

        private void Select(SalvagePartKind kind)
        {
            selected = kind;
            for (int i = 0; i < slotFrames.Length; i++)
                if (slotFrames[i] != null) slotFrames[i].color = i == (int)kind ? Mint : Color.white;
            if (detailName == null) return;
            detailName.text = $"{SalvageInventory.GetDisplayName(kind)}  x{state.GetPartCount(kind)}";
            detailText.text = SalvageInventory.GetDescription(kind);
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
    }
}
