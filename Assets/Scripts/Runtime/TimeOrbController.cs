using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Desktopirates
{
    [RequireComponent(typeof(RawImage))]
    public sealed class TimeOrbController : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private RawImage image;
        private DayNightVisualController dayNight;
        private WindowsOverlayController windowOverlay;
        private Texture2D generatedTexture;
        private float shownHour = -100f;
        private Vector3 targetScale = Vector3.one;

        public void Initialize(DayNightVisualController visualController, WindowsOverlayController overlayController)
        {
            image = GetComponent<RawImage>();
            image.raycastTarget = true;
            dayNight = visualController;
            windowOverlay = overlayController;
            RefreshTexture(true);
        }

        private void Update()
        {
            RefreshTexture(false);
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * 12f);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            windowOverlay?.BeginWindowDrag();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            targetScale = Vector3.one * 1.08f;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            targetScale = Vector3.one;
        }

        private void RefreshTexture(bool force)
        {
            if (dayNight == null || image == null) return;
            float hour = dayNight.CurrentHour;
            if (!force && Mathf.Abs(hour - shownHour) < 0.01f) return;

            shownHour = hour;
            if (generatedTexture != null) Destroy(generatedTexture);
            generatedTexture = PixelTextureFactory.CreateTimeOrb(hour, 96);
            image.texture = generatedTexture;
        }

        private void OnDestroy()
        {
            if (generatedTexture != null) Destroy(generatedTexture);
        }
    }
}
