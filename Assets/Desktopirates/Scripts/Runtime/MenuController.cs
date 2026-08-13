using System;
using UnityEngine;
using UnityEngine.UI;

namespace Desktopirates
{
    public sealed class MenuController : MonoBehaviour
    {
        private static readonly Color Navy = new Color(0.018f, 0.055f, 0.095f, 1f);
        private static readonly Color Brass = new Color(0.88f, 0.58f, 0.17f, 1f);
        private RectTransform canvas;
        private WindowsOverlayController overlay;
        private GameState state;
        private SaveSystem saves;
        private BoatController boat;
        private PoiSystem pois;
        private InventoryController inventory;
        private GameObject compassRoot;
        private GameObject menuRoot;
        private GameObject mapRoot;
        private GameObject mapMarkersRoot;
        private RawImage mapImage;
        private Text mapStatus;
        private Text localZoomText;
        private Text wideZoomText;
        private Texture2D mapTexture;
        private MapZoom mapZoom = MapZoom.Local;
        private Vector2 mapChartCenter;
        private float nextMapLiveUpdate;
        private int mappedExploredChunkCount = -1;
        private RectTransform mapPlayerMarker;
        private RectTransform mapHeading;
        private GameObject portRoot;
        private GameObject shipyardRoot;
        private GameObject shipyardGunsPage;
        private GameObject shipyardSystemsPage;
        private GameObject hudRoot;
        private Text hullValue;
        private Text goldValue;
        private Text crewValue;
        private Text loadValue;
        private Image hullBar;
        private Image loadBar;
        private Text prompt;
        private Text toast;
        private Text engineUpgradeText;
        private Text shipyardSummary;
        private readonly Text[] cannonSlotTexts = new Text[ShipCustomizationModel.CannonSlotCount];
        private Text capacityUpgradeText;
        private Text propulsionUpgradeText;
        private Text armorUpgradeText;
        private Text turningUpgradeText;
        private Text gunUpgradeText;
        private Text crewHireText;
        private float toastUntil;
        private Font font;
        private Sprite panelSprite;
        private Sprite pillSprite;
        private Sprite diamondSprite;
        public bool IsModalOpen => (portRoot != null && portRoot.activeSelf) || (shipyardRoot != null && shipyardRoot.activeSelf) || mapRoot != null;

        public void Initialize(RectTransform canvasRoot, WindowsOverlayController windowOverlay, GameState gameState, SaveSystem saveSystem, BoatController player, PoiSystem poiSystem, InventoryController cargoInventory, GameObject compassDisplay)
        {
            canvas = canvasRoot;
            overlay = windowOverlay;
            state = gameState;
            saves = saveSystem;
            boat = player;
            pois = poiSystem;
            inventory = cargoInventory;
            compassRoot = compassDisplay;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            panelSprite = UiTextureFactory.LoadPanelSprite(128);
            pillSprite = UiTextureFactory.LoadPillSprite();
            diamondSprite = UiTextureFactory.LoadDiamondSprite();
            AudioListener.volume = PlayerPrefs.GetFloat("master_volume", 0.65f);
            BuildMenu();
            BuildHud();
            BuildPortPanel();
            BuildShipyardPanel();
            pois.Message += ShowMessage;
            pois.PortRequested += OpenPort;
            pois.StateChanged += Autosave;
            if (Array.Exists(Environment.GetCommandLineArgs(), argument => argument == "--shipyard-preview"))
            {
                OpenPort();
                OpenShipyard();
            }
            else if (Array.Exists(Environment.GetCommandLineArgs(), argument => argument == "--map-preview")) OpenMap();
        }

        private void Update()
        {
            hullValue.text = $"{state.Hull}/{state.MaxHull}";
            goldValue.text = $"{state.Gold} G";
            crewValue.text = state.Crew.ToString();
            float loadRatio = ShipCustomizationModel.GetLoadRatio(state);
            loadValue.text = $"{loadRatio * 100f:0}%";
            hullBar.fillAmount = state.Hull / (float)Mathf.Max(1, state.MaxHull);
            loadBar.fillAmount = Mathf.Clamp01(loadRatio);
            hullBar.color = state.Hull <= state.MaxHull * 0.3f ? UiTheme.Danger : UiTheme.Mint;
            loadBar.color = loadRatio >= 0.92f ? UiTheme.Danger : loadRatio >= 0.78f ? UiTheme.Warning : UiTheme.Brass;
            bool normalHud = (inventory == null || !inventory.IsOpen) && !IsModalOpen;
            hudRoot.SetActive(normalHud);
            inventory?.SetLauncherVisible(normalHud);
            if (compassRoot != null) compassRoot.SetActive(normalHud);
            prompt.text = pois.InteractionPrompt;
            prompt.transform.parent.gameObject.SetActive(normalHud && !string.IsNullOrEmpty(prompt.text));
            toast.transform.parent.gameObject.SetActive((inventory == null || !inventory.IsOpen) && Time.unscaledTime < toastUntil);
            if (mapRoot != null && Time.unscaledTime >= nextMapLiveUpdate) UpdateLiveMap();
            if (Input.GetKeyDown(KeyCode.M))
            {
                if (mapRoot != null) CloseMap();
                else { CloseAll(); OpenMap(); }
            }
            if (Input.GetKeyDown(KeyCode.Escape)) CloseAll();
        }

        private void OnApplicationPause(bool paused) { if (paused && state != null) saves.Save(state); }
        private void OnApplicationQuit() { if (state != null) saves.Save(state); }

