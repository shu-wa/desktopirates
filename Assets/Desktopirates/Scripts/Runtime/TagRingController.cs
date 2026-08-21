using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Desktopirates
{
    public sealed class TagRingController : MonoBehaviour
    {
        public const float RimPadding = 6f;
        public const float WindowPadding = 8f;
        public const float MarkerSeparation = 5f;
        private const int VisibleMarkerLimit = 5;
        private readonly List<RawImage> markers = new List<RawImage>();
        private readonly List<ulong> markerIds = new List<ulong>();
        private readonly List<int> visibleIndices = new List<int>();
        private readonly List<Rect> placedMarkerRects = new List<Rect>();
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
                // Direction tags belong behind every HUD/modal surface. Their centers can
                // correctly sit beyond the top sea rim without covering HULL/GOLD cards.
                markerObject.transform.SetAsFirstSibling();
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
            visibleIndices.Clear();
            for (int i = 0; i < markers.Count; i++) markers[i].gameObject.SetActive(false);
            for (int i = 0; i < poiSystem.Items.Count; i++)
            {
                PoiRecord poi = poiSystem.Items[i];
                Vector2 relative = poi.LogicalPosition - player.LogicalPosition;
                float distance = relative.magnitude;
                if (!poi.Resolved && distance > PoiSystem.VisibleRadius) visibleIndices.Add(i);
            }
            visibleIndices.Sort((left, right) =>
            {
                float leftDistance = Vector2.SqrMagnitude(poiSystem.Items[left].LogicalPosition - player.LogicalPosition);
                float rightDistance = Vector2.SqrMagnitude(poiSystem.Items[right].LogicalPosition - player.LogicalPosition);
                int distanceOrder = leftDistance.CompareTo(rightDistance);
                return distanceOrder != 0 ? distanceOrder : poiSystem.Items[left].Id.CompareTo(poiSystem.Items[right].Id);
            });

            placedMarkerRects.Clear();
            int count = Mathf.Min(VisibleMarkerLimit, visibleIndices.Count);
            for (int rank = 0; rank < count; rank++)
            {
                int i = visibleIndices[rank];
                PoiRecord poi = poiSystem.Items[i];
                RawImage marker = markers[i];
                Vector2 relative = poi.LogicalPosition - player.LogicalPosition;
                float distance = relative.magnitude;

                float pixels = DistanceTagMath.PixelSizeForDistance(distance);
                Vector2 direction = relative.sqrMagnitude > 0.001f ? relative.normalized : Vector2.up;
                if (!TryFindClearPosition(direction, pixels, out Vector2 position))
                {
                    // A hidden far hint is preferable to two unreadable badges occupying the
                    // same pixels. It returns automatically as soon as its rim sector clears.
                    continue;
                }
                marker.gameObject.SetActive(true);
                marker.rectTransform.anchoredPosition = position;
                marker.rectTransform.sizeDelta = new Vector2(pixels, pixels);
                marker.color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0.72f, Mathf.InverseLerp(8f, 70f, distance)));
                placedMarkerRects.Add(DistanceTagMath.MarkerRect(position, pixels, MarkerSeparation));
            }
        }

        private bool TryFindClearPosition(Vector2 worldDirection, float pixels, out Vector2 position)
        {
            // Never rotate a discovery away from its true bearing. HUD avoidance used to try
            // many angular offsets, making north/south markers appear to teleport. When the
            // window edge is tight we now pull the marker inward on the same ray, allowing a
            // small overlap with the sea while the HUD remains drawn above it.
            Vector2 candidate = ProjectOutsideRim(worldDirection, pixels, out Vector2 discCenter);
            candidate = DistanceTagMath.PullAlongRayInside(discCenter, candidate, canvas.rect, pixels, WindowPadding);
            Rect markerRect = DistanceTagMath.MarkerRect(candidate, pixels, MarkerSeparation);
            if (!DistanceTagMath.IsClear(markerRect, placedMarkerRects))
            {
                position = default;
                return false;
            }
            position = candidate;
            return true;
        }

        private Vector2 ProjectOutsideRim(Vector2 direction, float pixels, out Vector2 discCenter)
        {
            Camera worldCamera = cameraRig.WorldCamera;
            if (worldCamera != null
                && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, worldCamera.WorldToScreenPoint(Vector3.zero), null, out discCenter)
                && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvas,
                    worldCamera.WorldToScreenPoint(new Vector3(direction.x * OceanDisc.Radius, 0f, direction.y * OceanDisc.Radius)),
                    null,
                    out Vector2 rimPosition))
                return DistanceTagMath.PositionOutsideRim(discCenter, rimPosition, pixels, RimPadding);

            Vector2 screenDirection = DistanceTagMath.WorldToScreenDirection(direction, cameraRig.CurrentYaw);
            discCenter = new Vector2(0f, -63f);
            Vector2 fallbackRim = DistanceTagMath.PositionOnEllipse(screenDirection, discCenter, new Vector2(260f, 182f));
            return DistanceTagMath.PositionOutsideRim(discCenter, fallbackRim, pixels, RimPadding);
        }

        private bool MarkersAreStale()
        {
            if (markerIds.Count != poiSystem.Items.Count) return true;
            for (int i = 0; i < markerIds.Count; i++) if (markerIds[i] != poiSystem.Items[i].Id) return true;
            return false;
        }

        // Textures are shared Resources assets. Destroy only marker objects, never their shared texture.
    }
}
