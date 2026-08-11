using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Desktopirates
{
    public sealed class DesktopiratesGame : MonoBehaviour
    {
        private static readonly Color TransparentKey = new Color32(255, 0, 255, 255);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<DesktopiratesGame>() != null) return;
            new GameObject("desktopirates Runtime").AddComponent<DesktopiratesGame>();
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Application.runInBackground = true;
            Application.targetFrameRate = 30;
            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 0;
            QualitySettings.shadows = ShadowQuality.Disable;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.skybox = null;

            WindowsOverlayController overlay = gameObject.AddComponent<WindowsOverlayController>();
            Camera camera = CreateCamera();
            Light sun = CreateSun();

            var world = new GameObject("Circular Sea World").transform;
            world.SetParent(transform, false);

            var oceanObject = new GameObject("Ocean Disc", typeof(MeshFilter), typeof(MeshRenderer), typeof(OceanDisc));
            oceanObject.transform.SetParent(world, false);
            OceanDisc ocean = oceanObject.GetComponent<OceanDisc>();
            ocean.Initialize();

            Transform boatVisual = ProceduralSceneFactory.CreatePlayerBoat(world);
            BoatController boat = gameObject.AddComponent<BoatController>();
            boat.Initialize(boatVisual);

            CameraRigController cameraRig = gameObject.AddComponent<CameraRigController>();
            cameraRig.Initialize(camera);

            var poiObject = new GameObject("Hidden Map POIs", typeof(PoiSystem));
            poiObject.transform.SetParent(world, false);
            PoiSystem poiSystem = poiObject.GetComponent<PoiSystem>();
            poiSystem.Initialize(boat);

            DayNightVisualController dayNight = gameObject.AddComponent<DayNightVisualController>();
            dayNight.Initialize(ocean, sun);

            RectTransform canvas = CreateCanvas();
            CreateCompassArc(canvas);
            CreateTimeOrb(canvas, dayNight, overlay);

            var tagObject = new GameObject("Perimeter Tags", typeof(TagRingController));
            tagObject.transform.SetParent(canvas, false);
            tagObject.GetComponent<TagRingController>().Initialize(canvas, boat, cameraRig, poiSystem);
        }

        private static Camera CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = TransparentKey;
            camera.orthographic = true;
            camera.orthographicSize = 7.9f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 80f;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            return camera;
        }

        private static Light CreateSun()
        {
            var lightObject = new GameObject("Time Of Day Light", typeof(Light));
            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.None;
            light.intensity = 0.85f;
            light.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            return light;
        }

        private static RectTransform CreateCanvas()
        {
            var canvasObject = new GameObject("Circular Overlay UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720f, 760f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (FindFirstObjectByType<EventSystem>() == null)
                new GameObject("Event System", typeof(EventSystem), typeof(StandaloneInputModule));

            return canvasObject.GetComponent<RectTransform>();
        }

        private static void CreateTimeOrb(RectTransform canvas, DayNightVisualController dayNight, WindowsOverlayController overlay)
        {
            var connectorObject = new GameObject("Time Orb Connector", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            connectorObject.transform.SetParent(canvas, false);
            Image connector = connectorObject.GetComponent<Image>();
            connector.color = new Color(0.78f, 0.54f, 0.20f, 0.95f);
            connector.raycastTarget = false;
            connector.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            connector.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            connector.rectTransform.pivot = new Vector2(0.5f, 1f);
            connector.rectTransform.anchoredPosition = new Vector2(0f, -109f);
            connector.rectTransform.sizeDelta = new Vector2(5f, 34f);

            var orbObject = new GameObject("Current Time Drag Orb", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(TimeOrbController));
            orbObject.transform.SetParent(canvas, false);
            RawImage orb = orbObject.GetComponent<RawImage>();
            orb.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            orb.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            orb.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            orb.rectTransform.anchoredPosition = new Vector2(0f, -66f);
            orb.rectTransform.sizeDelta = new Vector2(88f, 88f);
            orbObject.GetComponent<TimeOrbController>().Initialize(dayNight, overlay);
        }

        private static void CreateCompassArc(RectTransform canvas)
        {
            for (int i = 0; i < 9; i++)
            {
                float degrees = Mathf.Lerp(205f, 335f, i / 8f);
                float radians = degrees * Mathf.Deg2Rad;
                var tickObject = new GameObject($"Compass Tick {i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                tickObject.transform.SetParent(canvas, false);
                Image tick = tickObject.GetComponent<Image>();
                tick.color = i == 4 ? new Color(1f, 0.45f, 0.12f, 0.95f) : new Color(0.86f, 0.80f, 0.62f, 0.75f);
                tick.raycastTarget = false;
                RectTransform rect = tick.rectTransform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(Mathf.Cos(radians) * 237f, -63f + Mathf.Sin(radians) * 164f);
                rect.sizeDelta = i == 4 ? new Vector2(7f, 18f) : new Vector2(4f, 11f);
                rect.localRotation = Quaternion.Euler(0f, 0f, degrees - 90f);
            }
        }
    }
}