        public void ToggleMenu()
        {
            bool open = !menuRoot.activeSelf;
            CloseAll();
            menuRoot.SetActive(open);
        }

        private void BuildMenu()
        {
            menuRoot = CreateUiObject("Menu Circle Radial Controls", canvas).gameObject;
            CreateButton(menuRoot.transform, "MAP", new Vector2(122f, -126f), () => { menuRoot.SetActive(false); OpenMap(); });
            CreateButton(menuRoot.transform, "BAG", new Vector2(196f, -160f), () => { menuRoot.SetActive(false); inventory.Toggle(); });
            CreateButton(menuRoot.transform, "SAVE", new Vector2(122f, -197f), () => { int bytes = saves.Save(state); ShowMessage($"VOYAGE SAVED  {bytes} bytes"); });
            CreateButton(menuRoot.transform, "EXIT", new Vector2(0f, -205f), () => { saves.Save(state); Application.Quit(); });
            CreateSlider(menuRoot.transform, "VOL", new Vector2(-132f, -126f), 0f, 1f, AudioListener.volume, value =>
            {
                AudioListener.volume = value;
                PlayerPrefs.SetFloat("master_volume", value);
            });
            CreateSlider(
                menuRoot.transform,
                "SIZE",
                new Vector2(-132f, -197f),
                0.72f,
                1.28f,
                overlay.WindowScale,
                value => overlay.SetWindowScale(value),
                true);
            menuRoot.SetActive(false);
        }

        private void BuildHud()
        {
            RectTransform dashboard = CreateUiObject("Ship Dashboard", canvas);
            dashboard.anchoredPosition = new Vector2(0f, -194f);
            dashboard.sizeDelta = new Vector2(408f, 48f);
            hudRoot = dashboard.gameObject;
            Image dashboardBack = dashboard.gameObject.AddComponent<Image>(); dashboardBack.sprite = pillSprite; dashboardBack.type = Image.Type.Sliced; dashboardBack.color = Color.white; dashboardBack.raycastTarget = false;
            hullValue = CreateStatusCard(dashboard, "HULL", new Vector2(-150f, 0f), UiTheme.Mint, out hullBar);
            goldValue = CreateStatusCard(dashboard, "GOLD", new Vector2(-50f, 0f), UiTheme.Brass, out _);
            crewValue = CreateStatusCard(dashboard, "CREW", new Vector2(50f, 0f), UiTheme.SecondaryText, out _);
            loadValue = CreateStatusCard(dashboard, "LOAD", new Vector2(150f, 0f), UiTheme.Brass, out loadBar);

            prompt = CreatePillText(canvas, "Context Action", new Vector2(0f, -646f), new Vector2(390f, 34f), 17);
            prompt.color = UiTheme.PrimaryText;
            UiTheme.StyleText(prompt, 18);
            toast = CreatePillText(canvas, "Event Message", new Vector2(0f, -365f), new Vector2(430f, 40f), 18);
            toast.color = UiTheme.Brass;
            UiTheme.StyleText(toast, 18);
            toast.transform.parent.gameObject.SetActive(false);
        }

        private Text CreateStatusCard(Transform parent, string label, Vector2 position, Color accent, out Image meter)
        {
            RectTransform card = CreateUiObject(label + " Status Card", parent); card.anchoredPosition = position; card.sizeDelta = new Vector2(94f, 40f);
            Image background = card.gameObject.AddComponent<Image>(); background.sprite = pillSprite; background.type = Image.Type.Sliced; background.color = new Color(0.72f, 0.82f, 0.80f, 0.96f); background.raycastTarget = false;
            Text caption = CreateText(card, label, new Vector2(0f, 9f), new Vector2(84f, 15f), 13, TextAnchor.MiddleCenter); caption.color = accent; UiTheme.StyleText(caption, 13);
            Text value = CreateText(card, label + " Value", new Vector2(0f, -7f), new Vector2(84f, 20f), 16, TextAnchor.MiddleCenter); value.color = UiTheme.PrimaryText; UiTheme.StyleText(value, 16);
            RectTransform bar = CreateUiObject(label + " Meter", card); bar.anchoredPosition = new Vector2(0f, -17f); bar.sizeDelta = new Vector2(68f, 3f);
            Image track = bar.gameObject.AddComponent<Image>(); track.color = new Color(0.04f, 0.12f, 0.15f, 1f); track.raycastTarget = false;
            RectTransform fill = CreateUiObject(label + " Meter Fill", bar); fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one; fill.offsetMin = fill.offsetMax = Vector2.zero;
            meter = fill.gameObject.AddComponent<Image>(); meter.type = Image.Type.Filled; meter.fillMethod = Image.FillMethod.Horizontal; meter.color = accent; meter.raycastTarget = false;
            if (label != "HULL" && label != "LOAD") bar.gameObject.SetActive(false);
            return value;
        }

