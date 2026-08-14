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
        private int mappedResolvedEventCount = -1;
        private RectTransform mapPlayerMarker;
        private RectTransform mapHeading;
        private GameObject portRoot;
        private GameObject shipyardRoot;
        private GameObject shipyardGunsPage;
        private GameObject shipyardSystemsPage;
        private GameObject crewManagementPage;
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
        private readonly RawImage[] cannonSlotIcons = new RawImage[ShipCustomizationModel.CannonSlotCount];
        private Text capacityUpgradeText;
        private Text propulsionUpgradeText;
        private Text armorUpgradeText;
        private Text turningUpgradeText;
        private Text gunUpgradeText;
        private Text crewHireText;
        private Text shipLevelUpgradeText;
        private Text provisionText;
        private Text portRepairText;
        private Text portFoodText;
        private Text portWaterText;
        private readonly Text[] crewRoleTexts = new Text[CrewManagementModel.RoleCount];
        private readonly Text[] crewPerkTexts = new Text[CrewManagementModel.RoleCount];
        private float toastUntil;
        private Font font;
        private Sprite panelSprite;
        private Sprite pillSprite;
        private Sprite diamondSprite;
        public bool IsModalOpen => (menuRoot != null && menuRoot.activeSelf) || (portRoot != null && portRoot.activeSelf) || (shipyardRoot != null && shipyardRoot.activeSelf) || mapRoot != null;

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
            if (Array.Exists(Environment.GetCommandLineArgs(), argument => argument == "--crew-preview"))
            {
                OpenPort();
                OpenShipyard();
                ShowShipyardPage(2);
            }
            else if (Array.Exists(Environment.GetCommandLineArgs(), argument => argument == "--shipyard-preview"))
            {
                OpenPort();
                OpenShipyard();
            }
            else if (Array.Exists(Environment.GetCommandLineArgs(), argument => argument == "--map-preview"))
            {
                OpenMap();
                boat.IncreaseCruiseStep();
            }
            else if (Array.Exists(Environment.GetCommandLineArgs(), argument => argument == "--port-preview")) OpenPort();
            else if (Array.Exists(Environment.GetCommandLineArgs(), argument => argument == "--menu-preview")) menuRoot.SetActive(true);
        }

        private void Update()
        {
            hullValue.text = $"{state.Hull}/{state.MaxHull}";
            goldValue.text = $"{state.Gold} G";
            crewValue.text = state.Crew.ToString();
            float loadRatio = ShipCustomizationModel.GetLoadRatio(state);
            loadValue.text = $"{ShipCustomizationModel.GetMass(state):0}/{ShipCustomizationModel.GetCapacity(state):0}";
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
            dashboard.anchoredPosition = new Vector2(0f, -190f);
            dashboard.sizeDelta = new Vector2(700f, 84f);
            hudRoot = dashboard.gameObject;
            hullValue = CreateStatusCard(dashboard, "HULL", new Vector2(-250f, 0f), UiTheme.Mint, out hullBar);
            goldValue = CreateStatusCard(dashboard, "GOLD", new Vector2(-82f, 0f), UiTheme.Brass, out _);
            crewValue = CreateStatusCard(dashboard, "CREW", new Vector2(82f, 0f), UiTheme.SecondaryText, out _);
            loadValue = CreateStatusCard(dashboard, "LOAD", new Vector2(250f, 0f), UiTheme.Brass, out loadBar);

            prompt = CreatePillText(canvas, "Context Action", new Vector2(0f, -625f), new Vector2(390f, 34f), 17);
            prompt.color = UiTheme.PrimaryText;
            UiTheme.StyleText(prompt, 18);
            toast = CreatePillText(canvas, "Event Message", new Vector2(0f, -365f), new Vector2(430f, 40f), 18);
            toast.color = UiTheme.Brass;
            UiTheme.StyleText(toast, 18);
            toast.transform.parent.gameObject.SetActive(false);
        }

        private Text CreateStatusCard(Transform parent, string label, Vector2 position, Color accent, out Image meter)
        {
            RectTransform card = CreateUiObject(label + " Status Card", parent); card.anchoredPosition = position; card.sizeDelta = new Vector2(142f, 72f);
            Image cardFill = card.gameObject.AddComponent<Image>(); cardFill.sprite = UiTextureFactory.LoadConceptSprite("Chrome", "button_fill"); cardFill.color = Color.white; cardFill.raycastTarget = false;
            RectTransform cardFrame = CreateUiObject(label + " Status Frame", card); cardFrame.sizeDelta = card.sizeDelta;
            Image frameImage = cardFrame.gameObject.AddComponent<Image>(); frameImage.sprite = UiTextureFactory.LoadConceptSprite("Chrome", "service_button", 22f); frameImage.type = Image.Type.Sliced; frameImage.color = Color.white; frameImage.raycastTarget = false;
            RectTransform iconRect = CreateUiObject(label + " Authored Icon", card); iconRect.anchoredPosition = new Vector2(-42f, 0f); iconRect.sizeDelta = new Vector2(36f, 36f);
            RawImage icon = iconRect.gameObject.AddComponent<RawImage>(); icon.texture = UiTextureFactory.LoadConceptTexture("Navigation", $"status_{label.ToLowerInvariant()}"); icon.raycastTarget = false;
            Text caption = CreateText(card, label, new Vector2(18f, 18f), new Vector2(78f, 20f), 14, TextAnchor.MiddleCenter); caption.color = accent; UiTheme.StyleText(caption, 14);
            Text value = CreateText(card, label + " Value", new Vector2(18f, -5f), new Vector2(80f, 26f), 20, TextAnchor.MiddleCenter); value.color = UiTheme.PrimaryText; UiTheme.StyleText(value, 20);
            value.resizeTextForBestFit = true; value.resizeTextMinSize = 13; value.resizeTextMaxSize = 20;
            RectTransform bar = CreateUiObject(label + " Meter", card); bar.anchoredPosition = new Vector2(18f, -26f); bar.sizeDelta = new Vector2(80f, 9f);
            Image track = bar.gameObject.AddComponent<Image>(); track.sprite = UiTextureFactory.LoadConceptSprite("Chrome", "slider_rail", 14f); track.type = Image.Type.Sliced; track.color = Color.white; track.raycastTarget = false;
            RectTransform fill = CreateUiObject(label + " Meter Fill", bar); fill.anchorMin = new Vector2(0f, 0.5f); fill.anchorMax = new Vector2(1f, 0.5f); fill.sizeDelta = new Vector2(-8f, 5f); fill.anchoredPosition = Vector2.zero;
            meter = fill.gameObject.AddComponent<Image>(); meter.type = Image.Type.Filled; meter.fillMethod = Image.FillMethod.Horizontal; meter.color = accent; meter.raycastTarget = false;
            if (label != "HULL" && label != "LOAD") bar.gameObject.SetActive(false);
            return value;
        }

        private void BuildPortPanel()
        {
            portRoot = CreateUiObject("Harbor Services", canvas).gameObject;
            Image back = portRoot.AddComponent<Image>();
            back.sprite = UiTextureFactory.LoadConceptSprite("Chrome", "port_panel_frame");
            back.color = Color.white;
            RectTransform rect = (RectTransform)portRoot.transform;
            rect.anchoredPosition = new Vector2(0f, -398f);
            rect.sizeDelta = new Vector2(490f, 540f);
            AddAuthoredPanelFill(portRoot.transform, new Vector2(420f, 462f));
            AddPanelFrameOverlay(portRoot.transform, "port_panel_frame", rect.sizeDelta);
            CreateText(portRoot.transform, "HARBOR", new Vector2(0f, 190f), new Vector2(280f, 34f), 22, TextAnchor.MiddleCenter).color = Brass;
            provisionText = CreateText(portRoot.transform, "F 20   W 20", new Vector2(0f, 154f), new Vector2(360f, 28f), 15, TextAnchor.MiddleCenter);
            provisionText.color = UiTheme.SecondaryText;
            portRepairText = CreateWideButton(portRoot.transform, "+10%   20G", new Vector2(0f, 108f), Repair, "repair").GetComponentInChildren<Text>();
            portFoodText = CreateWideButton(portRoot.transform, "+10   18G", new Vector2(0f, 54f), () => BuyProvision(true), "food").GetComponentInChildren<Text>();
            portWaterText = CreateWideButton(portRoot.transform, "+10   14G", new Vector2(0f, 0f), () => BuyProvision(false), "water").GetComponentInChildren<Text>();
            Button engineButton = CreateWideButton(portRoot.transform, "L0 > L1", new Vector2(0f, -54f), UpgradeEngine, "propulsion");
            engineUpgradeText = engineButton.GetComponentInChildren<Text>();
            CreateWideButton(portRoot.transform, "OPEN", new Vector2(0f, -108f), OpenShipyard, "shipyard");
            CreateButton(portRoot.transform, "SAIL", new Vector2(0f, -196f), () => DepartPort(portRoot));
            portRoot.SetActive(false);
        }

        private void BuildShipyardPanel()
        {
            shipyardRoot = CreateUiObject("Shipyard Customization", canvas).gameObject;
            Image back = shipyardRoot.AddComponent<Image>(); back.sprite = UiTextureFactory.LoadConceptSprite("Chrome", "shipyard_panel_frame"); back.color = Color.white;
            RectTransform rect = (RectTransform)shipyardRoot.transform;
            rect.anchoredPosition = new Vector2(0f, -410f);
            rect.sizeDelta = new Vector2(560f, 580f);
            AddAuthoredPanelFill(shipyardRoot.transform, new Vector2(482f, 502f));
            AddPanelFrameOverlay(shipyardRoot.transform, "shipyard_panel_frame", rect.sizeDelta);
            RectTransform titleFill = CreateUiObject("Shipyard Title Navy Fill", shipyardRoot.transform);
            titleFill.anchoredPosition = new Vector2(0f, 230f);
            titleFill.sizeDelta = new Vector2(250f, 30f);
            RawImage titleFillImage = titleFill.gameObject.AddComponent<RawImage>();
            titleFillImage.texture = UiTextureFactory.LoadConceptTexture("Chrome", "button_fill");
            titleFillImage.raycastTarget = false;
            CreateMapStrip(shipyardRoot.transform, "Shipyard Title Plaque", new Vector2(0f, 230f), new Vector2(264f, 34f));
            CreateText(shipyardRoot.transform, "SHIPYARD", new Vector2(0f, 230f), new Vector2(260f, 30f), 22, TextAnchor.MiddleCenter).color = Brass;
            shipyardSummary = CreateText(shipyardRoot.transform, "Shipyard Summary", new Vector2(0f, 191f), new Vector2(500f, 42f), 13, TextAnchor.MiddleCenter);
            shipyardSummary.color = new Color(0.78f, 0.91f, 0.90f, 1f);

            CreateSizedButton(shipyardRoot.transform, "GUN DECK", new Vector2(-172f, 142f), new Vector2(160f, 38f), () => ShowShipyardPage(0));
            CreateSizedButton(shipyardRoot.transform, "SYSTEMS", new Vector2(0f, 142f), new Vector2(160f, 38f), () => ShowShipyardPage(1));
            CreateSizedButton(shipyardRoot.transform, "CREW", new Vector2(172f, 142f), new Vector2(160f, 38f), () => ShowShipyardPage(2));

            shipyardGunsPage = CreateUiObject("Gun Deck Page", shipyardRoot.transform).gameObject;
            CreateText(shipyardGunsPage.transform, "CLICK A HARDPOINT TO INSTALL OR STORE", new Vector2(0f, 92f), new Vector2(430f, 24f), 15, TextAnchor.MiddleCenter).color = UiTheme.SecondaryText;
            CreateCannonSlotButton(CannonSlot.Bow, new Vector2(0f, 48f));
            CreateCannonSlotButton(CannonSlot.PortFore, new Vector2(-125f, 6f));
            CreateCannonSlotButton(CannonSlot.StarboardFore, new Vector2(125f, 6f));
            CreateCannonSlotButton(CannonSlot.PortAft, new Vector2(-125f, -36f));
            CreateCannonSlotButton(CannonSlot.StarboardAft, new Vector2(125f, -36f));
            CreateCannonSlotButton(CannonSlot.Stern, new Vector2(0f, -78f));

            shipyardSystemsPage = CreateUiObject("Ship Systems Page", shipyardRoot.transform).gameObject;
            capacityUpgradeText = CreateCompactButton(shipyardSystemsPage.transform, "CAPACITY", new Vector2(-120f, 72f), UpgradeCapacity);
            propulsionUpgradeText = CreateCompactButton(shipyardSystemsPage.transform, "PROPULSION", new Vector2(120f, 72f), UpgradeEngine);
            armorUpgradeText = CreateCompactButton(shipyardSystemsPage.transform, "ARMOR", new Vector2(-120f, 20f), UpgradeArmor);
            turningUpgradeText = CreateCompactButton(shipyardSystemsPage.transform, "TURNING", new Vector2(120f, 20f), UpgradeTurning);
            gunUpgradeText = CreateCompactButton(shipyardSystemsPage.transform, "GUNS", new Vector2(-120f, -32f), UpgradeCannon);
            crewHireText = CreateCompactButton(shipyardSystemsPage.transform, "HIRE CREW", new Vector2(120f, -32f), HireCrew);
            shipLevelUpgradeText = CreateCompactButton(shipyardSystemsPage.transform, "SHIP LEVEL", new Vector2(0f, -84f), UpgradeShipLevel);

            crewManagementPage = CreateUiObject("Crew Management Page", shipyardRoot.transform).gameObject;
            CreateText(crewManagementPage.transform, "1 PERK SLOT PER CREW  •  CLICK TO STACK  •  FULL CLICK RESETS", new Vector2(0f, 98f), new Vector2(470f, 24f), 13, TextAnchor.MiddleCenter).color = UiTheme.SecondaryText;
            for (int i = 0; i < CrewManagementModel.RoleCount; i++) CreateCrewRoleRow((CrewRole)i, 65f - i * 43f);

            CreateCompactButton(shipyardRoot.transform, "BACK TO PORT", new Vector2(-120f, -236f), () => { shipyardRoot.SetActive(false); portRoot.SetActive(true); RefreshPortStatus(); });
            CreateCompactButton(shipyardRoot.transform, "SAIL", new Vector2(120f, -236f), () => DepartPort(shipyardRoot));
            shipyardRoot.SetActive(false);
            ShowShipyardPage(0);
        }

        private void CreateCrewRoleRow(CrewRole role, float y)
        {
            int index = (int)role;
            crewRoleTexts[index] = CreateText(crewManagementPage.transform, role.ToString().ToUpperInvariant(), new Vector2(-105f, y), new Vector2(200f, 36f), 14, TextAnchor.MiddleLeft);
            CreateCrewStepButton(crewManagementPage.transform, "-", new Vector2(28f, y), () => ChangeCrewRole(role, -1));
            CreateCrewStepButton(crewManagementPage.transform, "+", new Vector2(78f, y), () => ChangeCrewRole(role, 1));
            crewPerkTexts[index] = CreateSizedButton(crewManagementPage.transform, "PERKS", new Vector2(172f, y), new Vector2(136f, 36f), () => CycleRolePerk(role)).GetComponentInChildren<Text>();
        }

        private void CreateCrewStepButton(Transform parent, string label, Vector2 position, Action action)
        {
            RectTransform rect = CreateUiObject(label + " Crew Step", parent); rect.anchoredPosition = position; rect.sizeDelta = new Vector2(48f, 38f);
            Image image = rect.gameObject.AddComponent<Image>(); image.sprite = UiTextureFactory.LoadConceptSprite("Chrome", "tab_frame", 18f); image.type = Image.Type.Sliced;
            Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(() => action());
            Text text = CreateText(rect, label, Vector2.zero, rect.sizeDelta, 20, TextAnchor.MiddleCenter); text.color = UiTheme.PrimaryText; UiTheme.StyleText(text, 20);
        }

        private void CreateCannonSlotButton(CannonSlot slot, Vector2 position)
        {
            Button button = CreateSizedButton(shipyardGunsPage.transform, ShipCustomizationModel.GetDefinition(slot).ShortName, position, new Vector2(210f, 38f), () => ToggleCannon(slot));
            cannonSlotTexts[(int)slot] = button.GetComponentInChildren<Text>();
            cannonSlotIcons[(int)slot] = AddButtonIcon(button.transform, UiTextureFactory.LoadPortIcon("hardpoint_empty"), new Vector2(-82f, 0f), 32f);
        }

        private Text CreateCompactButton(Transform parent, string label, Vector2 position, Action action)
            => CreateSizedButton(parent, label, position, new Vector2(226f, 42f), action).GetComponentInChildren<Text>();

        private void ShowShipyardPage(int page)
        {
            if (shipyardGunsPage != null) shipyardGunsPage.SetActive(page == 0);
            if (shipyardSystemsPage != null) shipyardSystemsPage.SetActive(page == 1);
            if (crewManagementPage != null) crewManagementPage.SetActive(page == 2);
        }

        private void OpenPort()
        {
            CloseAll();
            boat.HoldAtMooring();
            RefreshEngineUpgradeText();
            RefreshPortStatus();
            portRoot.SetActive(true);
            saves.Save(state);
        }

        private void DepartPort(GameObject panel)
        {
            panel.SetActive(false);
            boat.ReleaseMooring();
            saves.Save(state);
            ShowMessage("CAST OFF — W/S TELEGRAPH READY");
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
                if (!ShipCustomizationModel.IsHardpointUnlocked(state, slot)) { ShowMessage("HARDPOINT LOCKED BY SHIP LEVEL"); return; }
                if (ShipCustomizationModel.GetInstalledCannonCount(state) >= ShipCustomizationModel.GetCannonCapacity(state)) { ShowMessage("SHIP LEVEL CANNON LIMIT"); return; }
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
            int oldMax = state.MaxHull;
            state.Gold -= cost;
            state.ArmorLevel++;
            state.MaxHull = ShipProgressionModel.GetMaxHull(state);
            int gained = state.MaxHull - oldMax;
            state.Hull = Mathf.Min(state.MaxHull, state.Hull + gained);
            boat.RefreshCustomizationVisual(); RefreshShipyard(); saves.Save(state); ShowMessage($"ARMOR {state.ArmorLevel}  HULL +{gained}");
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
            if (state.Crew >= ShipCustomizationModel.GetCrewCapacity(state)) { ShowMessage("SHIP LEVEL CREW LIMIT"); return; }
            if (!ShipCustomizationModel.CanAddMass(state, ShipCustomizationModel.CrewMass)) { ShowMessage("NO BERTH CAPACITY"); return; }
            if (state.Gold < cost) { ShowMessage($"CREW NEEDS {cost}G"); return; }
            state.Gold -= cost; state.Crew++; RefreshShipyard(); saves.Save(state); ShowMessage($"CREW ABOARD  {state.Crew}");
        }

        private void ChangeCrewRole(CrewRole role, int delta)
        {
            bool changed = delta > 0 ? CrewManagementModel.AssignOne(state, role) : CrewManagementModel.UnassignOne(state, role);
            if (!changed) { ShowMessage(delta > 0 ? "NO UNASSIGNED CREW" : "ROLE ALREADY EMPTY"); return; }
            RefreshShipyard(); saves.Save(state);
        }

        private void CycleRolePerk(CrewRole role)
        {
            int equippedTotal = state.GetEquippedPerkTotal(role);
            if (equippedTotal >= state.GetRoleCrew(role) && equippedTotal > 0)
            {
                state.ClearEquippedPerks(role);
                RefreshShipyard(); saves.Save(state);
                ShowMessage($"{role.ToString().ToUpperInvariant()} PERKS CLEARED");
                return;
            }
            for (int i = 1; i < CrewManagementModel.PerkCount; i++)
            {
                CrewPerk candidate = (CrewPerk)i;
                if (!CrewManagementModel.IsCompatible(role, candidate)) continue;
                if (!state.TryEquipPerkStack(role, candidate, out PerkRank rank)) continue;
                RefreshShipyard(); saves.Save(state);
                int stacks = state.GetEquippedPerkCount(role, candidate);
                ShowMessage($"{role.ToString().ToUpperInvariant()} — {CrewManagementModel.GetPerkName(candidate)} {PerkRankModel.GetLabel(rank)} x{stacks}");
                return;
            }
            if (equippedTotal > 0)
            {
                state.ClearEquippedPerks(role);
                RefreshShipyard(); saves.Save(state);
                ShowMessage($"{role.ToString().ToUpperInvariant()} PERKS CLEARED");
            }
            else ShowMessage(state.GetRoleCrew(role) <= 0 ? "ASSIGN CREW BEFORE EQUIPPING PERKS" : "NO COMPATIBLE BOSS PERKS OWNED");
        }

        private void UpgradeShipLevel()
        {
            if (ShipProgressionModel.IsMax(state.ShipLevel)) { ShowMessage("LARGE SHIP IS MAX LEVEL"); return; }
            ShipTierDefinition next = ShipProgressionModel.Get(state.ShipLevel + 1);
            if (!ShipProgressionModel.Upgrade(state)) { ShowMessage($"{next.Name} NEEDS {next.UpgradeCost}G"); return; }
            boat.RefreshCustomizationVisual(); RefreshShipyard(); saves.Save(state);
            ShowMessage($"SHIP LEVEL UP — {next.Name}");
        }

        private bool CanBuyUpgrade(int level, int cost, string name)
        {
            if (level >= ShipCustomizationModel.GetUpgradeCap(state)) { ShowMessage($"{name} CAPPED — UPGRADE SHIP LEVEL"); return false; }
            if (state.Gold < cost) { ShowMessage($"{name} NEEDS {cost}G"); return false; }
            return true;
        }

        private void RefreshShipyard()
        {
            float mass = ShipCustomizationModel.GetMass(state);
            float capacity = ShipCustomizationModel.GetCapacity(state);
            ShipTierDefinition tier = ShipProgressionModel.Get(state.ShipLevel);
            shipyardSummary.text = $"L{state.ShipLevel + 1} {tier.Name}   HULL {state.MaxHull}   MASS {mass:0.0}/{capacity:0.0}   SPEED {ShipCustomizationModel.GetSpeedMultiplier(state) * 100f:0}%\nCREW {state.Crew}/{tier.MaxCrew}  FREE {CrewManagementModel.GetUnassigned(state)}   GUNS {ShipCustomizationModel.GetInstalledCannonCount(state)}/{tier.MaxCannons}   UPGRADE CAP {tier.UpgradeCap}";
            for (int i = 0; i < cannonSlotTexts.Length; i++)
            {
                CannonSlot slot = (CannonSlot)i;
                CannonSlotDefinition definition = ShipCustomizationModel.GetDefinition(slot);
                bool unlocked = ShipCustomizationModel.IsHardpointUnlocked(state, slot);
                cannonSlotTexts[i].text = $"{definition.ShortName}  {(ShipCustomizationModel.HasCannon(state, slot) ? "● ARMED" : unlocked ? "+ EMPTY" : "LOCKED")}";
                cannonSlotTexts[i].color = ShipCustomizationModel.HasCannon(state, slot) ? new Color(1f, 0.76f, 0.28f) : new Color(0.62f, 0.76f, 0.76f);
                if (cannonSlotIcons[i] != null) cannonSlotIcons[i].texture = UiTextureFactory.LoadPortIcon(ShipCustomizationModel.HasCannon(state, slot) ? "hardpoint_cannon" : "hardpoint_empty");
            }
            int cap = ShipCustomizationModel.GetUpgradeCap(state);
            capacityUpgradeText.text = UpgradeLabel("CAPACITY", state.CapacityLevel, ShipCustomizationModel.GetCapacityUpgradeCost(state.CapacityLevel), cap);
            propulsionUpgradeText.text = UpgradeLabel("PROPULSION", state.EngineLevel, CruiseModel.GetUpgradeCost(state.EngineLevel), cap);
            armorUpgradeText.text = UpgradeLabel("ARMOR", state.ArmorLevel, ShipCustomizationModel.GetArmorUpgradeCost(state.ArmorLevel), cap);
            turningUpgradeText.text = UpgradeLabel("TURNING", state.TurningLevel, ShipCustomizationModel.GetTurningUpgradeCost(state.TurningLevel), cap);
            gunUpgradeText.text = UpgradeLabel("GUN DAMAGE", state.CannonLevel, ShipCustomizationModel.GetGunUpgradeCost(state.CannonLevel), cap);
            crewHireText.text = $"HIRE CREW  {ShipCustomizationModel.GetCrewHireCost(state)}G";
            ShipTierDefinition next = ShipProgressionModel.Get(Mathf.Min(state.ShipLevel + 1, ShipProgressionModel.TierCount - 1));
            shipLevelUpgradeText.text = ShipProgressionModel.IsMax(state.ShipLevel) ? "SHIP LEVEL  MAX" : $"SHIP → {next.Name}  {next.UpgradeCost}G";
            for (int i = 0; i < CrewManagementModel.RoleCount; i++)
            {
                CrewRole role = (CrewRole)i;
                crewRoleTexts[i].text = $"{role.ToString().ToUpperInvariant()}  {state.GetRoleCrew(role)}";
                int equipped = state.GetEquippedPerkTotal(role);
                crewPerkTexts[i].text = equipped <= 0
                    ? $"PERKS 0/{Mathf.Max(0, state.GetRoleCrew(role))}"
                    : $"{PerkRankModel.GetLabel(CrewManagementModel.GetHighestEquippedRank(state, role))}  {equipped}/{Mathf.Max(0, state.GetRoleCrew(role))}";
            }
        }

        private static string UpgradeLabel(string name, int level, int cost, int cap)
            => level >= cap ? $"{name}  CAP {cap}" : $"{name}  L{level}→{level + 1}  {cost}G";

        private void Repair()
        {
            GetPortRepairQuote(out int amount, out int cost);
            if (amount <= 0) { ShowMessage("HULL ALREADY FULL"); return; }
            if (state.Gold < cost) { ShowMessage($"REPAIR NEEDS {cost}G"); return; }
            state.Gold -= cost;
            state.Hull += amount;
            ShowMessage($"REPAIRED +{amount}  -{cost}G");
            RefreshPortStatus();
            saves.Save(state);
        }

        private void GetPortRepairQuote(out int amount, out int cost)
        {
            int missing = Mathf.Max(0, state.MaxHull - state.Hull);
            int chunk = Mathf.Max(1, Mathf.CeilToInt(state.MaxHull * 0.10f));
            amount = Mathf.Min(missing, chunk);
            int fullCost = 20 + state.ShipLevel * 18;
            cost = amount <= 0 ? 0 : Mathf.Max(5, Mathf.CeilToInt(fullCost * (amount / (float)chunk)));
        }

        private void BuySupplies()
        {
            if (state.Gold < 20) { ShowMessage("NOT ENOUGH GOLD"); return; }
            state.Gold -= 20; state.Supplies += 5; ShowMessage("SUPPLIES +5"); saves.Save(state);
        }

        private void BuyProvision(bool food)
        {
            int cost = food ? 18 : 14;
            if (state.Gold < cost) { ShowMessage("NOT ENOUGH GOLD"); return; }
            state.Gold -= cost;
            if (food) state.Food += 10; else state.Water += 10;
            RefreshPortStatus(); saves.Save(state);
            ShowMessage(food ? "FOOD +10" : "FRESH WATER +10");
        }

        private void RefreshPortStatus()
        {
            if (provisionText != null)
                provisionText.text = $"F {state.Food}   W {state.Water}   CREW {state.Crew}   -{ProvisionModel.GetUnitsPerInterval(state.Crew)}/2m";
            GetPortRepairQuote(out int repairAmount, out int repairCost);
            if (portRepairText != null) portRepairText.text = repairAmount <= 0 ? "FULL" : $"+{repairAmount}   {repairCost}G";
            if (portFoodText != null) portFoodText.text = "+10   18G";
            if (portWaterText != null) portWaterText.text = "+10   14G";
        }

        private void UpgradeEngine()
        {
            int cap = Mathf.Min(CruiseModel.MaxEngineLevel, ShipCustomizationModel.GetUpgradeCap(state));
            if (state.EngineLevel >= cap) { ShowMessage("PROPULSION CAPPED — UPGRADE SHIP LEVEL"); return; }
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
            int cap = Mathf.Min(CruiseModel.MaxEngineLevel, ShipCustomizationModel.GetUpgradeCap(state));
            engineUpgradeText.text = state.EngineLevel >= cap
                ? $"MAX   {CruiseModel.GetMaxSpeed(state.EngineLevel):0.00}"
                : $"L{state.EngineLevel} > L{state.EngineLevel + 1}   {CruiseModel.GetUpgradeCost(state.EngineLevel)}G";
        }

        private void UpgradeCannon()
        {
            int cost = ShipCustomizationModel.GetGunUpgradeCost(state.CannonLevel);
            if (!CanBuyUpgrade(state.CannonLevel, cost, "GUN DAMAGE")) return;
            state.Gold -= cost; state.CannonLevel++; RefreshShipyard();
            int perCannon = ShipCustomizationModel.BaseCannonDamage + state.CannonLevel * ShipCustomizationModel.CannonUpgradeDamage;
            ShowMessage($"GUN DAMAGE {perCannon} PER CANNON"); saves.Save(state);
        }

        private void OpenMap()
        {
            if (mapRoot != null) return;
            mapChartCenter = boat.LogicalPosition;
            RectTransform panel = CreateUiObject("Exploration Chart", canvas);
            mapRoot = panel.gameObject;
            panel.anchoredPosition = new Vector2(0f, -425f);
            panel.sizeDelta = new Vector2(550f, 550f);
            Image panelBack = panel.gameObject.AddComponent<Image>(); panelBack.sprite = UiTextureFactory.LoadConceptSprite("Chrome", "map_panel_frame"); panelBack.color = Color.white; panelBack.raycastTarget = false;
            AddAuthoredPanelFill(panel, new Vector2(488f, 488f));
            AddPanelFrameOverlay(panel, "map_panel_frame", panel.sizeDelta);

            Text title = CreateText(panel, "EXPLORATION CHART", new Vector2(0f, 246f), new Vector2(320f, 30f), 22, TextAnchor.MiddleCenter);
            title.color = UiTheme.Brass; UiTheme.StyleText(title, 22);
            localZoomText = CreateSizedButton(panel, "LOCAL", new Vector2(-74f, 211f), new Vector2(136f, 38f), () => SetMapZoom(MapZoom.Local)).GetComponentInChildren<Text>();
            wideZoomText = CreateSizedButton(panel, "WIDE", new Vector2(74f, 211f), new Vector2(136f, 38f), () => SetMapZoom(MapZoom.Wide)).GetComponentInChildren<Text>();

            RectTransform frameRect = CreateUiObject("Chart Brass Bezel", panel); frameRect.anchoredPosition = new Vector2(0f, -2f); frameRect.sizeDelta = new Vector2(438f, 438f);
            Image frame = frameRect.gameObject.AddComponent<Image>(); frame.sprite = UiTextureFactory.LoadConceptSprite("Chrome", "radial_hub"); frame.color = Color.white; frame.raycastTarget = false;
            RectTransform clip = CreateUiObject("Chart Circular Viewport", panel); clip.anchoredPosition = new Vector2(0f, -2f); clip.sizeDelta = new Vector2(408f, 408f);
            Image clipGraphic = clip.gameObject.AddComponent<Image>(); clipGraphic.sprite = UiTextureFactory.LoadConceptSprite("Chrome", "panel_fill"); clipGraphic.color = Color.white; clipGraphic.raycastTarget = false;
            Mask mask = clip.gameObject.AddComponent<Mask>(); mask.showMaskGraphic = false;
            RectTransform mapRect = CreateUiObject("Explored Waters", clip); mapRect.sizeDelta = new Vector2(398f, 398f);
            mapImage = mapRect.gameObject.AddComponent<RawImage>(); mapImage.raycastTarget = false;
            RectTransform markersRect = CreateUiObject("Chart Markers", clip);
            markersRect.sizeDelta = new Vector2(398f, 398f);
            mapMarkersRoot = markersRect.gameObject;

            CreateCardinalLabel(panel, "N", new Vector2(0f, 184f));
            CreateCardinalLabel(panel, "E", new Vector2(208f, -2f));
            CreateCardinalLabel(panel, "S", new Vector2(0f, -169f));
            CreateCardinalLabel(panel, "W", new Vector2(-208f, -2f));
            CreateMapStrip(panel, "Chart Position Strip", new Vector2(0f, -199f), new Vector2(404f, 30f));
            mapStatus = CreateText(panel, "Chart Status", new Vector2(0f, -199f), new Vector2(394f, 26f), 15, TextAnchor.MiddleCenter);
            mapStatus.color = UiTheme.Brass; UiTheme.StyleText(mapStatus, 15);
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
            mappedResolvedEventCount = state.ResolvedEvents.Count;
        }

        private void UpdateLiveMap()
        {
            nextMapLiveUpdate = Time.unscaledTime + 0.12f;
            bool moved = (boat.LogicalPosition - mapChartCenter).sqrMagnitude > 0.0001f;
            mapChartCenter = boat.LogicalPosition;
            if (moved || mappedExploredChunkCount != state.ExploredChunks.Count || mappedResolvedEventCount != state.ResolvedEvents.Count)
            {
                RefreshMapTexture();
            }
            // Enemy ships can move while the chart is open; rebuild only the lightweight
            // marker layer every live tick while keeping the player locked at the center.
            RebuildMapMarkers();
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
                    Vector2 worldPosition = GetLivePoiPosition(data.Id, data.Position);
                    Vector2 position = MapChartModel.WorldToMap(worldPosition, mapChartCenter, mapZoom);
                    if (!MapChartModel.IsInside(position)) continue;
                    RectTransform marker = CreateUiObject(data.Boss == BossKind.None ? $"Known {data.Kind}" : $"Boss {data.Boss}", mapMarkersRoot.transform);
                    float size = data.Boss != BossKind.None ? (mapZoom == MapZoom.Local ? 58f : 46f) : (mapZoom == MapZoom.Local ? 46f : 36f);
                    marker.anchoredPosition = position; marker.sizeDelta = Vector2.one * size;
                    RawImage image = marker.gameObject.AddComponent<RawImage>(); image.texture = UiTextureFactory.LoadPoiBadge(data.Kind); image.raycastTarget = false;
                    if (data.Boss != BossKind.None) image.color = UiTheme.Warning;
                }
            }

            mapHeading = CreateUiObject("Ship Heading", mapMarkersRoot.transform); mapHeading.sizeDelta = new Vector2(5f, 48f); mapHeading.pivot = new Vector2(0.5f, 0f);
            Image course = mapHeading.gameObject.AddComponent<Image>(); course.color = UiTheme.Brass; course.raycastTarget = false;
            mapPlayerMarker = CreateUiObject("Current Ship Position", mapMarkersRoot.transform); mapPlayerMarker.sizeDelta = new Vector2(48f, 48f);
            RawImage playerImage = mapPlayerMarker.gameObject.AddComponent<RawImage>(); playerImage.texture = UiTextureFactory.LoadConceptTexture("Icons", "marker_player"); playerImage.color = Color.white; playerImage.raycastTarget = false;
            UpdateMapPlayerMarker();
        }

        private void UpdateMapPlayerMarker()
        {
            if (mapPlayerMarker == null || mapHeading == null) return;
            Vector2 position = Vector2.zero;
            mapPlayerMarker.anchoredPosition = position;
            mapHeading.anchoredPosition = position;
            mapPlayerMarker.localRotation = Quaternion.Euler(0f, 0f, -boat.HeadingDegrees);
            mapHeading.localRotation = Quaternion.Euler(0f, 0f, -boat.HeadingDegrees);
        }

        private Vector2 GetLivePoiPosition(ulong id, Vector2 generatedPosition)
        {
            if (pois == null) return generatedPosition;
            foreach (PoiRecord item in pois.Items)
                if (item.Id == id && !item.Resolved) return item.LogicalPosition;
            return generatedPosition;
        }

        private void CreateCardinalLabel(Transform parent, string label, Vector2 position)
        {
            Text text = CreateText(parent, label, position, new Vector2(34f, 30f), 18, TextAnchor.MiddleCenter);
            text.color = label == "N" ? UiTheme.Brass : UiTheme.PrimaryText; UiTheme.StyleText(text, 18);
        }

        private void BuildMapLegend(Transform parent)
        {
            CreateMapStrip(parent, "Chart Legend Strip", new Vector2(0f, -234f), new Vector2(438f, 40f));
            PoiKind[] kinds = { PoiKind.Port, PoiKind.Enemy, PoiKind.Wreck, PoiKind.Treasure };
            string[] labels = { "PORT", "ENEMY", "WRECK", "TREASURE" };
            for (int i = 0; i < kinds.Length; i++)
            {
                float x = -150f + i * 100f;
                RectTransform icon = CreateUiObject(labels[i] + " Legend Icon", parent); icon.anchoredPosition = new Vector2(x - 28f, -234f); icon.sizeDelta = new Vector2(38f, 38f);
                RawImage badge = icon.gameObject.AddComponent<RawImage>(); badge.texture = UiTextureFactory.LoadPoiBadge(kinds[i]); badge.raycastTarget = false;
                Text label = CreateText(parent, labels[i], new Vector2(x + 14f, -234f), new Vector2(72f, 24f), 14, TextAnchor.MiddleLeft);
                label.color = UiTheme.PrimaryText; UiTheme.StyleText(label, 14);
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
            mapPlayerMarker = null; mapHeading = null; mappedExploredChunkCount = -1; mappedResolvedEventCount = -1;
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
            rect.anchoredPosition = position; rect.sizeDelta = new Vector2(80f, 80f);
            MenuGlyph glyph = label == "SAVE" ? MenuGlyph.Save
                : label == "BAG" ? MenuGlyph.Inventory
                : label == "BACK" ? MenuGlyph.Back
                : label == "EXIT" || label == "SAIL" ? MenuGlyph.Exit
                : MenuGlyph.Map;
            RawImage image = rect.gameObject.AddComponent<RawImage>(); image.texture = label == "SAIL" ? UiTextureFactory.LoadPortIcon("sail") : UiTextureFactory.LoadMenuButton(glyph, 128); image.color = Color.white;
            Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(() => action());
            return button;
        }

        private Button CreateWideButton(Transform parent, string label, Vector2 position, Action action, string iconName = null)
        {
            RectTransform rect = CreateUiObject(label, parent); rect.anchoredPosition = position; rect.sizeDelta = new Vector2(310f, 50f);
            Image image = rect.gameObject.AddComponent<Image>(); image.sprite = UiTextureFactory.LoadConceptSprite("Chrome", "service_button", 22f); image.type = Image.Type.Sliced; image.color = Color.white;
            Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(() => action());
            Text text = CreateText(rect, label, new Vector2(26f, 0f), new Vector2(238f, 46f), 17, TextAnchor.MiddleCenter); text.color = UiTheme.PrimaryText; UiTheme.StyleText(text, 17);
            Texture2D icon = string.IsNullOrEmpty(iconName) ? GetActionIcon(label) : UiTextureFactory.LoadPortIcon(iconName);
            if (icon != null) AddButtonIcon(rect, icon, new Vector2(-126f, 0f), 42f);
            return button;
        }

        private Button CreateSizedButton(Transform parent, string label, Vector2 position, Vector2 size, Action action)
        {
            RectTransform rect = CreateUiObject(label, parent); rect.anchoredPosition = position; rect.sizeDelta = size;
            Image image = rect.gameObject.AddComponent<Image>(); image.sprite = UiTextureFactory.LoadConceptSprite("Chrome", "tab_frame", 18f); image.type = Image.Type.Sliced; image.color = Color.white;
            Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(() => action());
            Text text = CreateText(rect, label, new Vector2(12f, 0f), size - new Vector2(42f, 0f), 14, TextAnchor.MiddleCenter); text.color = UiTheme.PrimaryText; UiTheme.StyleText(text, 14);
            Texture2D icon = GetActionIcon(label);
            if (icon != null) AddButtonIcon(rect, icon, new Vector2(-size.x * 0.5f + 24f, 0f), Mathf.Min(32f, size.y - 6f));
            return button;
        }

        private void CreateSlider(Transform parent, string label, Vector2 position, float min, float max, float value, Action<float> changed, bool commitOnRelease = false)
        {
            RectTransform holder = CreateUiObject(label, parent); holder.anchoredPosition = position; holder.sizeDelta = new Vector2(152f, 48f);
            Image holderBackground = holder.gameObject.AddComponent<Image>(); holderBackground.sprite = UiTextureFactory.LoadConceptSprite("Chrome", "button_fill"); holderBackground.color = Color.white;
            RectTransform holderFrame = CreateUiObject(label + " Authored Frame", holder); holderFrame.sizeDelta = holder.sizeDelta;
            Image holderFrameImage = holderFrame.gameObject.AddComponent<Image>(); holderFrameImage.sprite = UiTextureFactory.LoadConceptSprite("Chrome", "service_button", 22f); holderFrameImage.type = Image.Type.Sliced; holderFrameImage.raycastTarget = false;
            RectTransform glyphRect = CreateUiObject(label + " Icon", holder); glyphRect.anchoredPosition = new Vector2(-55f, 0f); glyphRect.sizeDelta = new Vector2(28f, 28f);
            RawImage glyphImage = glyphRect.gameObject.AddComponent<RawImage>(); glyphImage.texture = UiTextureFactory.LoadGlyph(label == "VOL" ? MenuGlyph.Volume : MenuGlyph.Size, 32); glyphImage.raycastTarget = false;
            RectTransform bar = CreateUiObject("Bar", holder); bar.anchoredPosition = new Vector2(27f, commitOnRelease ? 6f : 0f); bar.sizeDelta = new Vector2(82f, 8f);
            Image background = bar.gameObject.AddComponent<Image>(); background.sprite = UiTextureFactory.LoadConceptSprite("Chrome", "slider_rail", 14f); background.type = Image.Type.Sliced; background.color = Color.white;
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

        private void AddAuthoredPanelFill(Transform parent, Vector2 size)
        {
            RectTransform fill = CreateUiObject("Authored Navy Panel Fill", parent);
            fill.sizeDelta = size;
            RawImage image = fill.gameObject.AddComponent<RawImage>();
            image.texture = UiTextureFactory.LoadConceptTexture("Chrome", "panel_fill");
            image.color = Color.white;
            image.raycastTarget = false;
            fill.SetAsFirstSibling();
        }

        private static void AddPanelFrameOverlay(Transform parent, string textureName, Vector2 size)
        {
            RectTransform frame = CreateUiObject("Authored Frame Overlay", parent);
            frame.sizeDelta = size;
            RawImage image = frame.gameObject.AddComponent<RawImage>();
            image.texture = UiTextureFactory.LoadConceptTexture("Chrome", textureName);
            image.raycastTarget = false;
        }

        private static RawImage AddButtonIcon(Transform parent, Texture2D texture, Vector2 position, float size)
        {
            RectTransform rect = CreateUiObject("Authored Action Icon", parent);
            rect.anchoredPosition = position;
            rect.sizeDelta = Vector2.one * size;
            RawImage image = rect.gameObject.AddComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
            return image;
        }

        private static Texture2D GetActionIcon(string label)
        {
            if (label.Contains("REPAIR")) return UiTextureFactory.LoadPortIcon("repair");
            if (label.Contains("SUPPLIES")) return UiTextureFactory.LoadPortIcon("supplies");
            if (label.Contains("ENGINE") || label.Contains("PROPULSION")) return UiTextureFactory.LoadPortIcon("propulsion");
            if (label.Contains("SHIPYARD")) return UiTextureFactory.LoadPortIcon("shipyard");
            if (label.Contains("CAPACITY")) return UiTextureFactory.LoadPortIcon("capacity");
            if (label.Contains("ARMOR")) return UiTextureFactory.LoadPortIcon("armor");
            if (label.Contains("TURNING")) return UiTextureFactory.LoadPortIcon("turning");
            if (label.Contains("GUN DECK")) return UiTextureFactory.LoadPortIcon("gun_deck");
            if (label.Contains("SHIP SYSTEMS")) return UiTextureFactory.LoadPortIcon("systems");
            if (label.Contains("GUN")) return UiTextureFactory.LoadPortIcon("gun_upgrade");
            if (label.Contains("CREW")) return UiTextureFactory.LoadPortIcon("hire_crew");
            if (label.Contains("SAIL")) return UiTextureFactory.LoadPortIcon("sail");
            if (label.Contains("BACK")) return UiTextureFactory.LoadMenuButton(MenuGlyph.Back);
            return null;
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
