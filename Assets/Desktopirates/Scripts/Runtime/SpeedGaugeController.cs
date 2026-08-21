using UnityEngine;
using UnityEngine.UI;

namespace Desktopirates
{
    public sealed class SpeedGaugeController : MonoBehaviour
    {
        private static readonly Color Locked = new Color(0.18f, 0.23f, 0.24f, 0.72f);
        private static readonly Color Available = new Color(0.55f, 0.67f, 0.66f, 0.88f);
        private static readonly Color Active = Color.white;
        private BoatController boat;
        private InventoryController inventory;
        private MenuController menu;
        private GameObject gaugeRoot;
        private readonly RawImage[] segments = new RawImage[SpeedGaugeModel.SegmentCount];
        private Image motionBar;
        private Text stateLabel;
        private Text stepLabel;
        private Texture2D activeTexture;
        private Texture2D inactiveTexture;
        private int lastStep = -1;
        private int lastMax = -1;

        public void Initialize(RectTransform canvas, BoatController player, InventoryController cargoInventory, MenuController menuController = null)
        {
            boat = player;
            inventory = cargoInventory;
            menu = menuController;
            activeTexture = UiTextureFactory.LoadSpeedSegment(true);
            inactiveTexture = UiTextureFactory.LoadSpeedSegment(false);
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            RectTransform root = CreateRect("Compact Engine Telegraph", canvas);
            gaugeRoot = root.gameObject;
            root.anchoredPosition = new Vector2(0f, -724f);
            root.sizeDelta = new Vector2(420f, 96f);
            RawImage background = root.gameObject.AddComponent<RawImage>();
            background.texture = UiTextureFactory.LoadGeneratedTelegraph();
            background.color = Color.white;
            background.raycastTarget = false;

            for (int i = 0; i < segments.Length; i++)
            {
                RectTransform rect = CreateRect($"Telegraph Segment {i + 1}", root);
                rect.anchoredPosition = new Vector2(-132f + i * 37.5f, 13f);
                rect.sizeDelta = new Vector2(26f, 26f);
                segments[i] = rect.gameObject.AddComponent<RawImage>();
                segments[i].raycastTarget = false;
            }

            RectTransform motionTrack = CreateRect("Actual Speed Track", root);
            motionTrack.anchoredPosition = new Vector2(0f, -25f);
            motionTrack.sizeDelta = new Vector2(280f, 6f);
            Image trackImage = motionTrack.gameObject.AddComponent<Image>();
            trackImage.color = new Color(0.025f, 0.09f, 0.11f, 1f); trackImage.raycastTarget = false;
            RectTransform motionFill = CreateRect("Actual Speed Fill", motionTrack);
            motionFill.anchorMin = Vector2.zero; motionFill.anchorMax = Vector2.one; motionFill.offsetMin = motionFill.offsetMax = Vector2.zero;
            motionBar = motionFill.gameObject.AddComponent<Image>(); motionBar.type = Image.Type.Filled; motionBar.fillMethod = Image.FillMethod.Horizontal;
            motionBar.color = UiTheme.Mint; motionBar.raycastTarget = false;

            stateLabel = CreateText("Speed State", root, font, 15, new Vector2(-204f, -18f), new Vector2(92f, 22f));
            stateLabel.color = UiTheme.Brass;
            UiTheme.StyleText(stateLabel, 16);
            stepLabel = CreateText("Speed Step Hint", root, font, 12, new Vector2(204f, -18f), new Vector2(92f, 22f));
            stepLabel.color = UiTheme.SecondaryText;
            UiTheme.StyleText(stepLabel, 13);
            Refresh(true);
        }

        private void Update()
        {
            if (gaugeRoot != null) gaugeRoot.SetActive((inventory == null || !inventory.IsOpen) && (menu == null || !menu.IsModalOpen));
            Refresh(false);
        }

        private void Refresh(bool force)
        {
            if (boat == null) return;
            int step = boat.CruiseStep;
            int max = boat.MaxCruiseStep;
            if (force || step != lastStep || max != lastMax)
            {
                lastStep = step;
                lastMax = max;
                for (int i = 0; i < segments.Length; i++)
                {
                    bool unlocked = i < max;
                    bool lit = i < step;
                    segments[i].texture = lit ? activeTexture : inactiveTexture;
                    segments[i].color = !unlocked ? Locked : lit ? Active : Available;
                }
            }

            // Lamps show the engine order; the lower bar follows actual speed and visibly lags while coasting.
            float normalized = SpeedGaugeModel.GetActualNeedle01(boat.Speed, boat.MaxSpeed);
            motionBar.fillAmount = normalized;
            motionBar.color = boat.IsCoasting ? UiTheme.Brass : UiTheme.Mint;
            stateLabel.text = SpeedGaugeModel.GetMotionLabel(step, max, boat.Speed, boat.TargetSpeed);
            if (boat.IsCoasting)
            {
                stepLabel.text = step == 0 ? $"DRIFT {boat.Speed:0.0}" : $"SLOW {step}/{max}";
            }
            else
            {
                stepLabel.text = step == 0 ? "W AHEAD" : $"W+ {step}/{max} S-";
            }
        }

        private static Text CreateText(string name, Transform parent, Font font, int size, Vector2 position, Vector2 dimensions)
        {
            RectTransform rect = CreateRect(name, parent);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)gameObject.transform;
            Vector2 anchor = parent.GetComponent<Canvas>() != null ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0.5f);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            return rect;
        }
    }
}