        private void BuildPortPanel()
        {
            portRoot = CreateUiObject("Harbor Services", canvas).gameObject;
            Image back = portRoot.AddComponent<Image>();
            back.sprite = panelSprite;
            back.color = Color.white;
            RectTransform rect = (RectTransform)portRoot.transform;
            rect.anchoredPosition = new Vector2(0f, -390f);
            rect.sizeDelta = new Vector2(330f, 330f);
            UiTheme.AddChartWoodSurface(portRoot.transform, new Vector2(282f, 282f), 0.55f, panelSprite);
            CreateText(portRoot.transform, "PORT", new Vector2(0f, 112f), new Vector2(220f, 30f), 22, TextAnchor.MiddleCenter).color = Brass;
            CreateWideButton(portRoot.transform, "REPAIR  8G / HULL", new Vector2(0f, 60f), Repair);
            CreateWideButton(portRoot.transform, "SUPPLIES +5  20G", new Vector2(0f, 15f), BuySupplies);
            Button engineButton = CreateWideButton(portRoot.transform, "ENGINE +MAX SPEED", new Vector2(0f, -30f), UpgradeEngine);
            engineUpgradeText = engineButton.GetComponentInChildren<Text>();
            CreateWideButton(portRoot.transform, "SHIPYARD / CREW", new Vector2(0f, -75f), OpenShipyard);
            CreateButton(portRoot.transform, "SAIL", new Vector2(0f, -126f), () => { portRoot.SetActive(false); saves.Save(state); });
            portRoot.SetActive(false);
        }

        private void BuildShipyardPanel()
        {
            shipyardRoot = CreateUiObject("Shipyard Customization", canvas).gameObject;
            Image back = shipyardRoot.AddComponent<Image>(); back.sprite = panelSprite; back.color = Color.white;
            RectTransform rect = (RectTransform)shipyardRoot.transform;
            rect.anchoredPosition = new Vector2(0f, -390f);
            rect.sizeDelta = new Vector2(520f, 500f);
            UiTheme.AddChartWoodSurface(shipyardRoot.transform, new Vector2(460f, 438f), 0.58f, panelSprite);
            CreateText(shipyardRoot.transform, "SHIPYARD", new Vector2(0f, 214f), new Vector2(260f, 30f), 22, TextAnchor.MiddleCenter).color = Brass;
            shipyardSummary = CreateText(shipyardRoot.transform, "Shipyard Summary", new Vector2(0f, 178f), new Vector2(470f, 36f), 12, TextAnchor.MiddleCenter);
            shipyardSummary.color = new Color(0.78f, 0.91f, 0.90f, 1f);

            CreateSizedButton(shipyardRoot.transform, "GUN DECK", new Vector2(-112f, 132f), new Vector2(210f, 38f), () => ShowShipyardPage(true));
            CreateSizedButton(shipyardRoot.transform, "SHIP SYSTEMS", new Vector2(112f, 132f), new Vector2(210f, 38f), () => ShowShipyardPage(false));

            shipyardGunsPage = CreateUiObject("Gun Deck Page", shipyardRoot.transform).gameObject;
            CreateText(shipyardGunsPage.transform, "CLICK A HARDPOINT TO INSTALL OR STORE", new Vector2(0f, 92f), new Vector2(430f, 24f), 15, TextAnchor.MiddleCenter).color = UiTheme.SecondaryText;
            CreateCannonSlotButton(CannonSlot.Bow, new Vector2(0f, 58f));
            CreateCannonSlotButton(CannonSlot.PortFore, new Vector2(-125f, 18f));
            CreateCannonSlotButton(CannonSlot.StarboardFore, new Vector2(125f, 18f));
            CreateCannonSlotButton(CannonSlot.PortAft, new Vector2(-125f, -24f));
            CreateCannonSlotButton(CannonSlot.StarboardAft, new Vector2(125f, -24f));
            CreateCannonSlotButton(CannonSlot.Stern, new Vector2(0f, -66f));

            shipyardSystemsPage = CreateUiObject("Ship Systems Page", shipyardRoot.transform).gameObject;
            capacityUpgradeText = CreateCompactButton(shipyardSystemsPage.transform, "CAPACITY", new Vector2(-120f, 72f), UpgradeCapacity);
            propulsionUpgradeText = CreateCompactButton(shipyardSystemsPage.transform, "PROPULSION", new Vector2(120f, 72f), UpgradeEngine);
            armorUpgradeText = CreateCompactButton(shipyardSystemsPage.transform, "ARMOR", new Vector2(-120f, 20f), UpgradeArmor);
            turningUpgradeText = CreateCompactButton(shipyardSystemsPage.transform, "TURNING", new Vector2(120f, 20f), UpgradeTurning);
            gunUpgradeText = CreateCompactButton(shipyardSystemsPage.transform, "GUNS", new Vector2(-120f, -32f), UpgradeCannon);
            crewHireText = CreateCompactButton(shipyardSystemsPage.transform, "HIRE CREW", new Vector2(120f, -32f), HireCrew);
            CreateCompactButton(shipyardRoot.transform, "BACK TO PORT", new Vector2(-120f, -188f), () => { shipyardRoot.SetActive(false); portRoot.SetActive(true); });
            CreateCompactButton(shipyardRoot.transform, "SAIL", new Vector2(120f, -188f), () => { shipyardRoot.SetActive(false); saves.Save(state); });
            shipyardRoot.SetActive(false);
            ShowShipyardPage(true);
        }

        private void CreateCannonSlotButton(CannonSlot slot, Vector2 position)
        {
            Button button = CreateSizedButton(shipyardGunsPage.transform, ShipCustomizationModel.GetDefinition(slot).ShortName, position, new Vector2(210f, 38f), () => ToggleCannon(slot));
            cannonSlotTexts[(int)slot] = button.GetComponentInChildren<Text>();
        }

        private Text CreateCompactButton(Transform parent, string label, Vector2 position, Action action)
            => CreateSizedButton(parent, label, position, new Vector2(226f, 42f), action).GetComponentInChildren<Text>();

