using UnityEngine;
using UnityEngine.UI;

namespace Desktopirates
{
    public sealed class SeaRegionController : MonoBehaviour
    {
        private GameState state;
        private BoatController boat;
        private OceanDisc ocean;
        private InventoryController inventory;
        private MenuController menu;
        private GameObject badge;
        private Text label;
        private SeaRegionKind current = (SeaRegionKind)255;
        private Color targetTint;
        private float nextSample;
        private float discoveryUntil;

        public SeaRegionKind Current => current;

        public void Initialize(RectTransform canvas, GameState gameState, BoatController player, OceanDisc oceanDisc, InventoryController cargo, MenuController menuController)
        {
            state = gameState;
            boat = player;
            ocean = oceanDisc;
            inventory = cargo;
            menu = menuController;

            var badgeObject = new GameObject("Sea Region Badge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            badgeObject.transform.SetParent(canvas, false);
            var rect = badgeObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(18f, -62f);
            rect.sizeDelta = new Vector2(252f, 52f);
            Image frame = badgeObject.GetComponent<Image>();
            frame.sprite = UiTextureFactory.LoadConceptSprite("Chrome", "tooltip_card", 18f);
            frame.type = Image.Type.Sliced;
            frame.color = Color.white;
            frame.raycastTarget = false;

            var fillObject = new GameObject("Sea Region Navy Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            fillObject.transform.SetParent(rect, false);
            var fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = new Vector2(-18f, -12f);
            RawImage fill = fillObject.GetComponent<RawImage>();
            fill.texture = UiTextureFactory.LoadConceptTexture("Chrome", "button_fill");
            fill.raycastTarget = false;
            fillRect.SetAsFirstSibling();

            var textObject = new GameObject("Sea Region Name", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(rect, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = new Vector2(-24f, -10f);
            label = textObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 13;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = UiTheme.PrimaryText;
            label.raycastTarget = false;
            UiTheme.StyleText(label, 13);
            badge = badgeObject;
            SampleRegion(true);
        }

        private void Update()
        {
            if (state == null || boat == null || ocean == null) return;
            if (Time.unscaledTime >= nextSample) SampleRegion(false);
            ocean.Tint = Color.Lerp(ocean.Tint, targetTint, 1f - Mathf.Exp(-Time.deltaTime * 1.8f));
            if (badge != null)
                badge.SetActive((inventory == null || !inventory.IsOpen) && (menu == null || !menu.IsModalOpen));
        }

        private void SampleRegion(bool force)
        {
            nextSample = Time.unscaledTime + 0.25f;
            SeaRegionProfile profile = SeaRegionModel.At(state.WorldSeed, boat.LogicalPosition);
            state.RegionSpeedMultiplier = profile.SpeedMultiplier;
            state.RegionTurnMultiplier = profile.TurnMultiplier;
            targetTint = profile.OceanTint;
            if (!force && profile.Kind == current)
            {
                if (Time.unscaledTime >= discoveryUntil) label.text = $"{profile.Name}\n{profile.EffectLabel}";
                return;
            }

            current = profile.Kind;
            bool first = state.Captain.DiscoverRegion(profile.Kind);
            discoveryUntil = first ? Time.unscaledTime + 4f : 0f;
            label.text = first ? $"NEW CHART: {profile.Name}\n{profile.EffectLabel}" : $"{profile.Name}\n{profile.EffectLabel}";
        }
    }
}
