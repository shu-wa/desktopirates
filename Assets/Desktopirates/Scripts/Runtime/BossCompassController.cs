using UnityEngine;
using UnityEngine.UI;

namespace Desktopirates
{
    public static class BossCompassModel
    {
        public const int PurchaseCost = 240;

        public static float GetNeedleAngle(Vector2 worldDirection, float cameraYawDegrees)
        {
            Vector2 screenDirection = DistanceTagMath.WorldToScreenDirection(worldDirection, cameraYawDegrees);
            return -Mathf.Atan2(screenDirection.x, screenDirection.y) * Mathf.Rad2Deg;
        }
    }

    /// <summary>Optional brass instrument that points toward the nearest undefeated boss.</summary>
    public sealed class BossCompassController : MonoBehaviour
    {
        private GameState state;
        private BoatController boat;
        private CameraRigController cameraRig;
        private InventoryController inventory;
        private MenuController menu;
        private RectTransform root;
        private RectTransform needle;
        private Text readout;
        private GeneratedEventData target;
        private bool hasTarget;
        private float nextTargetRefresh;

        public void Initialize(RectTransform canvas, GameState gameState, BoatController player, CameraRigController rig, InventoryController cargo, MenuController menuController)
        {
            state = gameState;
            boat = player;
            cameraRig = rig;
            inventory = cargo;
            menu = menuController;

            var rootObject = new GameObject("Boss Compass", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            rootObject.transform.SetParent(canvas, false);
            root = rootObject.GetComponent<RectTransform>();
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = new Vector2(-282f, -250f);
            root.sizeDelta = new Vector2(132f, 156f);
            Image plate = rootObject.GetComponent<Image>();
            plate.sprite = UiTextureFactory.LoadConceptSprite("Chrome", "radial_hub");
            plate.color = Color.white;
            plate.raycastTarget = false;

            var needleObject = new GameObject("Boss Bearing Needle", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            needleObject.transform.SetParent(root, false);
            needle = needleObject.GetComponent<RectTransform>();
            needle.anchoredPosition = new Vector2(0f, 15f);
            needle.sizeDelta = new Vector2(54f, 54f);
            RawImage needleImage = needleObject.GetComponent<RawImage>();
            needleImage.texture = UiTextureFactory.LoadCompassArrow();
            needleImage.color = new Color(1f, 0.48f, 0.12f, 1f);
            needleImage.raycastTarget = false;

            var readoutObject = new GameObject("Boss Compass Readout", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            readoutObject.transform.SetParent(root, false);
            RectTransform readoutRect = readoutObject.GetComponent<RectTransform>();
            readoutRect.anchoredPosition = new Vector2(0f, -49f);
            readoutRect.sizeDelta = new Vector2(118f, 43f);
            readout = readoutObject.GetComponent<Text>();
            readout.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            readout.fontSize = 11;
            readout.alignment = TextAnchor.MiddleCenter;
            readout.color = UiTheme.Brass;
            readout.horizontalOverflow = HorizontalWrapMode.Wrap;
            readout.verticalOverflow = VerticalWrapMode.Truncate;
            UiTheme.StyleText(readout, 11);
            RefreshTarget();
        }

        private void Update()
        {
            if (root == null || state == null || boat == null || cameraRig == null) return;
            bool navigationVisible = state.BossCompassOwned
                && (inventory == null || !inventory.IsOpen)
                && (menu == null || !menu.IsModalOpen);
            root.gameObject.SetActive(navigationVisible);
            if (!navigationVisible) return;

            if (Time.unscaledTime >= nextTargetRefresh) RefreshTarget();
            if (!hasTarget)
            {
                needle.gameObject.SetActive(false);
                readout.text = "NO BOSS\nSIGNAL";
                return;
            }

            needle.gameObject.SetActive(true);
            Vector2 relative = target.Position - boat.LogicalPosition;
            float angle = BossCompassModel.GetNeedleAngle(relative, cameraRig.CurrentYaw);
            needle.localRotation = Quaternion.Euler(0f, 0f, angle);
            readout.text = $"{EnemyIdentityModel.GetName(target.Id, target.Boss)}\n{relative.magnitude:0} m";
        }

        private void RefreshTarget()
        {
            nextTargetRefresh = Time.unscaledTime + 1f;
            hasTarget = state != null && boat != null
                && WorldGenerator.TryFindNearestBoss(state.WorldSeed, boat.LogicalPosition, state.ResolvedEvents, out target);
        }
    }
}