        private void ShowShipyardPage(bool guns)
        {
            if (shipyardGunsPage != null) shipyardGunsPage.SetActive(guns);
            if (shipyardSystemsPage != null) shipyardSystemsPage.SetActive(!guns);
        }

        private void OpenPort()
        {
            CloseAll();
            RefreshEngineUpgradeText();
            portRoot.SetActive(true);
            saves.Save(state);
        }

        private void OpenShipyard()
        {
            portRoot.SetActive(false);
            RefreshShipyard();
            shipyardRoot.SetActive(true);
        }

        private void ToggleCannon(CannonSlot slot)
        {
            int bit = 1 << (int)slot;
            if (ShipCustomizationModel.HasCannon(state, slot))
            {
                state.CannonMountMask &= ~bit;
                state.SpareCannons++;
                ShowMessage($"{ShipCustomizationModel.GetDefinition(slot).ShortName} CANNON STORED");
            }
            else
            {
                if (!ShipCustomizationModel.CanAddMass(state, ShipCustomizationModel.CannonMass)) { ShowMessage("CAPACITY EXCEEDED — UPGRADE THE HULL"); return; }
                if (state.SpareCannons > 0) state.SpareCannons--;
                else
                {
                    if (state.Gold < ShipCustomizationModel.CannonPrice) { ShowMessage($"CANNON NEEDS {ShipCustomizationModel.CannonPrice}G"); return; }
                    state.Gold -= ShipCustomizationModel.CannonPrice;
                }
                state.CannonMountMask |= bit;
                ShowMessage($"CANNON MOUNTED — {ShipCustomizationModel.GetDefinition(slot).ArcName} ARC");
            }
            boat.RefreshCustomizationVisual();
            RefreshShipyard();
            saves.Save(state);
        }

        private void UpgradeCapacity()
        {
            int cost = ShipCustomizationModel.GetCapacityUpgradeCost(state.CapacityLevel);
            if (!CanBuyUpgrade(state.CapacityLevel, cost, "CAPACITY")) return;
            state.Gold -= cost; state.CapacityLevel++; RefreshShipyard(); saves.Save(state);
            ShowMessage($"LOAD LIMIT {ShipCustomizationModel.GetCapacity(state):0.0}");
        }

        private void UpgradeArmor()
        {
            int cost = ShipCustomizationModel.GetArmorUpgradeCost(state.ArmorLevel);
            if (!CanBuyUpgrade(state.ArmorLevel, cost, "ARMOR")) return;
            if (!ShipCustomizationModel.CanAddMass(state, 3.4f)) { ShowMessage("CAPACITY EXCEEDED — UPGRADE THE HULL"); return; }
            state.Gold -= cost; state.ArmorLevel++; state.MaxHull += 3; state.Hull += 3;
            boat.RefreshCustomizationVisual(); RefreshShipyard(); saves.Save(state); ShowMessage($"ARMOR {state.ArmorLevel}  HULL +3");
        }

        private void UpgradeTurning()
        {
            int cost = ShipCustomizationModel.GetTurningUpgradeCost(state.TurningLevel);
            if (!CanBuyUpgrade(state.TurningLevel, cost, "TURNING")) return;
            if (!ShipCustomizationModel.CanAddMass(state, 0.75f)) { ShowMessage("CAPACITY EXCEEDED — UPGRADE THE HULL"); return; }
            state.Gold -= cost; state.TurningLevel++; RefreshShipyard(); saves.Save(state); ShowMessage("RUDDER RESPONSE IMPROVED");
        }

        private void HireCrew()
        {
            int cost = ShipCustomizationModel.GetCrewHireCost(state);
            if (!ShipCustomizationModel.CanAddMass(state, ShipCustomizationModel.CrewMass)) { ShowMessage("NO BERTH CAPACITY"); return; }
            if (state.Gold < cost) { ShowMessage($"CREW NEEDS {cost}G"); return; }
            state.Gold -= cost; state.Crew++; RefreshShipyard(); saves.Save(state); ShowMessage($"CREW ABOARD  {state.Crew}");
        }

        private bool CanBuyUpgrade(int level, int cost, string name)
        {
            if (level >= ShipCustomizationModel.MaxUpgradeLevel) { ShowMessage($"{name} AT MAX LEVEL"); return false; }
            if (state.Gold < cost) { ShowMessage($"{name} NEEDS {cost}G"); return false; }
            return true;
        }

        private void RefreshShipyard()
        {
            float mass = ShipCustomizationModel.GetMass(state);
            float capacity = ShipCustomizationModel.GetCapacity(state);
            shipyardSummary.text = $"MASS {mass:0.0}/{capacity:0.0}   SPEED {ShipCustomizationModel.GetSpeedMultiplier(state) * 100f:0}%   TURN {ShipCustomizationModel.GetTurningMultiplier(state) * 100f:0}%\nCREW {state.Crew}   GUNS {ShipCustomizationModel.GetInstalledCannonCount(state)}   STORAGE {state.SpareCannons}";
            for (int i = 0; i < cannonSlotTexts.Length; i++)
            {
                CannonSlot slot = (CannonSlot)i;
                CannonSlotDefinition definition = ShipCustomizationModel.GetDefinition(slot);
                cannonSlotTexts[i].text = $"{definition.ShortName}  {(ShipCustomizationModel.HasCannon(state, slot) ? "● ARMED" : "+ EMPTY")}";
                cannonSlotTexts[i].color = ShipCustomizationModel.HasCannon(state, slot) ? new Color(1f, 0.76f, 0.28f) : new Color(0.62f, 0.76f, 0.76f);
            }
            capacityUpgradeText.text = UpgradeLabel("CAPACITY", state.CapacityLevel, ShipCustomizationModel.GetCapacityUpgradeCost(state.CapacityLevel));
            propulsionUpgradeText.text = UpgradeLabel("PROPULSION", state.EngineLevel, CruiseModel.GetUpgradeCost(state.EngineLevel));
            armorUpgradeText.text = UpgradeLabel("ARMOR", state.ArmorLevel, ShipCustomizationModel.GetArmorUpgradeCost(state.ArmorLevel));
            turningUpgradeText.text = UpgradeLabel("TURNING", state.TurningLevel, ShipCustomizationModel.GetTurningUpgradeCost(state.TurningLevel));
            gunUpgradeText.text = UpgradeLabel("GUN DAMAGE", state.CannonLevel, ShipCustomizationModel.GetGunUpgradeCost(state.CannonLevel));
            crewHireText.text = $"HIRE CREW  {ShipCustomizationModel.GetCrewHireCost(state)}G";
        }

