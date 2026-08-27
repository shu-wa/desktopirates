using System;
using System.IO;
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
            bool preview = Array.Exists(Environment.GetCommandLineArgs(), argument => argument.EndsWith("-preview", StringComparison.Ordinal));
            var saves = preview
                ? new SaveSystem(Path.Combine(Application.temporaryCachePath, "desktopirates-preview.dprs"))
                : new SaveSystem();
            GameState state = preview ? new GameState() : saves.LoadOrNew();
            if (Array.Exists(Environment.GetCommandLineArgs(), argument => argument == "--compass-preview"))
                state.BossCompassOwned = true;
            if (Array.Exists(Environment.GetCommandLineArgs(), argument => argument == "--auto-preview" || argument == "--auto-sailing-preview"))
                state.BossCompassOwned = true;
            if (Array.Exists(Environment.GetCommandLineArgs(), argument => argument == "--inventory-preview"))
            {
                state.Food = 28;
                state.Water = 19;
                state.Supplies = 14;
                state.SpareCannons = 3;
                state.SetPartCount(SalvagePartKind.Timber, 26);
                state.SetPartCount(SalvagePartKind.Canvas, 12);
                state.SetPartCount(SalvagePartKind.Iron, 9);
                state.SetPartCount(SalvagePartKind.Gear, 6);
                state.SetPartCount(SalvagePartKind.Chart, 4);
                state.SetPartCount(SalvagePartKind.Relic, 2);
            }
            if (Array.Exists(Environment.GetCommandLineArgs(), argument => argument == "--log-preview"))
            {
                state.Captain.SetDistanceHundredths(184235);
                state.Captain.DamageDealt = 987654;
                state.Captain.GoldEarned = 543210;
                state.Captain.WrecksSalvaged = 128;
                state.Captain.TreasuresFound = 47;
                state.Captain.PortCalls = 63;
                for (int i = 0; i < EnemyArchetypeModel.Count; i++) state.Captain.SetEnemyCount((EnemyArchetype)i, (i + 1) * 7);
                for (int i = 1; i < BossMutationModel.BossCount; i++) state.Captain.SetBossCount((BossKind)i, i * 3);
                for (int i = 1; i < BossMutationModel.MutationCount; i++) state.Captain.SetMutationCount((BossMutation)i, i * 4);
                for (int i = 0; i < SeaRegionModel.Count; i++) state.Captain.DiscoverRegion((SeaRegionKind)i);
                for (int y = -4; y <= 4; y++)
                for (int x = -4; x <= 4; x++) state.ExploredChunks.Add(GameState.PackChunk(x, y));
            }
            if (Array.Exists(Environment.GetCommandLineArgs(), argument => argument == "--hud-max-preview"))
            {
                state.ShipLevel = ShipProgressionModel.TierCount - 1;
                state.MaxHull = ShipProgressionModel.GetMaxHull(state);
                state.Hull = state.MaxHull;
                state.Gold = 999999;
                state.Crew = ShipProgressionModel.Get(state.ShipLevel).MaxCrew;
                state.CapacityLevel = ShipCustomizationModel.GetUpgradeCap(state);
            }
            if (Array.Exists(Environment.GetCommandLineArgs(), argument => argument == "--shipyard-preview"))
            {
                state.ShipLevel = 3;
                state.MaxHull = ShipProgressionModel.GetMaxHull(state);
                state.Hull = state.MaxHull;
                state.Gold = 2400;
                state.Crew = 8;
                state.SpareCannons = 2;
                state.CannonMountMask = (1 << (int)CannonSlot.Bow)
                    | (1 << (int)CannonSlot.PortFore)
                    | (1 << (int)CannonSlot.StarboardAft);
                state.SetRoleCrew(CrewRole.Cannons, 3);
            }
            ProvisionController provisions = gameObject.AddComponent<ProvisionController>();
            provisions.Initialize(state, saves);
            Camera camera = CreateCamera();
            Light sun = CreateSun();
            CreateFillLight();

            var world = new GameObject("Circular Sea World").transform;
            world.SetParent(transform, false);

            var oceanObject = new GameObject("Ocean Disc", typeof(MeshFilter), typeof(MeshRenderer), typeof(OceanDisc));
            oceanObject.transform.SetParent(world, false);
            OceanDisc ocean = oceanObject.GetComponent<OceanDisc>();
            ocean.Initialize();

            Transform boatVisual = ProceduralSceneFactory.CreatePlayerBoat(world, state.ShipLevel);
            BoatController boat = gameObject.AddComponent<BoatController>();
            boat.Initialize(boatVisual, state);
            if (Array.Exists(Environment.GetCommandLineArgs(), argument => argument == "--wake-preview"))
            {
                boat.IncreaseCruiseStep();
                boat.IncreaseCruiseStep();
                boat.IncreaseCruiseStep();
            }
            var wakeObject = new GameObject("Recorded Ship Wake", typeof(ShipWakeTrailController));
            wakeObject.transform.SetParent(world, false);
            wakeObject.GetComponent<ShipWakeTrailController>().Initialize(boat);
            PlayerShipConditionController playerConditions = gameObject.AddComponent<PlayerShipConditionController>();
            playerConditions.Initialize(boat, state);
            ocean.Bind(boat);

            CameraRigController cameraRig = gameObject.AddComponent<CameraRigController>();
            cameraRig.Initialize(camera);

            CombatVfxController combatVfx = gameObject.AddComponent<CombatVfxController>();
            combatVfx.Initialize(world);

            var poiObject = new GameObject("Hidden Map POIs", typeof(PoiSystem));
            poiObject.transform.SetParent(world, false);
            PoiSystem poiSystem = poiObject.GetComponent<PoiSystem>();
            poiSystem.Initialize(boat, state, combatVfx, playerConditions);

            DayNightVisualController dayNight = gameObject.AddComponent<DayNightVisualController>();
            dayNight.Initialize(ocean, sun);

            RectTransform canvas = CreateCanvas();
            camera.gameObject.AddComponent<PixelWorldRenderer>();
            EnemyHudController enemyHud = gameObject.AddComponent<EnemyHudController>();
            enemyHud.Initialize(canvas, camera, poiSystem);
            InventoryController inventory = gameObject.AddComponent<InventoryController>();
            inventory.Initialize(canvas, state);
            MenuController menu = gameObject.AddComponent<MenuController>();
            menu.Initialize(canvas, overlay, state, saves, boat, poiSystem, inventory, null);
            AutoVoyageController autoVoyage = gameObject.AddComponent<AutoVoyageController>();
            autoVoyage.Initialize(canvas, state, boat, poiSystem, menu, saves);
            BossCompassController bossCompass = gameObject.AddComponent<BossCompassController>();
            bossCompass.Initialize(canvas, state, boat, cameraRig, inventory, menu);
            SeaRegionController seaRegions = gameObject.AddComponent<SeaRegionController>();
            seaRegions.Initialize(canvas, state, boat, ocean, inventory, menu);
            SpeedGaugeController speedGauge = gameObject.AddComponent<SpeedGaugeController>();
            speedGauge.Initialize(canvas, boat, inventory, menu);
            NotificationController notifications = gameObject.AddComponent<NotificationController>();
            notifications.Initialize(canvas, camera, poiSystem, menu);
            CreateMenuCircle(canvas, dayNight, overlay, menu);
            RuntimeScreenshotController screenshot = gameObject.AddComponent<RuntimeScreenshotController>();
            screenshot.TryStartFromCommandLine();

            var tagObject = new GameObject("Perimeter Tags", typeof(TagRingController));
            tagObject.transform.SetParent(canvas, false);
            tagObject.GetComponent<TagRingController>().Initialize(canvas, boat, cameraRig, poiSystem, inventory, menu);
            // Tags may occupy the same rim sector as the top dashboard. Render them
            // beneath authored HUD frames so landmark art never obscures vital values.
            tagObject.transform.SetAsFirstSibling();
        }

        private static Camera CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = TransparentKey;
            camera.orthographic = true;
            camera.orthographicSize = WorldPresentationMetrics.CameraOrthographicSize;
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
            var orbObject = new GameObject("Menu Circle", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(MenuCircleController));
            orbObject.transform.SetParent(canvas, false);
            RawImage orb = orbObject.GetComponent<RawImage>();
            orb.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            orb.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            orb.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            orb.rectTransform.anchoredPosition = new Vector2(0f, -72f);
            orb.rectTransform.sizeDelta = new Vector2(120f, 120f);
            orbObject.GetComponent<MenuCircleController>().Initialize(dayNight, overlay, menu);
        }

    }
}
