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
            QualitySettings.shadows = ShadowQuality.HardOnly;
            QualitySettings.shadowResolution = ShadowResolution.Low;
            QualitySettings.shadowDistance = 18f;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.skybox = null;

            WindowsOverlayController overlay = gameObject.AddComponent<WindowsOverlayController>();
            gameObject.AddComponent<AmbientAudioController>();
            var saves = new SaveSystem();
            GameState state = saves.LoadOrNew();
            Camera camera = CreateCamera();
            Light sun = CreateSun();
            CreateFillLight();

            var world = new GameObject("Circular Sea World").transform;
            world.SetParent(transform, false);

            var oceanObject = new GameObject("Ocean Disc", typeof(MeshFilter), typeof(MeshRenderer), typeof(OceanDisc));
            oceanObject.transform.SetParent(world, false);
            OceanDisc ocean = oceanObject.GetComponent<OceanDisc>();
            ocean.Initialize();

            Transform boatVisual = ProceduralSceneFactory.CreatePlayerBoat(world);
            BoatController boat = gameObject.AddComponent<BoatController>();
            boat.Initialize(boatVisual, state);
            ocean.Bind(boat);

            CameraRigController cameraRig = gameObject.AddComponent<CameraRigController>();
            cameraRig.Initialize(camera);

            var poiObject = new GameObject("Hidden Map POIs", typeof(PoiSystem));
            poiObject.transform.SetParent(world, false);
            PoiSystem poiSystem = poiObject.GetComponent<PoiSystem>();
            poiSystem.Initialize(boat, state);

            DayNightVisualController dayNight = gameObject.AddComponent<DayNightVisualController>();
            dayNight.Initialize(ocean, sun);

            RectTransform canvas = CreateCanvas();
            GameObject compass = CreateCompassArc(canvas);
            InventoryController inventory = gameObject.AddComponent<InventoryController>();
            inventory.Initialize(canvas, state);
            MenuController menu = gameObject.AddComponent<MenuController>();
            menu.Initialize(canvas, overlay, state, saves, boat, poiSystem, inventory, compass);
            SpeedGaugeController speedGauge = gameObject.AddComponent<SpeedGaugeController>();
            speedGauge.Initialize(canvas, boat, inventory, menu);
            CreateMenuCircle(canvas, dayNight, overlay, menu);

            var tagObject = new GameObject("Perimeter Tags", typeof(TagRingController));
            tagObject.transform.SetParent(canvas, false);
            tagObject.GetComponent<TagRingController>().Initialize(canvas, boat, cameraRig, poiSystem, inventory, menu);
        }

        private static Camera CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = TransparentKey;
            camera.orthographic = true;
            camera.orthographicSize = 7.65f;
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
            light.shadows = LightShadows.Hard;
            light.intensity = 0.85f;
            light.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            return light;
        }

        private static void CreateFillLight()
        {
            var fillObject = new GameObject("Cool Ocean Fill Light", typeof(Light));
            Light fill = fillObject.GetComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.28f, 0.50f, 0.62f);
            fill.intensity = 0.28f;
            fill.shadows = LightShadows.None;
            fill.transform.rotation = Quaternion.Euler(58f, 142f, 0f);
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

        private static void CreateMenuCircle(RectTransform canvas, DayNightVisualController dayNight, WindowsOverlayController overlay, MenuController menu)
        {
            var connectorObject = new GameObject("Menu Circle Connector", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            connectorObject.transform.SetParent(canvas, false);
            Image connector = connectorObject.GetComponent<Image>();
            connector.color = new Color(0.78f, 0.54f, 0.20f, 0.95f);
            connector.raycastTarget = false;
            connector.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            connector.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            connector.rectTransform.pivot = new Vector2(0.5f, 1f);
            connector.rectTransform.anchoredPosition = new Vector2(0f, -109f);
            connector.rectTransform.sizeDelta = new Vector2(5f, 34f);

            var handleObject = new GameObject("Menu Circle Brass Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            handleObject.transform.SetParent(canvas, false);
            Image handle = handleObject.GetComponent<Image>();
            handle.sprite = UiTextureFactory.LoadDiamondSprite(24);
            handle.color = Color.white;
            handle.raycastTarget = false;
            handle.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            handle.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            handle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            handle.rectTransform.anchoredPosition = new Vector2(0f, -126f);
            handle.rectTransform.sizeDelta = new Vector2(18f, 18f);

            var orbObject = new GameObject("Menu Circle", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(MenuCircleController));
            orbObject.transform.SetParent(canvas, false);
            RawImage orb = orbObject.GetComponent<RawImage>();
            orb.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            orb.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            orb.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            orb.rectTransform.anchoredPosition = new Vector2(0f, -66f);
            orb.rectTransform.sizeDelta = new Vector2(88f, 88f);
            orbObject.GetComponent<MenuCircleController>().Initialize(dayNight, overlay, menu);
        }

        private static GameObject CreateCompassArc(RectTransform canvas)
        {
            var root = new GameObject("Compass Arc", typeof(RectTransform));
            root.transform.SetParent(canvas, false);
            for (int i = 0; i < 9; i++)
            {
                float degrees = Mathf.Lerp(205f, 335f, i / 8f);
                float radians = degrees * Mathf.Deg2Rad;
                var tickObject = new GameObject($"Compass Tick {i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
                tickObject.transform.SetParent(root.transform, false);
                RawImage tick = tickObject.GetComponent<RawImage>();
                tick.texture = UiTextureFactory.LoadCompassArrow();
                tick.color = i == 4 ? new Color(1f, 0.42f, 0.08f, 1f) : new Color(0.86f, 0.62f, 0.25f, 0.92f);
                tick.raycastTarget = false;
                RectTransform rect = tick.rectTransform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(Mathf.Cos(radians) * 237f, -63f + Mathf.Sin(radians) * 164f);
                rect.sizeDelta = i == 4 ? new Vector2(18f, 25f) : new Vector2(13f, 19f);
                rect.localRotation = Quaternion.Euler(0f, 0f, degrees - 90f);
            }
            return root;
        }
    }
}