        private static string UpgradeLabel(string name, int level, int cost)
            => level >= ShipCustomizationModel.MaxUpgradeLevel ? $"{name}  MAX" : $"{name}  L{level}→{level + 1}  {cost}G";

        private void Repair()
        {
            int missing = state.MaxHull - state.Hull;
            int affordable = Mathf.Min(missing, state.Gold / 8);
            if (affordable <= 0) { ShowMessage(missing == 0 ? "HULL ALREADY FULL" : "NOT ENOUGH GOLD"); return; }
            state.Gold -= affordable * 8;
            state.Hull += affordable;
            ShowMessage($"REPAIRED +{affordable}");
            saves.Save(state);
        }

        private void BuySupplies()
        {
            if (state.Gold < 20) { ShowMessage("NOT ENOUGH GOLD"); return; }
            state.Gold -= 20; state.Supplies += 5; ShowMessage("SUPPLIES +5"); saves.Save(state);
        }

        private void UpgradeEngine()
        {
            if (state.EngineLevel >= CruiseModel.MaxEngineLevel) { ShowMessage("ENGINE AT MAX LEVEL"); return; }
            int cost = CruiseModel.GetUpgradeCost(state.EngineLevel);
            if (state.Gold < cost) { ShowMessage($"ENGINE NEEDS {cost}G"); return; }
            if (!ShipCustomizationModel.CanAddMass(state, 1.25f)) { ShowMessage("CAPACITY EXCEEDED — UPGRADE THE HULL"); return; }
            state.Gold -= cost;
            state.EngineLevel++;
            RefreshEngineUpgradeText();
            if (shipyardRoot != null) RefreshShipyard();
            ShowMessage($"MAX SPEED {CruiseModel.GetMaxSpeed(state.EngineLevel):0.00}  {CruiseModel.GetMaxStep(state.EngineLevel)} STEPS");
            saves.Save(state);
        }

        private void RefreshEngineUpgradeText()
        {
            if (engineUpgradeText == null) return;
            engineUpgradeText.text = state.EngineLevel >= CruiseModel.MaxEngineLevel
                ? $"ENGINE MAX  SPD {CruiseModel.GetMaxSpeed(state.EngineLevel):0.00}"
                : $"ENGINE +MAX SPD  {CruiseModel.GetUpgradeCost(state.EngineLevel)}G";
        }

        private void UpgradeCannon()
        {
            int cost = ShipCustomizationModel.GetGunUpgradeCost(state.CannonLevel);
            if (!CanBuyUpgrade(state.CannonLevel, cost, "GUN DAMAGE")) return;
            state.Gold -= cost; state.CannonLevel++; RefreshShipyard(); ShowMessage($"GUN DAMAGE {state.CannonLevel + 1} PER CANNON"); saves.Save(state);
        }

