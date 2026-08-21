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
        private static readonly float[] BearingOffsets =
        {
            0f, 7.5f, -7.5f, 15f, -15f, 22.5f, -22.5f, 30f, -30f,
            37.5f, -37.5f, 45f, -45f, 52.5f, -52.5f, 60f, -60f,
            67.5f, -67.5f, 75f, -75f, 82.5f, -82.5f, 90f, -90f
        };
        private readonly List<RawImage> markers = new List<RawImage>();
        private readonly List<ulong> markerIds = new List<ulong>();
        private readonly List<int> visibleIndices = new List<int>();
        private readonly List<Rect> obstacleRects = new List<Rect>();
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

            CollectObstacleRects();
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
                marker.color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0.58f, Mathf.InverseLerp(8f, 60f, distance)));
                placedMarkerRects.Add(DistanceTagMath.MarkerRect(position, pixels, MarkerSeparation));
            }
        }

        private bool TryFindClearPosition(Vector2 worldDirection, float pixels, out Vector2 position)
        {
            for (int i = 0; i < BearingOffsets.Length; i++)
            {
                Vector2 candidateDirection = DistanceTagMath.Rotate(worldDirection, BearingOffsets[i]);
                Vector2 candidate = ProjectOutsideRim(candidateDirection, pixels);
                candidate = DistanceTagMath.ClampMarkerInside(candidate, canvas.rect, pixels, WindowPadding);
                Rect markerRect = DistanceTagMath.MarkerRect(candidate, pixels, MarkerSeparation);
                if (!DistanceTagMath.IsClear(markerRect, obstacleRects)) continue;
                if (!DistanceTagMath.IsClear(markerRect, placedMarkerRects)) continue;
                position = candidate;
                return true;
            }
            position = default;
            return false;
        }

        private Vector2 ProjectOutsideRim(Vector2 direction, float pixels)
        {
            Camera worldCamera = cameraRig.WorldCamera;
            if (worldCamera != null
                && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, worldCamera.WorldToScreenPoint(Vector3.zero), null, out Vector2 discCenter)
                && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvas,
                    worldCamera.WorldToScreenPoint(new Vector3(direction.x * OceanDisc.Radius, 0f, direction.y * OceanDisc.Radius)),
                    null,
                    out Vector2 rimPosition))
                return DistanceTagMath.PositionOutsideRim(discCenter, rimPosition, pixels, RimPadding);

            Vector2 screenDirection = DistanceTagMath.WorldToScreenDirection(direction, cameraRig.CurrentYaw);
            Vector2 fallbackCenter = new Vector2(0f, -63f);
            Vector2 fallbackRim = DistanceTagMath.PositionOnEllipse(screenDirection, fallbackCenter, new Vector2(260f, 182f));
            return DistanceTagMath.PositionOutsideRim(fallbackCenter, fallbackRim, pixels, RimPadding);
        }

        private void CollectObstacleRects()
        {
            obstacleRects.Clear();
            foreach (Transform child in canvas)
            {
                if (!child.gameObject.activeInHierarchy) continue;
                if (child.name == "Enemy HUD Layer")
                {
                    foreach (Transform enemyPlate in child)
                        if (enemyPlate.gameObject.activeInHierarchy && enemyPlate is RectTransform enemyRect) AddObstacle(enemyRect, 4f);
                    continue;
                }
                if (!(child is RectTransform rect) || !IsHudObstacle(child.name)) continue;
                AddObstacle(rect, 5f);
            }
        }

        private void AddObstacle(RectTransform rect, float padding)
        {
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(canvas, rect);
            Rect area = Rect.MinMaxRect(bounds.min.x, bounds.min.y, bounds.max.x, bounds.max.y);
            if (area.width > 1f && area.height > 1f) obstacleRects.Add(DistanceTagMath.Expanded(area, padding));
        }

        private static bool IsHudObstacle(string objectName)
        {
            return objectName == "Ship Dashboard"
                || objectName == "Menu Circle"
                || objectName == "Compact Engine Telegraph"
                || objectName == "Cargo Bag Button"
                || objectName == "Cargo Tab Hint Pill"
                || objectName == "Boss Compass"
                || objectName == "Sea Region Badge"
                || objectName == "Context Action Pill"
                || objectName == "Event Message Pill";
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
