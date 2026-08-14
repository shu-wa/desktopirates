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
            image.texture = UiTextureFactory.LoadMenuCircleFrame();
            Texture2D timeFace = UiTextureFactory.LoadTimeFace(dayNight != null ? dayNight.CurrentHour : 12f);
            if (timeFace != null)
            {
                var faceObject = new GameObject("Authored Time Face", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
                faceObject.transform.SetParent(transform, false);
                RawImage face = faceObject.GetComponent<RawImage>();
                face.texture = timeFace;
                face.raycastTarget = false;
                face.rectTransform.anchorMin = face.rectTransform.anchorMax = Vector2.one * 0.5f;
                face.rectTransform.pivot = Vector2.one * 0.5f;
                face.rectTransform.anchoredPosition = new Vector2(0f, 2f);
                face.rectTransform.sizeDelta = new Vector2(70f, 70f);
                face.transform.SetAsFirstSibling();
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
            RawImage face = transform.Find("Authored Time Face")?.GetComponent<RawImage>();
            if (face != null) face.texture = UiTextureFactory.LoadTimeFace(hour);
        }
    }
}