        private void OpenMap()
        {
            if (mapRoot != null) return;
            mapChartCenter = boat.LogicalPosition;
            RectTransform panel = CreateUiObject("Exploration Chart", canvas);
            mapRoot = panel.gameObject;
            panel.anchoredPosition = new Vector2(0f, -405f);
            panel.sizeDelta = new Vector2(550f, 550f);
            Image panelBack = panel.gameObject.AddComponent<Image>(); panelBack.sprite = panelSprite; panelBack.color = Color.white; panelBack.raycastTarget = false;
            UiTheme.AddChartWoodSurface(panel, new Vector2(510f, 510f), 0.58f, panelSprite);

            Text title = CreateText(panel, "EXPLORATION CHART", new Vector2(0f, 246f), new Vector2(320f, 30f), 22, TextAnchor.MiddleCenter);
            title.color = UiTheme.Brass; UiTheme.StyleText(title, 22);
            localZoomText = CreateSizedButton(panel, "LOCAL", new Vector2(-74f, 211f), new Vector2(136f, 38f), () => SetMapZoom(MapZoom.Local)).GetComponentInChildren<Text>();
            wideZoomText = CreateSizedButton(panel, "WIDE", new Vector2(74f, 211f), new Vector2(136f, 38f), () => SetMapZoom(MapZoom.Wide)).GetComponentInChildren<Text>();

            RectTransform frameRect = CreateUiObject("Chart Brass Bezel", panel); frameRect.anchoredPosition = new Vector2(0f, -2f); frameRect.sizeDelta = new Vector2(438f, 438f);
            Image frame = frameRect.gameObject.AddComponent<Image>(); frame.sprite = panelSprite; frame.color = Color.white; frame.raycastTarget = false;
            RectTransform clip = CreateUiObject("Chart Circular Viewport", panel); clip.anchoredPosition = new Vector2(0f, -2f); clip.sizeDelta = new Vector2(408f, 408f);
            Image clipGraphic = clip.gameObject.AddComponent<Image>(); clipGraphic.sprite = panelSprite; clipGraphic.color = Color.white; clipGraphic.raycastTarget = false;
            Mask mask = clip.gameObject.AddComponent<Mask>(); mask.showMaskGraphic = false;
            RectTransform mapRect = CreateUiObject("Explored Waters", clip); mapRect.sizeDelta = new Vector2(398f, 398f);
            mapImage = mapRect.gameObject.AddComponent<RawImage>(); mapImage.raycastTarget = false;
            RectTransform markersRect = CreateUiObject("Chart Markers", clip);
            markersRect.sizeDelta = new Vector2(398f, 398f);
            mapMarkersRoot = markersRect.gameObject;

            CreateCardinalLabel(panel, "N", new Vector2(0f, 184f));
            CreateCardinalLabel(panel, "E", new Vector2(208f, -2f));
            CreateCardinalLabel(panel, "S", new Vector2(0f, -188f));
            CreateCardinalLabel(panel, "W", new Vector2(-208f, -2f));
            CreateMapStrip(panel, "Chart Position Strip", new Vector2(0f, -222f), new Vector2(390f, 27f));
            mapStatus = CreateText(panel, "Chart Status", new Vector2(0f, -222f), new Vector2(382f, 24f), 15, TextAnchor.MiddleCenter);
            mapStatus.color = UiTheme.SecondaryText; UiTheme.StyleText(mapStatus, 15);
            BuildMapLegend(panel);
            CreateButton(panel, "BACK", new Vector2(-232f, 224f), CloseMap);
            SetMapZoom(mapZoom);
        }

        private void SetMapZoom(MapZoom zoom)
        {
            mapZoom = zoom;
            if (mapImage == null) return;
            mapChartCenter = boat.LogicalPosition;
            RefreshMapTexture();
            localZoomText.color = zoom == MapZoom.Local ? UiTheme.Brass : UiTheme.SecondaryText;
            wideZoomText.color = zoom == MapZoom.Wide ? UiTheme.Brass : UiTheme.SecondaryText;
            RebuildMapMarkers();
            UpdateMapStatus();
        }

        private void RefreshMapTexture()
        {
            if (mapTexture != null) Destroy(mapTexture);
            mapTexture = MapChartModel.CreateTexture(state, mapChartCenter, mapZoom);
            mapImage.texture = mapTexture;
            mappedExploredChunkCount = state.ExploredChunks.Count;
        }

        private void UpdateLiveMap()
        {
            nextMapLiveUpdate = Time.unscaledTime + 0.12f;
            bool recentered = MapChartModel.ShouldRecenter(boat.LogicalPosition, mapChartCenter, mapZoom);
            if (recentered) mapChartCenter = boat.LogicalPosition;
            if (recentered || mappedExploredChunkCount != state.ExploredChunks.Count)
            {
                RefreshMapTexture();
                RebuildMapMarkers();
            }
            else
            {
                UpdateMapPlayerMarker();
            }
            UpdateMapStatus();
        }

        private void UpdateMapStatus()
        {
            if (mapStatus == null) return;
            int chunkX = Mathf.FloorToInt(boat.LogicalPosition.x / WorldGenerator.ChunkSize);
            int chunkY = Mathf.FloorToInt(boat.LogicalPosition.y / WorldGenerator.ChunkSize);
            mapStatus.text = $"LIVE  {(mapZoom == MapZoom.Local ? "LOCAL 4.5" : "WIDE 9.5")}   POS {chunkX:+0;-0;0},{chunkY:+0;-0;0}   HDG {boat.HeadingDegrees:000}°   {SpeedGaugeModel.GetMotionLabel(boat.CruiseStep, boat.MaxCruiseStep, boat.Speed, boat.TargetSpeed)}";
        }

        private void RebuildMapMarkers()
        {
            if (mapMarkersRoot == null) return;
            foreach (Transform child in mapMarkersRoot.transform) Destroy(child.gameObject);
            float radius = MapChartModel.GetWorldRadius(mapZoom);
            int minX = Mathf.FloorToInt((mapChartCenter.x - radius) / WorldGenerator.ChunkSize);
            int maxX = Mathf.FloorToInt((mapChartCenter.x + radius) / WorldGenerator.ChunkSize);
            int minY = Mathf.FloorToInt((mapChartCenter.y - radius) / WorldGenerator.ChunkSize);
            int maxY = Mathf.FloorToInt((mapChartCenter.y + radius) / WorldGenerator.ChunkSize);
            var events = new System.Collections.Generic.List<GeneratedEventData>();
            for (int chunkY = minY; chunkY <= maxY; chunkY++)
            for (int chunkX = minX; chunkX <= maxX; chunkX++)
            {
                if (!state.ExploredChunks.Contains(GameState.PackChunk(chunkX, chunkY))) continue;
                WorldGenerator.GenerateChunk(state.WorldSeed, chunkX, chunkY, events);
                foreach (GeneratedEventData data in events)
                {
                    if (state.ResolvedEvents.Contains(data.Id)) continue;
                    Vector2 position = MapChartModel.WorldToMap(data.Position, mapChartCenter, mapZoom);
                    if (!MapChartModel.IsInside(position)) continue;
                    RectTransform marker = CreateUiObject($"Known {data.Kind}", mapMarkersRoot.transform);
                    marker.anchoredPosition = position; marker.sizeDelta = Vector2.one * (mapZoom == MapZoom.Local ? 36f : 28f);
                    RawImage image = marker.gameObject.AddComponent<RawImage>(); image.texture = UiTextureFactory.LoadPoiBadge(data.Kind); image.raycastTarget = false;
                }
            }

            mapHeading = CreateUiObject("Ship Heading", mapMarkersRoot.transform); mapHeading.sizeDelta = new Vector2(5f, 48f); mapHeading.pivot = new Vector2(0.5f, 0f);
            Image course = mapHeading.gameObject.AddComponent<Image>(); course.color = UiTheme.Brass; course.raycastTarget = false;
            mapPlayerMarker = CreateUiObject("Current Ship Position", mapMarkersRoot.transform); mapPlayerMarker.sizeDelta = new Vector2(44f, 44f);
            RawImage playerImage = mapPlayerMarker.gameObject.AddComponent<RawImage>(); playerImage.texture = UiTextureFactory.LoadCompassArrow(); playerImage.color = UiTheme.PrimaryText; playerImage.raycastTarget = false;
            UpdateMapPlayerMarker();
        }

