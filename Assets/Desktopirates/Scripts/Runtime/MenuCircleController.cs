using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Desktopirates
{
    [RequireComponent(typeof(RawImage))]
    public sealed class MenuCircleController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public const float DragThreshold = 7f;
        private RawImage image;
        private DayNightVisualController dayNight;
        private WindowsOverlayController windowOverlay;
        private MenuController menu;
        private Texture2D generatedTexture;
        private RawImage liveTimeFace;
        private float shownHour = -100f;
        private Vector3 targetScale = Vector3.one;
        private Vector2 pointerStart;
        private bool dragged;

        public void Initialize(DayNightVisualController visualController, WindowsOverlayController overlayController, MenuController menuController)
        {
            image = GetComponent<RawImage>();
            image.raycastTarget = true;
            dayNight = visualController;
            windowOverlay = overlayController;
            menu = menuController;
            Texture2D authoredCircle = UiTextureFactory.LoadGeneratedMenuCircle();
            if (authoredCircle != null)
            {
                image.texture = authoredCircle;
                var faceObject = new GameObject("Live Time Face", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
                faceObject.transform.SetParent(transform, false);
                liveTimeFace = faceObject.GetComponent<RawImage>();
                liveTimeFace.raycastTarget = false;
                liveTimeFace.rectTransform.anchorMin = liveTimeFace.rectTransform.anchorMax = Vector2.one * 0.5f;
                liveTimeFace.rectTransform.pivot = Vector2.one * 0.5f;
                liveTimeFace.rectTransform.anchoredPosition = new Vector2(0f, 3f);
                liveTimeFace.rectTransform.sizeDelta = new Vector2(54f, 54f);
            }
            RefreshTexture(true);
        }

        private void Update()
        {
            RefreshTexture(false);
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * 12f);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pointerStart = eventData.position;
            dragged = false;
            windowOverlay?.BeginPointerDrag();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (Vector2.Distance(pointerStart, eventData.position) >= DragThreshold) dragged = true;
            if (dragged) windowOverlay?.UpdatePointerDrag();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            windowOverlay?.EndPointerDrag();
            if (!dragged && Vector2.Distance(pointerStart, eventData.position) < DragThreshold) menu?.ToggleMenu();
        }

        public void OnPointerEnter(PointerEventData eventData) => targetScale = Vector3.one * 1.08f;
        public void OnPointerExit(PointerEventData eventData) => targetScale = Vector3.one;

        private void RefreshTexture(bool force)
        {
            if (dayNight == null || image == null) return;
            float hour = dayNight.CurrentHour;
            if (!force && Mathf.Abs(hour - shownHour) < 0.01f) return;
            shownHour = hour;
            if (generatedTexture != null) Destroy(generatedTexture);
            generatedTexture = PixelTextureFactory.CreateTimeOrb(hour, 96);
            if (liveTimeFace != null) liveTimeFace.texture = generatedTexture;
            else image.texture = generatedTexture;
        }

        private void OnDestroy() { if (generatedTexture != null) Destroy(generatedTexture); }
    }
}
