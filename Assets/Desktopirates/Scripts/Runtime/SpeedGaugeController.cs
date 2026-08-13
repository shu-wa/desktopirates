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
        private GameObject gaugeRoot;
        private readonly RawImage[] segments = new RawImage[SpeedGaugeModel.SegmentCount];
        private RectTransform needle;
        private Text stateLabel;
        private Text stepLabel;
        private Texture2D activeTexture;
        private Texture2D inactiveTexture;
        private int lastStep = -1;
        private int lastMax = -1;

        public void Initialize(RectTransform canvas, BoatController player, InventoryController cargoInventory)
        {
            boat = player;
            inventory = cargoInventory;
            activeTexture = UiTextureFactory.LoadSpeedSegment(true);
            inactiveTexture = UiTextureFactory.LoadSpeedSegment(false);
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            RectTransform root = CreateRect("Engine Telegraph", canvas);
            gaugeRoot = root.gameObject;
            root.anchoredPosition = new Vector2(0f, -575f);
            root.sizeDelta = new Vector2(255f, 102f);
            Image background = root.gameObject.AddComponent<Image>();
            background.sprite = UiTextureFactory.LoadPillSprite();
            background.type = Image.Type.Sliced;
            background.color = new Color(1f, 1f, 1f, 0.96f);
            background.raycastTarget = false;

            for (int i = 0; i < segments.Length; i++)
            {
                float t = i / (float)(segments.Length - 1);
                float degrees = Mathf.Lerp(154f, 26f, t);
                float radians = degrees * Mathf.Deg2Rad;
                RectTransform rect = CreateRect($"Telegraph Segment {i + 1}", root);
                rect.anchoredPosition = new Vector2(Mathf.Cos(radians) * 82f, 8f + Mathf.Sin(radians) * 62f);
                rect.sizeDelta = new Vector2(17f, 35f);
                rect.localRotation = Quaternion.Euler(0f, 0f, degrees - 90f);
                segments[i] = rect.gameObject.AddComponent<RawImage>();
                segments[i].raycastTarget = false;
            }

            needle = CreateRect("Telegraph Needle", root);
            needle.pivot = new Vector2(0.5f, 0.08f);
            needle.anchoredPosition = new Vector2(0f, -15f);
            needle.sizeDelta = new Vector2(14f, 62f);
            RawImage needleImage = needle.gameObject.AddComponent<RawImage>();
            needleImage.texture = UiTextureFactory.LoadSpeedNeedle();
            needleImage.raycastTarget = false;

            stateLabel = CreateText("Speed State", root, font, 17, new Vector2(0f, -33f), new Vector2(128f, 24f));
            stateLabel.color = new Color(1f, 0.76f, 0.27f, 1f);
            stepLabel = CreateText("Speed Step Hint", root, font, 11, new Vector2(0f, -51f), new Vector2(150f, 18f));
            stepLabel.color = new Color(0.69f, 0.83f, 0.81f, 0.95f);
            Refresh(true);
        }

        private void Update()
        {
            if (gaugeRoot != null) gaugeRoot.SetActive(inventory == null || !inventory.IsOpen);
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

            // Lamps show the engine order; the needle follows actual speed and visibly lags while coasting.
            float normalized = SpeedGaugeModel.GetActualNeedle01(boat.Speed, boat.MaxSpeed);
            needle.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(62f, -62f, normalized));
            stateLabel.text = SpeedGaugeModel.GetMotionLabel(step, max, boat.Speed, boat.TargetSpeed);
            if (boat.IsCoasting)
            {
                stepLabel.text = step == 0 ? $"DRIFT  {boat.Speed:0.0}" : $"SLOWING  {step}/{max}";
            }
            else
            {
                stepLabel.text = step == 0 ? "W  AHEAD" : $"W +   {step}/{max}   S -";
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