        private void UpdateMapPlayerMarker()
        {
            if (mapPlayerMarker == null || mapHeading == null) return;
            Vector2 position = MapChartModel.WorldToMap(boat.LogicalPosition, mapChartCenter, mapZoom);
            mapPlayerMarker.anchoredPosition = position;
            mapHeading.anchoredPosition = position;
            mapPlayerMarker.localRotation = Quaternion.Euler(0f, 0f, -boat.HeadingDegrees);
            mapHeading.localRotation = Quaternion.Euler(0f, 0f, -boat.HeadingDegrees);
        }

        private void CreateCardinalLabel(Transform parent, string label, Vector2 position)
        {
            Text text = CreateText(parent, label, position, new Vector2(34f, 30f), 18, TextAnchor.MiddleCenter);
            text.color = label == "N" ? UiTheme.Brass : UiTheme.PrimaryText; UiTheme.StyleText(text, 18);
        }

        private void BuildMapLegend(Transform parent)
        {
            CreateMapStrip(parent, "Chart Legend Strip", new Vector2(0f, -252f), new Vector2(420f, 29f));
            PoiKind[] kinds = { PoiKind.Port, PoiKind.Enemy, PoiKind.Wreck, PoiKind.Treasure };
            string[] labels = { "PORT", "ENEMY", "WRECK", "TREASURE" };
            for (int i = 0; i < kinds.Length; i++)
            {
                float x = -150f + i * 100f;
                RectTransform icon = CreateUiObject(labels[i] + " Legend Icon", parent); icon.anchoredPosition = new Vector2(x - 26f, -252f); icon.sizeDelta = new Vector2(24f, 24f);
                RawImage badge = icon.gameObject.AddComponent<RawImage>(); badge.texture = UiTextureFactory.LoadPoiBadge(kinds[i]); badge.raycastTarget = false;
                Text label = CreateText(parent, labels[i], new Vector2(x + 12f, -252f), new Vector2(70f, 22f), 13, TextAnchor.MiddleLeft);
                label.color = UiTheme.PrimaryText; UiTheme.StyleText(label, 13);
            }
        }

        private void CreateMapStrip(Transform parent, string name, Vector2 position, Vector2 size)
        {
            RectTransform strip = CreateUiObject(name, parent); strip.anchoredPosition = position; strip.sizeDelta = size;
            Image background = strip.gameObject.AddComponent<Image>(); background.sprite = pillSprite; background.type = Image.Type.Sliced;
            background.color = new Color(0.015f, 0.045f, 0.070f, 0.94f); background.raycastTarget = false;
        }

        private void CloseMap()
        {
            if (mapTexture != null) { Destroy(mapTexture); mapTexture = null; }
            if (mapRoot != null) { Destroy(mapRoot); mapRoot = null; }
            mapImage = null; mapMarkersRoot = null; mapStatus = null; localZoomText = null; wideZoomText = null;
            mapPlayerMarker = null; mapHeading = null; mappedExploredChunkCount = -1;
        }

        private void CloseAll()
        {
            menuRoot.SetActive(false);
            portRoot.SetActive(false);
            shipyardRoot.SetActive(false);
            inventory?.Close();
            CloseMap();
        }

        private void Autosave() => saves.Save(state);
        private void ShowMessage(string value) { toast.text = value; toastUntil = Time.unscaledTime + 2.8f; toast.transform.parent.gameObject.SetActive(true); }

        private Button CreateButton(Transform parent, string label, Vector2 position, Action action)
        {
            RectTransform rect = CreateUiObject(label, parent);
            rect.anchoredPosition = position; rect.sizeDelta = new Vector2(72f, 72f);
            MenuGlyph glyph = label == "SAVE" ? MenuGlyph.Save
                : label == "BAG" ? MenuGlyph.Inventory
                : label == "BACK" ? MenuGlyph.Back
                : label == "EXIT" || label == "SAIL" ? MenuGlyph.Exit
                : MenuGlyph.Map;
            RawImage image = rect.gameObject.AddComponent<RawImage>(); image.texture = UiTextureFactory.LoadMenuButton(glyph, 80); image.color = Color.white;
            Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(() => action());
            return button;
        }

