using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Desktopirates
{
    public sealed class TagRingController : MonoBehaviour
    {
        public const float HudSafeTop = 105f;
        private readonly List<RawImage> markers = new List<RawImage>();
        private readonly List<ulong> markerIds = new List<ulong>();
        private RectTransform canvas;
        private BoatController player;
        private CameraRigController cameraRig;
        private PoiSystem poiSystem;
        private InventoryController inventory;
        private MenuController menu;

        public void Initialize(RectTransform canvas, BoatController boat, CameraRigController rig, PoiSystem pois, InventoryController cargoInventory, MenuController menuController = null)
        {
            this.canvas = canvas;
            player = boat;
            cameraRig = rig;
            poiSystem = pois;
            inventory = cargoInventory;
            menu = menuController;

            RebuildMarkers();
        }

        private void RebuildMarkers()
        {
            foreach (RawImage old in markers)
            {
                if (old != null) Destroy(old.gameObject);
            }
            markers.Clear();
            markerIds.Clear();
            foreach (PoiRecord poi in poiSystem.Items)
            {
                var markerObject = new GameObject($"{poi.Kind} Direction Tag", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
                markerObject.transform.SetParent(canvas, false);
                var marker = markerObject.GetComponent<RawImage>();
                marker.texture = UiTextureFactory.LoadPoiBadge(poi.Kind, 128);
                marker.color = Color.white;
                marker.raycastTarget = false;
                marker.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                marker.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                marker.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                markers.Add(marker);
                markerIds.Add(poi.Id);
            }
        }

        private void LateUpdate()
        {
            if (player == null || cameraRig == null || poiSystem == null) return;
            if (MarkersAreStale()) RebuildMarkers();
            if ((inventory != null && inventory.IsOpen) || (menu != null && menu.IsModalOpen))
            {
                foreach (RawImage marker in markers) if (marker != null) marker.gameObject.SetActive(false);
                return;
            }
            for (int i = 0; i < poiSystem.Items.Count; i++)
            {
                PoiRecord poi = poiSystem.Items[i];
                RawImage marker = markers[i];
                Vector2 relative = poi.LogicalPosition - player.LogicalPosition;
                float distance = relative.magnitude;
                bool showTag = !poi.Resolved && distance > PoiSystem.VisibleRadius && IsAmongNearest(i, distance, 5);
                marker.gameObject.SetActive(showTag);
                if (!showTag) continue;

                Vector2 direction = relative.sqrMagnitude > 0.001f ? relative.normalized : Vector2.up;
                Vector2 screenDirection = DistanceTagMath.WorldToScreenDirection(direction, cameraRig.CurrentYaw);
                Vector2 discCenter = new Vector2(0f, -63f);
                Vector2 rimPosition = DistanceTagMath.PositionOnEllipse(
                    screenDirection,
                    discCenter,
                    new Vector2(312f, 218f));
                // The physical top rim sits behind the dashboard. Preserve horizontal
                // bearing but move that short arc below the HUD, accounting for the
                // largest 98 px marker so no artwork can cover HULL/GOLD/CREW/LOAD.
                rimPosition.y = Mathf.Min(rimPosition.y, HudSafeTop);
                marker.rectTransform.anchoredPosition = rimPosition;

                float pixels = DistanceTagMath.PixelSizeForDistance(distance);
                marker.rectTransform.sizeDelta = new Vector2(pixels, pixels);
                marker.color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0.58f, Mathf.InverseLerp(8f, 60f, distance)));
            }
        }

        private bool MarkersAreStale()
        {
            if (markerIds.Count != poiSystem.Items.Count) return true;
            for (int i = 0; i < markerIds.Count; i++) if (markerIds[i] != poiSystem.Items[i].Id) return true;
            return false;
        }

        private bool IsAmongNearest(int itemIndex, float distance, int limit)
        {
            int nearer = 0;
            for (int i = 0; i < poiSystem.Items.Count; i++)
            {
                if (i == itemIndex || poiSystem.Items[i].Resolved) continue;
                if (Vector2.Distance(poiSystem.Items[i].LogicalPosition, player.LogicalPosition) < distance && ++nearer >= limit) return false;
            }
            return true;
        }

        // Textures are shared Resources assets. Destroy only marker objects, never their shared texture.
    }
}