        private Button CreateWideButton(Transform parent, string label, Vector2 position, Action action)
        {
            RectTransform rect = CreateUiObject(label, parent); rect.anchoredPosition = position; rect.sizeDelta = new Vector2(225f, 34f);
            Image image = rect.gameObject.AddComponent<Image>(); image.sprite = pillSprite; image.type = Image.Type.Sliced; image.color = Color.white;
            Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(() => action());
            Text text = CreateText(rect, label, Vector2.zero, rect.sizeDelta, 15, TextAnchor.MiddleCenter); text.color = UiTheme.PrimaryText; UiTheme.StyleText(text, 15);
            return button;
        }

        private Button CreateSizedButton(Transform parent, string label, Vector2 position, Vector2 size, Action action)
        {
            RectTransform rect = CreateUiObject(label, parent); rect.anchoredPosition = position; rect.sizeDelta = size;
            Image image = rect.gameObject.AddComponent<Image>(); image.sprite = pillSprite; image.type = Image.Type.Sliced; image.color = Color.white;
            Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(() => action());
            Text text = CreateText(rect, label, Vector2.zero, size - new Vector2(8f, 0f), 15, TextAnchor.MiddleCenter); text.color = UiTheme.PrimaryText; UiTheme.StyleText(text, 15);
            return button;
        }

        private void CreateSlider(Transform parent, string label, Vector2 position, float min, float max, float value, Action<float> changed, bool commitOnRelease = false)
        {
            RectTransform holder = CreateUiObject(label, parent); holder.anchoredPosition = position; holder.sizeDelta = new Vector2(152f, 48f);
            Image holderBackground = holder.gameObject.AddComponent<Image>(); holderBackground.sprite = pillSprite; holderBackground.type = Image.Type.Sliced; holderBackground.color = Color.white;
            RectTransform glyphRect = CreateUiObject(label + " Icon", holder); glyphRect.anchoredPosition = new Vector2(-55f, 0f); glyphRect.sizeDelta = new Vector2(28f, 28f);
            RawImage glyphImage = glyphRect.gameObject.AddComponent<RawImage>(); glyphImage.texture = UiTextureFactory.LoadGlyph(label == "VOL" ? MenuGlyph.Volume : MenuGlyph.Size, 32); glyphImage.raycastTarget = false;
            RectTransform bar = CreateUiObject("Bar", holder); bar.anchoredPosition = new Vector2(27f, commitOnRelease ? 6f : 0f); bar.sizeDelta = new Vector2(82f, 8f);
            Image background = bar.gameObject.AddComponent<Image>(); background.color = new Color(0.10f, 0.21f, 0.26f, 1f);
            Slider slider = bar.gameObject.AddComponent<Slider>(); slider.minValue = min; slider.maxValue = max; slider.value = value;
            RectTransform fill = CreateUiObject("Fill", bar); fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one; fill.offsetMin = Vector2.zero; fill.offsetMax = Vector2.zero;
            Image fillImage = fill.gameObject.AddComponent<Image>(); fillImage.color = Brass; slider.fillRect = fill;
            RectTransform handle = CreateUiObject("Handle", bar); handle.sizeDelta = new Vector2(16f, 16f);
            Image handleImage = handle.gameObject.AddComponent<Image>(); handleImage.sprite = diamondSprite; handleImage.color = Color.white; slider.handleRect = handle; slider.targetGraphic = handleImage;
            if (commitOnRelease)
            {
                Text preview = CreateText(holder, "Size Preview", new Vector2(27f, -13f), new Vector2(82f, 14f), 10, TextAnchor.MiddleCenter);
                preview.color = new Color(0.96f, 0.77f, 0.34f, 0.95f);
                DeferredSliderCommit deferred = bar.gameObject.AddComponent<DeferredSliderCommit>();
                deferred.Initialize(slider, v => preview.text = $"{Mathf.RoundToInt(v * 100f)}%  RELEASE", changed);
            }
            else
            {
                slider.onValueChanged.AddListener(v => changed(v));
            }
        }

        private Text CreateText(Transform parent, string name, Vector2 position, Vector2 size, int fontSize, TextAnchor anchor)
        {
            RectTransform rect = CreateUiObject(name, parent); rect.anchoredPosition = position; rect.sizeDelta = size;
            Text text = rect.gameObject.AddComponent<Text>(); text.font = font; text.fontSize = fontSize; text.alignment = anchor; text.text = name; text.raycastTarget = false;
            return text;
        }

        private Text CreatePillText(Transform parent, string name, Vector2 position, Vector2 size, int fontSize)
        {
            RectTransform pill = CreateUiObject(name + " Pill", parent); pill.anchoredPosition = position; pill.sizeDelta = size;
            Image background = pill.gameObject.AddComponent<Image>(); background.sprite = pillSprite; background.type = Image.Type.Sliced; background.color = Color.white; background.raycastTarget = false;
            Text text = CreateText(pill, name, Vector2.zero, size - new Vector2(12f, 0f), fontSize, TextAnchor.MiddleCenter);
            return text;
        }

        private static RectTransform CreateUiObject(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform)); gameObject.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)gameObject.transform;
            RectTransform parentRect = parent as RectTransform;
            bool parentHasArea = parentRect != null && (parentRect.sizeDelta.x > 1f || parentRect.sizeDelta.y > 1f) && parent.GetComponent<Canvas>() == null;
            Vector2 anchor = parentHasArea ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 1f);
            rect.anchorMin = anchor; rect.anchorMax = anchor; rect.pivot = new Vector2(0.5f, 0.5f); rect.anchoredPosition = Vector2.zero;
            return rect;
        }

    }
}
