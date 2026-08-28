using System;
using System.Text;
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
        private GameObject captainLogRoot;
        private Text captainSummary;
        private Text enemyCatalog;
        private Text bossCatalog;
        private Text regionCatalog;
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
        private RectTransform hullStatusCard;
        private RectTransform goldStatusCard;
        private Text hullValue;
        private Text goldValue;
        private Text crewValue;
        private Text loadValue;
        private Image hullBar;
        private Image loadBar;
        private Text prompt;
        private Text toast;
        private Text menuVolumeValue;
        private Text menuSizeValue;
        private Text menuMuteLabel;
        private Text menuLanguageLabel;
        private Slider menuVolumeSlider;
        private Slider menuSizeSlider;
        private float lastAudibleVolume = 0.65f;
        private Text shipyardSummary;
        private readonly Text[] cannonSlotTexts = new Text[ShipCustomizationModel.CannonSlotCount];
        private readonly RectTransform[] cannonSlotRects = new RectTransform[ShipCustomizationModel.CannonSlotCount];
        private readonly Image[] cannonSlotBackplates = new Image[ShipCustomizationModel.CannonSlotCount];
        private readonly Image[] cannonSlotFrames = new Image[ShipCustomizationModel.CannonSlotCount];
        private readonly RawImage[] cannonSlotCardIcons = new RawImage[ShipCustomizationModel.CannonSlotCount];
        private readonly Image[] shipyardDeckMountRings = new Image[ShipCustomizationModel.CannonSlotCount];
        private readonly RawImage[] shipyardDeckCannons = new RawImage[ShipCustomizationModel.CannonSlotCount];
        private readonly Image[] shipyardTabBackplates = new Image[3];
        private Text capacityUpgradeText;
        private Text propulsionUpgradeText;
        private Text armorUpgradeText;
        private Text turningUpgradeText;
        private Text crewHireText;
        private Text shipLevelUpgradeText;
        private Text selectedCannonSummary;
        private Text cannonMountActionText;
        private Text cannonDamageSlotText;
        private Text cannonReloadSlotText;
        private Text cannonRangeSlotText;
        private Text cannonAmmoText;
        private CannonSlot selectedCannonSlot = CannonSlot.PortFore;
        private Text provisionText;
        private Text portFoodStockText;
        private Text portWaterStockText;
        private Text portCrewStockText;
        private Text portRepairText;
        private Text portFoodText;
        private Text portWaterText;
        private Text portBossCompassText;
        private readonly Text[] crewRoleTexts = new Text[CrewManagementModel.RoleCount];
        private readonly Text[] crewPerkTexts = new Text[CrewManagementModel.RoleCount];
        private readonly RectTransform[] cannonCrewMarkerRects = new RectTransform[ShipCustomizationModel.CannonSlotCount];
        private readonly Image[] cannonCrewMarkerRings = new Image[ShipCustomizationModel.CannonSlotCount];
        private readonly RawImage[] cannonCrewMarkerCannons = new RawImage[ShipCustomizationModel.CannonSlotCount];
        private readonly RawImage[] cannonCrewMarkerBadges = new RawImage[ShipCustomizationModel.CannonSlotCount];
        private readonly Text[] cannonCrewMarkerLabels = new Text[ShipCustomizationModel.CannonSlotCount];
        private Text selectedCannonCrewSummary;
        private Text selectedCannonCrewAssignmentText;
        private Text selectedCannonCrewPerkText;
        private CannonSlot selectedCrewCannonSlot = CannonSlot.Bow;
        private float toastUntil;
        private int displayedHull = int.MinValue;
        private int displayedMaxHull = int.MinValue;
        private int displayedGold = int.MinValue;
        private int displayedCrew = int.MinValue;
        private int displayedMass = int.MinValue;
        private int displayedCapacity = int.MinValue;
        private string displayedPrompt;
        private Font font;
        private Font englishFont;
        private Font japaneseFont;
        private Sprite panelSprite;
        private Sprite pillSprite;
        private Sprite diamondSprite;
        private Func<bool> externalModalOpen;
        private Action closeExternalModal;
        public event Action<string> Message;
        public RectTransform HullStatusCard => hullStatusCard;
        public RectTransform GoldStatusCard => goldStatusCard;
        public bool IsExternalModalOpen => externalModalOpen != null && externalModalOpen();
        public bool IsModalOpen => (menuRoot != null && menuRoot.activeSelf) || (captainLogRoot != null && captainLogRoot.activeSelf) || (portRoot != null && portRoot.activeSelf) || (shipyardRoot != null && shipyardRoot.activeSelf) || mapRoot != null || IsExternalModalOpen;

        public void RegisterExternalModal(Func<bool> isOpen, Action close)
        {
            externalModalOpen = isOpen;
            closeExternalModal = close;
        }

        public void PrepareExternalModal()
        {
            menuRoot.SetActive(false);
            captainLogRoot.SetActive(false);
            portRoot.SetActive(false);
            shipyardRoot.SetActive(false);
            inventory?.Close();
            CloseMap();
        }

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
            englishFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            japaneseFont = Font.CreateDynamicFontFromOSFont(new[] { "Yu Gothic UI", "Meiryo UI", "Meiryo", "Arial" }, 18);
            font = GameLocalization.IsJapanese && japaneseFont != null ? japaneseFont : englishFont;
            panelSprite = UiTextureFactory.LoadPanelSprite(128);
            pillSprite = UiTextureFactory.LoadPillSprite();
            diamondSprite = UiTextureFactory.LoadDiamondSprite();
            AudioListener.volume = PlayerPrefs.GetFloat("master_volume", 0.65f);
            if (AudioListener.volume > 0.001f) lastAudibleVolume = AudioListener.volume;
            BuildMenu();
            BuildHud();
            BuildCaptainLog();
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
            else if (Array.Exists(Environment.GetCommandLineArgs(), argument => argument == "--systems-preview"))
            {
                OpenPort();
                OpenShipyard();
                ShowShipyardPage(1);
            }
            else if (Array.Exists(Environment.GetCommandLineArgs(), argument => argument == "--map-preview"))
            {
                OpenMap();
                boat.IncreaseCruiseStep();
            }
            else if (Array.Exists(Environment.GetCommandLineArgs(), argument => argument == "--log-preview")) OpenCaptainLog();
            else if (Array.Exists(Environment.GetCommandLineArgs(), argument => argument == "--port-preview")) OpenPort();
            else if (Array.Exists(Environment.GetCommandLineArgs(), argument => argument == "--menu-preview")) menuRoot.SetActive(true);
        }

        private void Update()
        {
            // Esc or another panel can close the harbor UI. Never leave the boat invisibly
            // moored with W/S disabled after its docking controls are no longer visible.
            if (boat != null && boat.IsMoored && portRoot != null && !portRoot.activeSelf && shipyardRoot != null && !shipyardRoot.activeSelf)
                boat.ReleaseMooring();
            if (displayedHull != state.Hull || displayedMaxHull != state.MaxHull)
            {
                displayedHull = state.Hull;
                displayedMaxHull = state.MaxHull;
                hullValue.text = $"{state.Hull}/{state.MaxHull}";
            }
            if (displayedGold != state.Gold)
            {
                displayedGold = state.Gold;
                goldValue.text = $"{state.Gold} G";
            }
            if (displayedCrew != state.Crew)
            {
                displayedCrew = state.Crew;
                crewValue.text = state.Crew.ToString();
            }
            float loadRatio = ShipCustomizationModel.GetLoadRatio(state);
            int massDisplay = Mathf.RoundToInt(ShipCustomizationModel.GetMass(state));
            int capacityDisplay = Mathf.RoundToInt(ShipCustomizationModel.GetCapacity(state));
            if (displayedMass != massDisplay || displayedCapacity != capacityDisplay)
            {
                displayedMass = massDisplay;
                displayedCapacity = capacityDisplay;
                loadValue.text = $"{massDisplay}/{capacityDisplay}";
            }
            hullBar.fillAmount = state.Hull / (float)Mathf.Max(1, state.MaxHull);
            loadBar.fillAmount = Mathf.Clamp01(loadRatio);
            hullBar.color = state.Hull <= state.MaxHull * 0.3f ? UiTheme.Danger : UiTheme.Mint;
            loadBar.color = loadRatio >= 0.92f ? UiTheme.Danger : loadRatio >= 0.78f ? UiTheme.Warning : UiTheme.Brass;
            bool normalNavigation = (inventory == null || !inventory.IsOpen) && !IsModalOpen;
            // Hull, gold, crew and load are persistent ship state. Hiding them while
            // shopping or sorting cargo made every decision harder to evaluate.
            bool persistentStatusPanel = (inventory != null && inventory.IsOpen)
                || (portRoot != null && portRoot.activeSelf)
                || (shipyardRoot != null && shipyardRoot.activeSelf)
                || mapRoot != null
                || IsExternalModalOpen;
            bool hudVisible = normalNavigation || persistentStatusPanel;
            if (hudRoot.activeSelf != hudVisible) hudRoot.SetActive(hudVisible);
            bool dockPanelOpen = (portRoot != null && portRoot.activeSelf) || (shipyardRoot != null && shipyardRoot.activeSelf);
            Vector3 targetHudScale = Vector3.one * (dockPanelOpen ? 0.70f : 1f);
            if (hudRoot.transform.localScale != targetHudScale) hudRoot.transform.localScale = targetHudScale;
            if (persistentStatusPanel && hudRoot.transform.GetSiblingIndex() != hudRoot.transform.parent.childCount - 1) hudRoot.transform.SetAsLastSibling();
            inventory?.SetLauncherVisible(normalNavigation);
            if (compassRoot != null) compassRoot.SetActive(normalNavigation);
            if (displayedPrompt != pois.InteractionPrompt)
            {
                displayedPrompt = pois.InteractionPrompt;
                prompt.text = displayedPrompt;
            }
            bool promptVisible = normalNavigation && !string.IsNullOrEmpty(displayedPrompt);
            if (prompt.transform.parent.gameObject.activeSelf != promptVisible) prompt.transform.parent.gameObject.SetActive(promptVisible);
            toast.transform.parent.gameObject.SetActive((inventory == null || !inventory.IsOpen) && Time.unscaledTime < toastUntil);
            if (mapRoot != null && Time.unscaledTime >= nextMapLiveUpdate) UpdateLiveMap();
            if (captainLogRoot != null && captainLogRoot.activeSelf) RefreshCaptainLog();
            if (Input.GetKeyDown(KeyCode.M))
            {
                if (mapRoot != null) CloseMap();
                else { CloseAll(); OpenMap(); }
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (IsExternalModalOpen)
                {
                    closeExternalModal?.Invoke();
                    return;
                }
                bool menuOpen = menuRoot != null && menuRoot.activeSelf;
                bool anotherPanelOpen = (captainLogRoot != null && captainLogRoot.activeSelf)
                    || (portRoot != null && portRoot.activeSelf)
                    || (shipyardRoot != null && shipyardRoot.activeSelf)
                    || (inventory != null && inventory.IsOpen)
                    || mapRoot != null;
                if (menuOpen || anotherPanelOpen) CloseAll();
                else ToggleMenu();
            }
        }

        private void OnApplicationPause(bool paused) { if (paused && state != null) saves.Save(state); }
        private void OnApplicationQuit() { if (state != null) saves.Save(state); }

        public void ToggleMenu()
        {
            bool open = !menuRoot.activeSelf;
            CloseAll();
            if (open) RefreshBranchMenuState();
            menuRoot.SetActive(open);
        }

        private void BuildMenu()
        {
            menuRoot = CreateUiObject("Menu Circle Downward Controls", canvas).gameObject;
            var animatedRows = new RectTransform[8];
            float[] rowY = { -158f, -210f, -262f, -314f, -366f, -418f, -470f, -522f };
            CreateMenuBranchConnectors(menuRoot.transform, rowY);

            animatedRows[0] = CreateMenuSliderPanel(menuRoot.transform, "Volume Row", new Vector2(0f, rowY[0]), MenuGlyph.Volume, "VOLUME",
                0f, 1f, AudioListener.volume, false, SetMenuVolume, out menuVolumeSlider, out menuVolumeValue);
            animatedRows[1] = CreateMenuSliderPanel(menuRoot.transform, "Size Row", new Vector2(0f, rowY[1]), MenuGlyph.Size, "SIZE",
                WindowsOverlayController.MinimumWindowScale, WindowsOverlayController.MaximumWindowScale, overlay.WindowScale, true,
                value => overlay.SetWindowScale(value), out menuSizeSlider, out menuSizeValue);

            RectTransform mutePanel = CreateMenuActionPanel(menuRoot.transform, "Mute Row", new Vector2(0f, rowY[2]), MenuGlyph.Volume, "MUTE", ToggleMute, out menuMuteLabel);
            animatedRows[2] = mutePanel;
            RectTransform muteSlash = CreateUiObject("Mute Red Slash", mutePanel); muteSlash.anchoredPosition = new Vector2(-112f, 0f); muteSlash.sizeDelta = new Vector2(4f, 25f); muteSlash.localRotation = Quaternion.Euler(0f, 0f, -42f);
            Image muteSlashImage = muteSlash.gameObject.AddComponent<Image>(); muteSlashImage.sprite = null; muteSlashImage.color = UiTheme.Danger; muteSlashImage.raycastTarget = false;
            animatedRows[3] = CreateMenuActionPanel(menuRoot.transform, "Map Row", new Vector2(0f, rowY[3]), MenuGlyph.Map, "MAP", () => { menuRoot.SetActive(false); OpenMap(); }, out _);
            animatedRows[4] = CreateMenuActionPanel(menuRoot.transform, "Bag Row", new Vector2(0f, rowY[4]), MenuGlyph.Inventory, "BAG", () => { menuRoot.SetActive(false); inventory.Toggle(); }, out _);
            animatedRows[5] = CreateMenuActionPanel(menuRoot.transform, "Captain Log Row", new Vector2(0f, rowY[5]), MenuGlyph.Log, "CAPTAIN LOG", () => { menuRoot.SetActive(false); OpenCaptainLog(); }, out _);
            animatedRows[6] = CreateMenuActionPanel(menuRoot.transform, "Language Row", new Vector2(0f, rowY[6]), MenuGlyph.Log, "LANGUAGE", ToggleLanguage, out menuLanguageLabel);
            animatedRows[7] = CreateMenuActionPanel(menuRoot.transform, "Exit Row", new Vector2(0f, rowY[7]), MenuGlyph.Exit, "EXIT", () => { saves.Save(state); Application.Quit(); }, out Text exitLabel);
            exitLabel.color = UiTheme.Danger;

            MenuBranchAnimator animator = menuRoot.AddComponent<MenuBranchAnimator>();
            animator.Initialize(animatedRows);
            RefreshBranchMenuState();
            menuRoot.SetActive(false);
        }

        private void SetMenuVolume(float value)
        {
            AudioListener.volume = Mathf.Clamp01(value);
            if (AudioListener.volume > 0.001f) lastAudibleVolume = AudioListener.volume;
            PlayerPrefs.SetFloat("master_volume", AudioListener.volume);
            PlayerPrefs.Save();
            RefreshBranchMenuState();
        }

        private void ToggleMute()
        {
            if (AudioListener.volume > 0.001f)
            {
                lastAudibleVolume = AudioListener.volume;
                SetMenuVolume(0f);
            }
            else SetMenuVolume(Mathf.Max(0.10f, lastAudibleVolume));
        }

        private void ToggleLanguage()
        {
            GameLocalization.Toggle();
            font = GameLocalization.IsJapanese && japaneseFont != null ? japaneseFont : englishFont;
            foreach (Text text in canvas.GetComponentsInChildren<Text>(true)) text.font = font;
            foreach (LocalizedUiText localized in canvas.GetComponentsInChildren<LocalizedUiText>(true)) localized.Refresh();
            RefreshBranchMenuState();
            RefreshPortStatus();
            RefreshShipyard();
            if (captainLogRoot != null && captainLogRoot.activeSelf) RefreshCaptainLog();
            if (mapRoot != null) UpdateMapStatus();
            ShowMessage(GameLocalization.Choose("Language: English", "言語：日本語"));
        }

        private void RefreshBranchMenuState()
        {
            if (menuVolumeSlider != null) menuVolumeSlider.SetValueWithoutNotify(AudioListener.volume);
            if (menuVolumeValue != null) menuVolumeValue.text = $"{Mathf.RoundToInt(AudioListener.volume * 100f)}%";
            if (menuSizeSlider != null) menuSizeSlider.SetValueWithoutNotify(overlay.WindowScale);
            if (menuSizeValue != null) menuSizeValue.text = $"{Mathf.RoundToInt(overlay.WindowScale * 100f)}%";
            if (menuMuteLabel != null)
            {
                bool muted = AudioListener.volume <= 0.001f;
                menuMuteLabel.text = GameLocalization.Text(muted ? "UNMUTE" : "MUTE");
                menuMuteLabel.color = muted ? UiTheme.Danger : UiTheme.PrimaryText;
            }
            if (menuLanguageLabel != null)
                menuLanguageLabel.text = GameLocalization.IsJapanese ? "言語  日本語" : "LANGUAGE  ENGLISH";
        }

        private void BuildCaptainLog()
        {
            captainLogRoot = CreateUiObject("Captain Log", canvas).gameObject;
            RectTransform root = (RectTransform)captainLogRoot.transform;
            root.anchoredPosition = new Vector2(0f, -440f);
            root.sizeDelta = new Vector2(620f, 640f);
            RawImage background = captainLogRoot.AddComponent<RawImage>();
            background.texture = UiTextureFactory.LoadScreenBackground("captain_log");
            background.color = Color.white; background.raycastTarget = false;

            Text logTitle = CreateText(root, "CAPTAIN'S LOG", new Vector2(0f, 280f), new Vector2(286f, 32f), 22, TextAnchor.MiddleCenter);
            logTitle.color = Brass; UiTheme.StyleText(logTitle, 22);
            CreateButton(root, "BACK", new Vector2(-266f, -278f), CloseCaptainLog);
            CreateMapStrip(root, "Voyage Summary Header", new Vector2(-145f, 193f), new Vector2(260f, 28f));
            CreateMapStrip(root, "Enemy Ledger Header", new Vector2(145f, 193f), new Vector2(260f, 28f));
            Text voyageHeader = CreateText(root, "VOYAGE", new Vector2(-145f, 193f), new Vector2(250f, 24f), 15, TextAnchor.MiddleCenter);
            Text enemyHeader = CreateText(root, "ENEMY LEDGER", new Vector2(145f, 193f), new Vector2(250f, 24f), 15, TextAnchor.MiddleCenter);
            voyageHeader.color = enemyHeader.color = UiTheme.Brass;

            captainSummary = CreateText(root, "Captain Summary", new Vector2(-145f, 75f), new Vector2(250f, 206f), 14, TextAnchor.UpperLeft);
            enemyCatalog = CreateText(root, "Enemy Catalog", new Vector2(145f, 75f), new Vector2(250f, 206f), 13, TextAnchor.UpperLeft);
            CreateMapStrip(root, "Boss Ledger Header", new Vector2(-145f, -72f), new Vector2(260f, 28f));
            CreateMapStrip(root, "Region Atlas Header", new Vector2(145f, -72f), new Vector2(260f, 28f));
            Text bossHeader = CreateText(root, "BOSS MUTATIONS", new Vector2(-145f, -72f), new Vector2(250f, 24f), 15, TextAnchor.MiddleCenter);
            Text regionHeader = CreateText(root, "SEA ATLAS", new Vector2(145f, -72f), new Vector2(250f, 24f), 15, TextAnchor.MiddleCenter);
            bossHeader.color = regionHeader.color = UiTheme.Brass;
            bossCatalog = CreateText(root, "Boss Catalog", new Vector2(-145f, -158f), new Vector2(250f, 136f), 10, TextAnchor.UpperLeft);
            regionCatalog = CreateText(root, "Region Catalog", new Vector2(145f, -166f), new Vector2(250f, 142f), 11, TextAnchor.UpperLeft);
            foreach (Text text in new[] { captainSummary, enemyCatalog, bossCatalog, regionCatalog })
            {
                text.color = UiTheme.PrimaryText;
                text.lineSpacing = text == bossCatalog ? 0.95f : text == regionCatalog ? 1.0f : 1.12f;
                UiTheme.StyleText(text, text.fontSize);
            }
            captainLogRoot.SetActive(false);
        }

        private void OpenCaptainLog()
        {
            CloseAll();
            captainLogRoot.SetActive(true);
            RefreshCaptainLog();
        }

        private void CloseCaptainLog() => captainLogRoot.SetActive(false);

        private void RefreshCaptainLog()
        {
            CaptainRecord record = state.Captain;
            captainSummary.text = GameLocalization.Choose(
                $"SAILED        {record.DistanceSailed:0.0} nm\nCHARTED       {state.ExploredChunks.Count} sectors\nENEMIES SUNK  {record.TotalEnemiesSunk}\nBOSSES DOWN   {record.TotalBossesDefeated}\nDAMAGE DEALT  {record.DamageDealt}\nGOLD EARNED   {record.GoldEarned} G\nWRECKS        {record.WrecksSalvaged}\nTREASURES     {record.TreasuresFound}\nPORT CALLS    {record.PortCalls}",
                $"航海距離      {record.DistanceSailed:0.0} nm\n踏破海域      {state.ExploredChunks.Count}\n撃沈数        {record.TotalEnemiesSunk}\n討伐BOSS      {record.TotalBossesDefeated}\n総与ダメージ  {record.DamageDealt}\n獲得GOLD      {record.GoldEarned} G\n残骸回収      {record.WrecksSalvaged}\n宝箱発見      {record.TreasuresFound}\n寄港回数      {record.PortCalls}");

            var enemies = new StringBuilder();
            for (int i = 0; i < EnemyArchetypeModel.Count; i++)
            {
                EnemyArchetype kind = (EnemyArchetype)i;
                enemies.Append(EnemyArchetypeModel.Get(kind).ClassName.PadRight(15)).Append(" x").Append(record.GetEnemyCount(kind)).AppendLine();
            }
            enemyCatalog.text = enemies.ToString();

            var bosses = new StringBuilder();
            for (int i = 1; i < BossMutationModel.BossCount; i++)
            {
                BossKind boss = (BossKind)i;
                bosses.Append(EnemyIdentityModel.GetName(0, boss).PadRight(14)).Append(" x").Append(record.GetBossCount(boss)).AppendLine();
            }
            bosses.AppendLine(GameLocalization.Choose("-- MUTATIONS --", "-- 変異記録 --"));
            for (int i = 1; i < BossMutationModel.MutationCount; i++)
            {
                BossMutation mutation = (BossMutation)i;
                bosses.Append(BossMutationModel.GetLabel(mutation).PadRight(14)).Append(" x").Append(record.GetMutationCount(mutation)).AppendLine();
            }
            bossCatalog.text = bosses.ToString();

            var regions = new StringBuilder();
            for (int i = 0; i < SeaRegionModel.Count; i++)
            {
                SeaRegionKind kind = (SeaRegionKind)i;
                SeaRegionProfile profile = SeaRegionModel.Get(kind);
                regions.Append(record.HasDiscoveredRegion(kind)
                    ? GameLocalization.Choose("[CHARTED] ", "[踏破済] ")
                    : "[   ?   ] ").Append(profile.Name).AppendLine();
            }
            regionCatalog.text = regions.ToString();
        }

        private void BuildHud()
        {
            RectTransform dashboard = CreateUiObject("Ship Dashboard", canvas);
            dashboard.anchoredPosition = new Vector2(0f, -155f);
            dashboard.sizeDelta = new Vector2(UiLayoutMetrics.HudDashboardWidth, 88f);
            hudRoot = dashboard.gameObject;
            float spacing = UiLayoutMetrics.HudCardSpacing;
            hullValue = CreateStatusCard(dashboard, "HULL", new Vector2(-spacing * 1.5f, 0f), UiTheme.Mint, out hullBar);
            hullStatusCard = hullValue.transform.parent.parent as RectTransform;
            goldValue = CreateStatusCard(dashboard, "GOLD", new Vector2(-spacing * 0.5f, 0f), UiTheme.Brass, out _);
            goldStatusCard = goldValue.transform.parent.parent as RectTransform;
            crewValue = CreateStatusCard(dashboard, "CREW", new Vector2(spacing * 0.5f, 0f), UiTheme.SecondaryText, out _);
            loadValue = CreateStatusCard(dashboard, "LOAD", new Vector2(spacing * 1.5f, 0f), UiTheme.Brass, out loadBar);

            prompt = CreatePillText(canvas, "Context Action", new Vector2(0f, -586f), new Vector2(430f, 34f), 17);
            prompt.color = UiTheme.PrimaryText;
            UiTheme.StyleText(prompt, 18);
            toast = CreatePillText(canvas, "Event Message", new Vector2(190f, -660f), new Vector2(330f, 40f), 15);
            toast.color = UiTheme.Brass;
            UiTheme.StyleText(toast, 18);
            toast.transform.parent.gameObject.SetActive(false);
        }

        private Text CreateStatusCard(Transform parent, string label, Vector2 position, Color accent, out Image meter)
        {
            RectTransform card = CreateUiObject(label + " Status Card", parent); card.anchoredPosition = position; card.sizeDelta = new Vector2(UiLayoutMetrics.HudCardWidth, UiLayoutMetrics.HudCardHeight);
            Image cardFill = card.gameObject.AddComponent<Image>(); cardFill.sprite = UiTextureFactory.LoadConceptSprite("Chrome", "button_fill"); cardFill.color = Color.white; cardFill.raycastTarget = false;
            RectTransform cardFrame = CreateUiObject(label + " Status Frame", card); cardFrame.sizeDelta = card.sizeDelta;
            Image frameImage = cardFrame.gameObject.AddComponent<Image>(); frameImage.sprite = UiTextureFactory.LoadConceptSprite("Chrome", "service_button", 22f, true); frameImage.type = Image.Type.Sliced; frameImage.color = Color.white; frameImage.raycastTarget = false;
            RectTransform iconRect = CreateUiObject(label + " Authored Icon", card); iconRect.anchoredPosition = new Vector2(-62f, 0f); iconRect.sizeDelta = Vector2.one * 38f;
            RawImage icon = iconRect.gameObject.AddComponent<RawImage>(); icon.texture = UiTextureFactory.LoadConceptTexture("Navigation", $"status_{label.ToLowerInvariant()}"); icon.raycastTarget = false;
            RectTransform content = CreateUiObject(label + " Safe Content", card); content.anchoredPosition = new Vector2(22f, 0f); content.sizeDelta = new Vector2(UiLayoutMetrics.HudCardSafeWidth, 62f);
            Text caption = CreateText(content, label, new Vector2(0f, 18f), new Vector2(UiLayoutMetrics.HudCardSafeWidth, 17f), 12, TextAnchor.MiddleCenter); caption.color = accent; UiTheme.StyleText(caption, 12);
            Text value = CreateText(content, label + " Value", new Vector2(0f, -4f), new Vector2(UiLayoutMetrics.HudCardSafeWidth, 25f), 18, TextAnchor.MiddleCenter); value.color = UiTheme.PrimaryText; UiTheme.StyleText(value, 18);
            value.resizeTextForBestFit = true; value.resizeTextMinSize = 11; value.resizeTextMaxSize = 18;
            RectTransform bar = CreateUiObject(label + " Meter", content); bar.anchoredPosition = new Vector2(0f, -26f); bar.sizeDelta = new Vector2(UiLayoutMetrics.HudCardSafeWidth - 4f, 8f);
            Image track = bar.gameObject.AddComponent<Image>(); track.sprite = UiTextureFactory.LoadConceptSprite("Chrome", "slider_rail", 14f); track.type = Image.Type.Sliced; track.color = Color.white; track.raycastTarget = false;
            RectTransform fill = CreateUiObject(label + " Meter Fill", bar); fill.anchorMin = new Vector2(0f, 0.5f); fill.anchorMax = new Vector2(1f, 0.5f); fill.sizeDelta = new Vector2(-8f, 5f); fill.anchoredPosition = Vector2.zero;
            meter = fill.gameObject.AddComponent<Image>(); meter.type = Image.Type.Filled; meter.fillMethod = Image.FillMethod.Horizontal; meter.color = accent; meter.raycastTarget = false;
            if (label != "HULL" && label != "LOAD") bar.gameObject.SetActive(false);
            return value;
        }

        private void BuildPortPanel()
        {
            portRoot = CreateUiObject("Harbor Services", canvas).gameObject;
            RectTransform rect = (RectTransform)portRoot.transform;
            rect.anchoredPosition = new Vector2(0f, -440f);
            rect.sizeDelta = new Vector2(620f, 640f);
            RawImage back = portRoot.AddComponent<RawImage>();
            back.texture = UiTextureFactory.LoadScreenBackground("harbor"); back.color = Color.white; back.raycastTarget = false;

            RectTransform board = CreateUiObject("Harbor Readable Service Board", portRoot.transform);
            board.anchoredPosition = new Vector2(0f, -12f);
            board.sizeDelta = new Vector2(390f, 474f);
            Image boardFill = board.gameObject.AddComponent<Image>(); boardFill.sprite = null; boardFill.color = new Color(0.008f, 0.035f, 0.052f, 0.82f); boardFill.raycastTarget = false;

            Text title = CreateText(board, "HARBOR SERVICES", new Vector2(0f, 190f), new Vector2(250f, 30f), 21, TextAnchor.MiddleCenter); title.color = Brass; UiTheme.StyleText(title, 21);
            AddFlatDivider(board, "Harbor Title Divider", new Vector2(0f, 171f), 270f, new Color(0.31f, 0.76f, 0.74f, 0.55f));
            portFoodStockText = CreatePortStockChip(board, "FOOD", "food", new Vector2(-118f, 150f));
            portWaterStockText = CreatePortStockChip(board, "WATER", "water", new Vector2(0f, 150f));
            portCrewStockText = CreatePortStockChip(board, "CREW", "hire_crew", new Vector2(118f, 150f));
            provisionText = CreateText(board, "CREW CONSUMPTION", new Vector2(0f, 116f), new Vector2(334f, 22f), 12, TextAnchor.MiddleCenter);
            provisionText.color = UiTheme.SecondaryText; UiTheme.StyleText(provisionText, 12);

            portRepairText = CreatePortServiceButton(board, "REPAIR  +10%  20G", new Vector2(0f, 82f), Repair, "repair").GetComponentInChildren<Text>();
            portFoodText = CreatePortServiceButton(board, "BUY FOOD  +10  18G", new Vector2(0f, 38f), () => BuyProvision(true), "food").GetComponentInChildren<Text>();
            portWaterText = CreatePortServiceButton(board, "BUY WATER  +10  14G", new Vector2(0f, -6f), () => BuyProvision(false), "water").GetComponentInChildren<Text>();
            portBossCompassText = CreatePortServiceButton(board, $"BOSS COMPASS  {BossCompassModel.PurchaseCost}G", new Vector2(0f, -50f), BuyBossCompass, "boss_compass").GetComponentInChildren<Text>();
            CreatePortServiceButton(board, "SHIPYARD  CUSTOMIZE", new Vector2(0f, -94f), OpenShipyard, "shipyard");
            CreatePortServiceButton(board, "DEPART HARBOR", new Vector2(0f, -158f), () => DepartPort(portRoot), "sail", 220f, true);
            portRoot.SetActive(false);
        }

        private void BuildShipyardPanel()
        {
            shipyardRoot = CreateUiObject("Shipyard Customization", canvas).gameObject;
            RectTransform rect = (RectTransform)shipyardRoot.transform;
            rect.anchoredPosition = new Vector2(0f, -440f);
            rect.sizeDelta = new Vector2(620f, 640f);
            RawImage back = shipyardRoot.AddComponent<RawImage>(); back.texture = UiTextureFactory.LoadScreenBackground("shipyard"); back.color = Color.white; back.raycastTarget = false;
            CreateText(shipyardRoot.transform, "SHIPYARD", new Vector2(0f, 230f), new Vector2(260f, 30f), 22, TextAnchor.MiddleCenter).color = Brass;
            shipyardSummary = CreateText(shipyardRoot.transform, "Shipyard Summary", new Vector2(0f, 196f), new Vector2(552f, 44f), 12, TextAnchor.MiddleCenter);
            shipyardSummary.color = new Color(0.78f, 0.91f, 0.90f, 1f);
            shipyardSummary.resizeTextForBestFit = true; shipyardSummary.resizeTextMinSize = 9; shipyardSummary.resizeTextMaxSize = 12;

            shipyardTabBackplates[0] = CreateSizedButton(shipyardRoot.transform, "GUN DECK", new Vector2(-172f, 156f), new Vector2(160f, 36f), () => ShowShipyardPage(0)).GetComponent<Image>();
            shipyardTabBackplates[1] = CreateSizedButton(shipyardRoot.transform, "SYSTEMS", new Vector2(0f, 156f), new Vector2(160f, 36f), () => ShowShipyardPage(1)).GetComponent<Image>();
            shipyardTabBackplates[2] = CreateSizedButton(shipyardRoot.transform, "CREW", new Vector2(172f, 156f), new Vector2(160f, 36f), () => ShowShipyardPage(2)).GetComponent<Image>();

            shipyardGunsPage = CreateUiObject("Gun Deck Page", shipyardRoot.transform).gameObject;
            CreateShipyardDeckCannon(CannonSlot.Bow, new Vector2(0f, 50f), 0f);
            CreateShipyardDeckCannon(CannonSlot.PortFore, new Vector2(-68f, 22f), 90f);
            CreateShipyardDeckCannon(CannonSlot.StarboardFore, new Vector2(68f, 22f), -90f);
            CreateShipyardDeckCannon(CannonSlot.PortAft, new Vector2(-70f, -47f), 90f);
            CreateShipyardDeckCannon(CannonSlot.StarboardAft, new Vector2(70f, -47f), -90f);
            CreateShipyardDeckCannon(CannonSlot.Stern, new Vector2(0f, -88f), 180f);
            CreateCannonSlotButton(CannonSlot.Bow, new Vector2(0f, 106f));
            CreateCannonSlotButton(CannonSlot.PortFore, new Vector2(-184f, 35f));
            CreateCannonSlotButton(CannonSlot.StarboardFore, new Vector2(184f, 35f));
            CreateCannonSlotButton(CannonSlot.PortAft, new Vector2(-184f, -45f));
            CreateCannonSlotButton(CannonSlot.StarboardAft, new Vector2(184f, -45f));
            CreateCannonSlotButton(CannonSlot.Stern, new Vector2(0f, -128f));
            selectedCannonSummary = CreateText(shipyardGunsPage.transform, "Selected Cannon Summary", new Vector2(-69f, -202f), new Vector2(350f, 42f), 12, TextAnchor.MiddleCenter);
            selectedCannonSummary.color = UiTheme.PrimaryText; UiTheme.StyleText(selectedCannonSummary, 12);
            selectedCannonSummary.resizeTextForBestFit = true; selectedCannonSummary.resizeTextMinSize = 9; selectedCannonSummary.resizeTextMaxSize = 12;
            cannonMountActionText = CreateSizedButton(shipyardGunsPage.transform, "MOUNT / STORE", new Vector2(221f, -202f), new Vector2(148f, 40f), () => ToggleCannon(selectedCannonSlot)).GetComponentInChildren<Text>();
            cannonDamageSlotText = CreateSizedButton(shipyardGunsPage.transform, "DMG", new Vector2(-210f, -250f), new Vector2(128f, 40f), () => UpgradeSelectedCannon(0)).GetComponentInChildren<Text>();
            cannonReloadSlotText = CreateSizedButton(shipyardGunsPage.transform, "RLD", new Vector2(-70f, -250f), new Vector2(128f, 40f), () => UpgradeSelectedCannon(1)).GetComponentInChildren<Text>();
            cannonRangeSlotText = CreateSizedButton(shipyardGunsPage.transform, "RNG", new Vector2(70f, -250f), new Vector2(128f, 40f), () => UpgradeSelectedCannon(2)).GetComponentInChildren<Text>();
            cannonAmmoText = CreateSizedButton(shipyardGunsPage.transform, "AMMO", new Vector2(210f, -250f), new Vector2(128f, 40f), CycleSelectedCannonRound).GetComponentInChildren<Text>();

            shipyardSystemsPage = CreateUiObject("Ship Systems Page", shipyardRoot.transform).gameObject;
            capacityUpgradeText = CreateCompactButton(shipyardSystemsPage.transform, "CAPACITY", new Vector2(-120f, 110f), UpgradeCapacity);
            propulsionUpgradeText = CreateCompactButton(shipyardSystemsPage.transform, "PROPULSION", new Vector2(120f, 110f), UpgradeEngine);
            armorUpgradeText = CreateCompactButton(shipyardSystemsPage.transform, "ARMOR", new Vector2(-120f, 55f), UpgradeArmor);
            turningUpgradeText = CreateCompactButton(shipyardSystemsPage.transform, "TURNING", new Vector2(120f, 55f), UpgradeTurning);
            crewHireText = CreateCompactButton(shipyardSystemsPage.transform, "HIRE CREW", new Vector2(-120f, 0f), HireCrew);
            shipLevelUpgradeText = CreateCompactButton(shipyardSystemsPage.transform, "SHIP LEVEL", new Vector2(120f, 0f), UpgradeShipLevel);

            crewManagementPage = CreateUiObject("Crew Management Page", shipyardRoot.transform).gameObject;
            Text crewHelp = CreateText(crewManagementPage.transform, "Crew Assignment Help", new Vector2(0f, 137f), new Vector2(540f, 24f), 12, TextAnchor.MiddleCenter);
            crewHelp.text = GameLocalization.Choose("SELECT A HARDPOINT ON THE DECK PLAN, THEN ASSIGN ITS GUNNER", "船型図の砲座を選び、担当砲員を配置します");
            crewHelp.color = UiTheme.SecondaryText; UiTheme.StyleText(crewHelp, 12);
            Text cannonTitle = CreateText(crewManagementPage.transform, "CANNON CREW", new Vector2(-150f, 112f), new Vector2(280f, 22f), 14, TextAnchor.MiddleCenter);
            cannonTitle.text = GameLocalization.Choose("CANNON CREW", "砲台担当"); cannonTitle.color = UiTheme.Brass; UiTheme.StyleText(cannonTitle, 14);
            Text shipCrewTitle = CreateText(crewManagementPage.transform, "SHIP CREW", new Vector2(145f, 112f), new Vector2(270f, 22f), 14, TextAnchor.MiddleCenter);
            shipCrewTitle.text = GameLocalization.Choose("SHIP CREW", "船内配置"); shipCrewTitle.color = UiTheme.Brass; UiTheme.StyleText(shipCrewTitle, 14);

            RectTransform deckPlan = CreateUiObject("Crew Gun Deck Plan", crewManagementPage.transform);
            deckPlan.anchoredPosition = new Vector2(-150f, -10f);
            deckPlan.sizeDelta = new Vector2(280f, 238f);
            Image deckFill = deckPlan.gameObject.AddComponent<Image>();
            deckFill.sprite = UiTextureFactory.LoadConceptSprite("Chrome", "button_fill", 18f);
            deckFill.type = Image.Type.Sliced;
            deckFill.color = new Color(0.012f, 0.050f, 0.065f, 0.96f);
            deckFill.raycastTarget = false;
            RectTransform schematicRect = CreateUiObject("Top Down Hull Diagram", deckPlan);
            schematicRect.anchoredPosition = new Vector2(0f, 0f);
            schematicRect.sizeDelta = new Vector2(132f, 184f);
            RawImage schematic = schematicRect.gameObject.AddComponent<RawImage>();
            schematic.texture = UiTextureFactory.LoadConceptTexture("Port", "hull_schematic");
            schematic.color = new Color(0.72f, 0.85f, 0.80f, 0.52f);
            schematic.raycastTarget = false;
            CreateCrewDeckMarker(deckPlan, CannonSlot.Bow, new Vector2(0f, 78f), 0f);
            CreateCrewDeckMarker(deckPlan, CannonSlot.PortFore, new Vector2(-58f, 36f), 90f);
            CreateCrewDeckMarker(deckPlan, CannonSlot.StarboardFore, new Vector2(58f, 36f), -90f);
            CreateCrewDeckMarker(deckPlan, CannonSlot.PortAft, new Vector2(-58f, -31f), 90f);
            CreateCrewDeckMarker(deckPlan, CannonSlot.StarboardAft, new Vector2(58f, -31f), -90f);
            CreateCrewDeckMarker(deckPlan, CannonSlot.Stern, new Vector2(0f, -80f), 180f);

            selectedCannonCrewSummary = CreateText(crewManagementPage.transform, "Selected Cannon Crew Summary", new Vector2(-150f, -147f), new Vector2(280f, 38f), 12, TextAnchor.MiddleCenter);
            selectedCannonCrewSummary.color = UiTheme.PrimaryText; UiTheme.StyleText(selectedCannonCrewSummary, 12);
            selectedCannonCrewSummary.resizeTextForBestFit = true; selectedCannonCrewSummary.resizeTextMinSize = 9; selectedCannonCrewSummary.resizeTextMaxSize = 12;
            selectedCannonCrewAssignmentText = CreateCrewOptionButton(crewManagementPage.transform, "ASSIGN GUNNER", new Vector2(-216f, -188f), new Vector2(140f, 38f), () => ToggleCannonCrew(selectedCrewCannonSlot));
            selectedCannonCrewPerkText = CreateCrewOptionButton(crewManagementPage.transform, "PERK", new Vector2(-79f, -188f), new Vector2(128f, 38f), () => CycleCannonCrewPerk(selectedCrewCannonSlot));

            CreateCrewRoleRow(CrewRole.Helm, 76f);
            CreateCrewRoleRow(CrewRole.Sails, 20f);
            CreateCrewRoleRow(CrewRole.Anchor, -36f);
            CreateCrewRoleRow(CrewRole.Repairer, -92f);

            CreateCompactButton(shipyardRoot.transform, "BACK TO PORT", new Vector2(-120f, -300f), () => { shipyardRoot.SetActive(false); portRoot.SetActive(true); RefreshPortStatus(); });
            CreateCompactButton(shipyardRoot.transform, "SAIL", new Vector2(120f, -300f), () => DepartPort(shipyardRoot));
            shipyardRoot.SetActive(false);
            ShowShipyardPage(0);
        }

        private void CreateCrewRoleRow(CrewRole role, float y)
        {
            int index = (int)role;
            RectTransform row = CreateUiObject(role + " Crew Row", crewManagementPage.transform); row.anchoredPosition = new Vector2(145f, y); row.sizeDelta = new Vector2(270f, 46f);
            Image fill = row.gameObject.AddComponent<Image>(); fill.sprite = UiTextureFactory.LoadConceptSprite("Chrome", "button_fill", 18f); fill.type = Image.Type.Sliced; fill.color = new Color(0.02f, 0.075f, 0.095f, 0.92f); fill.raycastTarget = false;
            AddButtonIcon(row, GetCrewRoleIcon(role), new Vector2(-112f, 0f), 28f);
            crewRoleTexts[index] = CreateText(row, role.ToString().ToUpperInvariant(), new Vector2(-61f, 0f), new Vector2(68f, 36f), 13, TextAnchor.MiddleLeft);
            crewRoleTexts[index].resizeTextForBestFit = true; crewRoleTexts[index].resizeTextMinSize = 9; crewRoleTexts[index].resizeTextMaxSize = 13;
            CreateCrewStepButton(row, "-", new Vector2(-14f, 0f), () => ChangeCrewRole(role, -1));
            CreateCrewStepButton(row, "+", new Vector2(24f, 0f), () => ChangeCrewRole(role, 1));
            crewPerkTexts[index] = CreateCrewOptionButton(row, "PERK", new Vector2(88f, 0f), new Vector2(86f, 34f), () => CycleRolePerk(role));
        }

        private void CreateCrewDeckMarker(Transform parent, CannonSlot slot, Vector2 position, float rotation)
        {
            int index = (int)slot;
            RectTransform marker = CreateUiObject($"{slot} Crew Deck Marker", parent);
            marker.anchoredPosition = position;
            marker.sizeDelta = new Vector2(48f, 48f);
            cannonCrewMarkerRects[index] = marker;
            Image ring = marker.gameObject.AddComponent<Image>();
            ring.sprite = UiTextureFactory.LoadShipyardMountRing();
            ring.color = Color.white;
            cannonCrewMarkerRings[index] = ring;
            Button button = marker.gameObject.AddComponent<Button>();
            button.targetGraphic = ring;
            button.onClick.AddListener(() => SelectCrewCannonSlot(slot));

            RectTransform cannonRect = CreateUiObject($"{slot} Crew Deck Cannon", marker);
            cannonRect.sizeDelta = new Vector2(35f, 35f);
            cannonRect.localRotation = Quaternion.Euler(0f, 0f, rotation);
            RawImage cannon = cannonRect.gameObject.AddComponent<RawImage>();
            cannon.texture = UiTextureFactory.LoadShipyardCannonOverlay();
            cannon.raycastTarget = false;
            cannonCrewMarkerCannons[index] = cannon;

            RectTransform badgeRect = CreateUiObject($"{slot} Assigned Gunner Badge", marker);
            badgeRect.anchoredPosition = new Vector2(18f, 17f);
            badgeRect.sizeDelta = new Vector2(20f, 20f);
            RawImage badge = badgeRect.gameObject.AddComponent<RawImage>();
            badge.texture = UiTextureFactory.LoadFramelessPortIcon("hire_crew");
            badge.color = UiTheme.Mint;
            badge.raycastTarget = false;
            cannonCrewMarkerBadges[index] = badge;

            Text label = CreateText(marker, ShipCustomizationModel.GetDefinition(slot).ShortName, new Vector2(0f, -27f), new Vector2(48f, 15f), 10, TextAnchor.MiddleCenter);
            label.color = UiTheme.Brass; UiTheme.StyleText(label, 10); label.raycastTarget = false;
            cannonCrewMarkerLabels[index] = label;
        }

        private void SelectCrewCannonSlot(CannonSlot slot)
        {
            selectedCrewCannonSlot = slot;
            RefreshShipyard();
        }

        private Text CreateCrewOptionButton(Transform parent, string label, Vector2 position, Vector2 size, Action action)
        {
            RectTransform rect = CreateUiObject(label + " Crew Option", parent); rect.anchoredPosition = position; rect.sizeDelta = size;
            Image image = rect.gameObject.AddComponent<Image>(); image.sprite = null; image.color = new Color(0.025f, 0.105f, 0.125f, 0.98f);
            Outline outline = rect.gameObject.AddComponent<Outline>(); outline.effectColor = new Color(0.63f, 0.43f, 0.15f, 0.90f); outline.effectDistance = new Vector2(1f, -1f);
            Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(() => action());
            Text text = CreateText(rect, label, Vector2.zero, size - new Vector2(6f, 4f), 12, TextAnchor.MiddleCenter);
            text.color = UiTheme.PrimaryText; UiTheme.StyleText(text, 12);
            text.resizeTextForBestFit = true; text.resizeTextMinSize = 8; text.resizeTextMaxSize = 12;
            return text;
        }

        private void CreateCrewStepButton(Transform parent, string label, Vector2 position, Action action)
        {
            RectTransform rect = CreateUiObject(label + " Crew Step", parent); rect.anchoredPosition = position; rect.sizeDelta = new Vector2(34f, 32f);
            Image image = rect.gameObject.AddComponent<Image>(); image.sprite = null; image.color = new Color(0.025f, 0.105f, 0.125f, 0.98f);
            Outline outline = rect.gameObject.AddComponent<Outline>(); outline.effectColor = new Color(0.63f, 0.43f, 0.15f, 0.90f); outline.effectDistance = new Vector2(1f, -1f);
            Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(() => action());
            Text text = CreateText(rect, label, Vector2.zero, rect.sizeDelta, 20, TextAnchor.MiddleCenter); text.color = UiTheme.PrimaryText; UiTheme.StyleText(text, 20);
        }

        private void CreateCannonSlotButton(CannonSlot slot, Vector2 position)
        {
            RectTransform rect = CreateUiObject($"{slot} Hardpoint", shipyardGunsPage.transform);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(148f, 64f);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = UiTextureFactory.LoadConceptSprite("Chrome", "button_fill", 18f);
            image.type = Image.Type.Sliced;
            image.color = new Color(0.035f, 0.085f, 0.11f, 0.92f);
            cannonSlotBackplates[(int)slot] = image;
            cannonSlotRects[(int)slot] = rect;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => SelectCannonSlot(slot));
            RectTransform frameRect = CreateUiObject($"{slot} Hardpoint Border", rect);
            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.offsetMin = Vector2.zero;
            frameRect.offsetMax = Vector2.zero;
            Image frame = frameRect.gameObject.AddComponent<Image>();
            frame.sprite = UiTextureFactory.LoadConceptSprite("Chrome", "tab_frame", 14f, true);
            frame.type = Image.Type.Sliced;
            frame.raycastTarget = false;
            cannonSlotFrames[(int)slot] = frame;
            RectTransform iconRect = CreateUiObject($"{slot} Card Cannon", rect);
            iconRect.anchoredPosition = new Vector2(-48f, 0f);
            iconRect.sizeDelta = new Vector2(44f, 44f);
            RawImage icon = iconRect.gameObject.AddComponent<RawImage>();
            icon.texture = UiTextureFactory.LoadShipyardCannonOverlay();
            icon.raycastTarget = false;
            cannonSlotCardIcons[(int)slot] = icon;
            Text label = CreateText(rect, ShipCustomizationModel.GetDefinition(slot).ShortName, new Vector2(22f, 0f), new Vector2(94f, 56f), 13, TextAnchor.MiddleCenter);
            label.color = UiTheme.PrimaryText;
            UiTheme.StyleText(label, 13);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 9;
            label.resizeTextMaxSize = 13;
            cannonSlotTexts[(int)slot] = label;
        }

        private void CreateShipyardDeckCannon(CannonSlot slot, Vector2 position, float rotation)
        {
            RectTransform ringRect = CreateUiObject($"{slot} Deck Mount", shipyardGunsPage.transform);
            ringRect.anchoredPosition = position;
            ringRect.sizeDelta = new Vector2(64f, 64f);
            Image ring = ringRect.gameObject.AddComponent<Image>();
            ring.sprite = UiTextureFactory.LoadShipyardMountRing();
            ring.preserveAspect = true;
            ring.raycastTarget = false;
            shipyardDeckMountRings[(int)slot] = ring;
            RectTransform rect = CreateUiObject($"{slot} Equipped Cannon", shipyardGunsPage.transform);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(54f, 54f);
            rect.localEulerAngles = new Vector3(0f, 0f, rotation);
            RawImage image = rect.gameObject.AddComponent<RawImage>();
            image.texture = UiTextureFactory.LoadShipyardCannonOverlay();
            image.color = Color.white;
            image.raycastTarget = false;
            shipyardDeckCannons[(int)slot] = image;
        }

        private void SelectCannonSlot(CannonSlot slot)
        {
            selectedCannonSlot = slot;
            RefreshShipyard();
        }

        private Text CreateCompactButton(Transform parent, string label, Vector2 position, Action action)
            => CreateSizedButton(parent, label, position, new Vector2(226f, 42f), action).GetComponentInChildren<Text>();

        private void ShowShipyardPage(int page)
        {
            if (shipyardGunsPage != null) shipyardGunsPage.SetActive(page == 0);
            if (shipyardSystemsPage != null) shipyardSystemsPage.SetActive(page == 1);
            if (crewManagementPage != null) crewManagementPage.SetActive(page == 2);
            for (int i = 0; i < shipyardTabBackplates.Length; i++)
                if (shipyardTabBackplates[i] != null) shipyardTabBackplates[i].color = i == page
                    ? new Color(0.20f, 0.60f, 0.55f, 1f)
                    : Color.white;
        }

        private void OpenPort()
        {
            CloseAll();
            boat.HoldAtMooring();
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
                if (state.IsCannonCrewAssigned(slot)) state.UnassignCannonCrew(slot);
                state.CannonMountMask &= ~bit;
                state.SpareCannons++;
                ShowMessage($"{ShipCustomizationModel.GetDefinition(slot).ShortName} CANNON STORED");
            }
            else
            {
                if (!ShipCustomizationModel.IsHardpointUnlocked(state, slot)) { ShowMessage("HARDPOINT LOCKED BY SHIP LEVEL"); return; }
                if (ShipCustomizationModel.GetInstalledCannonCount(state) >= ShipCustomizationModel.GetCannonCapacity(state)) { ShowMessage("SHIP LEVEL CANNON LIMIT"); return; }
                bool mountingStoredCannon = state.SpareCannons > 0;
                if (!ShipCustomizationModel.CanAddMass(state, ShipCustomizationModel.CannonMass))
                {
                    ShowMessage(GameLocalization.Choose("CAPACITY EXCEEDED — UPGRADE THE HULL", "積載超過 — 船体の積載上限を強化してください"));
                    return;
                }
                if (mountingStoredCannon)
                {
                    if (!ShipCustomizationModel.TryMountStoredCannon(state, slot))
                    {
                        ShowMessage(GameLocalization.Choose("CANNON COULD NOT BE MOUNTED", "砲台を搭載できませんでした"));
                        return;
                    }
                }
                else
                {
                    if (state.Gold < ShipCustomizationModel.CannonPrice) { ShowMessage($"CANNON NEEDS {ShipCustomizationModel.CannonPrice}G"); return; }
                    state.Gold -= ShipCustomizationModel.CannonPrice;
                    state.CannonMountMask |= bit;
                }
                ShowMessage(GameLocalization.Choose(
                    $"CANNON MOUNTED — {ShipCustomizationModel.GetDefinition(slot).ArcName} ARC",
                    $"砲台を搭載 — {ShipCustomizationModel.GetDefinition(slot).ArcName} 射界"));
            }
            boat.RefreshCustomizationVisual();
            RefreshShipyard();
            saves.Save(state);
        }

        private void UpgradeSelectedCannon(int system)
        {
            if (!ShipCustomizationModel.HasCannon(state, selectedCannonSlot))
            {
                ShowMessage(GameLocalization.Choose("Mount a cannon before upgrading it.", "先に砲台を搭載してください"));
                return;
            }
            int level = system == 0 ? state.GetCannonDamageLevel(selectedCannonSlot)
                : system == 1 ? state.GetCannonReloadLevel(selectedCannonSlot)
                : state.GetCannonRangeLevel(selectedCannonSlot);
            if (level >= CannonUpgradeModel.MaxSlotUpgrade)
            {
                ShowMessage(GameLocalization.Choose("This hardpoint is already at maximum.", "この砲台は最大強化済みです"));
                return;
            }
            int cost = CannonUpgradeModel.GetUpgradeCost(level);
            if (state.Gold < cost)
            {
                ShowMessage(GameLocalization.Choose($"Upgrade needs {cost}G", $"強化には {cost}G 必要です"));
                return;
            }
            state.Gold -= cost;
            if (system == 0) state.SetCannonDamageLevel(selectedCannonSlot, level + 1);
            else if (system == 1) state.SetCannonReloadLevel(selectedCannonSlot, level + 1);
            else state.SetCannonRangeLevel(selectedCannonSlot, level + 1);
            RefreshShipyard(); saves.Save(state);
            ShowMessage(GameLocalization.Choose("Hardpoint upgraded.", "砲台を強化しました"));
        }

        private void CycleSelectedCannonRound()
        {
            if (!ShipCustomizationModel.HasCannon(state, selectedCannonSlot))
            {
                ShowMessage(GameLocalization.Choose("Mount a cannon before selecting ammunition.", "砲弾を選ぶには砲台を搭載してください"));
                return;
            }
            CannonRoundKind next = CannonUpgradeModel.NextRound(state.GetCannonRound(selectedCannonSlot));
            state.SetCannonRound(selectedCannonSlot, next);
            RefreshShipyard(); saves.Save(state);
            ShowMessage(GameLocalization.Text(CannonUpgradeModel.GetProfile(next).Name));
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

        private void ToggleCannonCrew(CannonSlot slot)
        {
            state.EnsureCannonCrewLayout();
            if (!ShipCustomizationModel.HasCannon(state, slot))
            {
                ShowMessage(GameLocalization.Choose("Mount this cannon before assigning a gunner.", "砲員を配置する前に砲台を搭載してください"));
                return;
            }
            bool changed = state.IsCannonCrewAssigned(slot)
                ? state.UnassignCannonCrew(slot)
                : state.TryAssignCannonCrew(slot);
            if (!changed)
            {
                ShowMessage(GameLocalization.Choose("No unassigned crew available.", "未配置の船員がいません"));
                return;
            }
            RefreshShipyard(); saves.Save(state);
        }

        private void CycleCannonCrewPerk(CannonSlot slot)
        {
            state.EnsureCannonCrewLayout();
            if (!state.IsCannonCrewAssigned(slot))
            {
                ShowMessage(GameLocalization.Choose("Assign a gunner to this cannon first.", "先にこの砲台へ砲員を配置してください"));
                return;
            }
            if (!state.TryCycleCannonCrewPerk(slot, out CrewPerk perk, out PerkRank rank))
            {
                ShowMessage(GameLocalization.Choose("No compatible boss perk is owned.", "装備できる砲員PERKを所持していません"));
                return;
            }
            RefreshShipyard(); saves.Save(state);
            ShowMessage(perk == CrewPerk.None
                ? GameLocalization.Choose("CANNON PERK REMOVED", "砲台担当のPERKを解除")
                : $"{ShipCustomizationModel.GetDefinition(slot).ShortName} — {CrewManagementModel.GetPerkName(perk)} {PerkRankModel.GetLabel(rank)}");
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
            state.EnsureCannonCrewLayout();
            float mass = ShipCustomizationModel.GetMass(state);
            float capacity = ShipCustomizationModel.GetCapacity(state);
            ShipTierDefinition tier = ShipProgressionModel.Get(state.ShipLevel);
            shipyardSummary.text = GameLocalization.Choose(
                $"L{state.ShipLevel + 1} {tier.Name}   HULL {state.MaxHull}   MASS {mass:0.0}/{capacity:0.0}   SPEED {ShipCustomizationModel.GetSpeedMultiplier(state) * 100f:0}%\nCREW {state.Crew}   FREE {CrewManagementModel.GetUnassigned(state)}   GUNS {ShipCustomizationModel.GetInstalledCannonCount(state)}/{tier.MaxCannons}  STORED {state.SpareCannons}   CAP {tier.UpgradeCap}",
                $"L{state.ShipLevel + 1} {tier.Name}   HULL {state.MaxHull}   重量 {mass:0.0}/{capacity:0.0}   速力 {ShipCustomizationModel.GetSpeedMultiplier(state) * 100f:0}%\n船員 {state.Crew}   未配置 {CrewManagementModel.GetUnassigned(state)}   砲台 {ShipCustomizationModel.GetInstalledCannonCount(state)}/{tier.MaxCannons}  予備 {state.SpareCannons}   上限 {tier.UpgradeCap}");
            for (int i = 0; i < cannonSlotTexts.Length; i++)
            {
                CannonSlot slot = (CannonSlot)i;
                CannonSlotDefinition definition = ShipCustomizationModel.GetDefinition(slot);
                bool unlocked = ShipCustomizationModel.IsHardpointUnlocked(state, slot);
                bool mounted = ShipCustomizationModel.HasCannon(state, slot);
                string status = mounted ? (GameLocalization.IsJapanese ? "搭載" : "ARMED") : unlocked ? (GameLocalization.IsJapanese ? "空き" : "EMPTY") : "LOCKED";
                cannonSlotTexts[i].text = mounted
                    ? $"{definition.ShortName}  {status}\nD{state.GetCannonDamageLevel(slot)} R{state.GetCannonReloadLevel(slot)} G{state.GetCannonRangeLevel(slot)}"
                    : $"{definition.ShortName}\n{status}";
                cannonSlotTexts[i].color = slot == selectedCannonSlot ? UiTheme.Mint
                    : mounted ? new Color(1f, 0.76f, 0.28f) : new Color(0.62f, 0.76f, 0.76f);
                if (cannonSlotBackplates[i] != null)
                    cannonSlotBackplates[i].color = slot == selectedCannonSlot
                        ? new Color(0.05f, 0.30f, 0.31f, 0.98f)
                        : mounted ? new Color(0.16f, 0.105f, 0.035f, 0.96f)
                        : unlocked ? new Color(0.025f, 0.075f, 0.095f, 0.92f)
                        : new Color(0.018f, 0.028f, 0.035f, 0.78f);
                if (cannonSlotFrames[i] != null)
                    cannonSlotFrames[i].color = slot == selectedCannonSlot
                        ? new Color(0.34f, 1f, 0.82f, 1f)
                        : mounted ? new Color(1f, 0.69f, 0.22f, 0.95f)
                        : unlocked ? new Color(0.54f, 0.67f, 0.66f, 0.74f)
                        : new Color(0.34f, 0.37f, 0.37f, 0.42f);
                if (cannonSlotCardIcons[i] != null)
                    cannonSlotCardIcons[i].color = mounted ? Color.white
                        : unlocked ? new Color(0.55f, 0.72f, 0.72f, 0.52f)
                        : new Color(0.28f, 0.32f, 0.32f, 0.24f);
                if (shipyardDeckMountRings[i] != null)
                    shipyardDeckMountRings[i].color = slot == selectedCannonSlot
                        ? new Color(0.38f, 1f, 0.83f, 1f)
                        : mounted ? Color.white
                        : unlocked ? new Color(0.50f, 0.66f, 0.66f, 0.45f)
                        : new Color(0.26f, 0.28f, 0.28f, 0.20f);
                if (shipyardDeckCannons[i] != null) shipyardDeckCannons[i].gameObject.SetActive(mounted);
                if (cannonSlotRects[i] != null) cannonSlotRects[i].localScale = slot == selectedCannonSlot ? Vector3.one * 1.08f : Vector3.one;
                if (cannonCrewMarkerRings[i] != null)
                {
                    bool assigned = state.IsCannonCrewAssigned(slot);
                    bool selectedForCrew = slot == selectedCrewCannonSlot;
                    cannonCrewMarkerRings[i].color = selectedForCrew
                        ? new Color(0.38f, 1f, 0.83f, 1f)
                        : assigned ? new Color(0.28f, 0.94f, 0.82f, 1f)
                        : mounted ? new Color(1f, 0.69f, 0.22f, 0.95f)
                        : unlocked ? new Color(0.52f, 0.68f, 0.68f, 0.52f)
                        : new Color(0.25f, 0.28f, 0.28f, 0.22f);
                    cannonCrewMarkerCannons[i].gameObject.SetActive(mounted);
                    cannonCrewMarkerCannons[i].color = assigned ? Color.white : new Color(0.76f, 0.70f, 0.54f, 0.78f);
                    cannonCrewMarkerBadges[i].gameObject.SetActive(assigned);
                    cannonCrewMarkerLabels[i].color = selectedForCrew ? UiTheme.Mint
                        : assigned ? UiTheme.Mint : mounted ? UiTheme.Brass : UiTheme.SecondaryText;
                    cannonCrewMarkerRects[i].localScale = selectedForCrew ? Vector3.one * 1.16f : Vector3.one;
                }
            }

            bool selectedCrewUnlocked = ShipCustomizationModel.IsHardpointUnlocked(state, selectedCrewCannonSlot);
            bool selectedCrewMounted = ShipCustomizationModel.HasCannon(state, selectedCrewCannonSlot);
            bool selectedCrewAssigned = state.IsCannonCrewAssigned(selectedCrewCannonSlot);
            CannonSlotDefinition crewDefinition = ShipCustomizationModel.GetDefinition(selectedCrewCannonSlot);
            CrewPerk selectedCrewPerk = state.GetCannonCrewPerk(selectedCrewCannonSlot);
            string crewMountStatus = !selectedCrewUnlocked ? "LOCKED"
                : !selectedCrewMounted ? GameLocalization.Choose("NO CANNON", "砲台なし")
                : selectedCrewAssigned ? GameLocalization.Choose("GUNNER ON DUTY", "砲員 配置済")
                : GameLocalization.Choose("GUNNER VACANT", "砲員 未配置");
            if (selectedCannonCrewSummary != null)
                selectedCannonCrewSummary.text = GameLocalization.Choose(
                    $"SELECTED  {crewDefinition.ShortName}  •  {crewDefinition.ArcName}\n{crewMountStatus}",
                    $"選択中  {crewDefinition.ShortName}  •  {crewDefinition.ArcName}\n{crewMountStatus}");
            if (selectedCannonCrewAssignmentText != null)
            {
                selectedCannonCrewAssignmentText.text = !selectedCrewMounted
                    ? (!selectedCrewUnlocked ? "LOCKED" : GameLocalization.Choose("NO CANNON", "砲台なし"))
                    : selectedCrewAssigned ? GameLocalization.Choose("REMOVE GUNNER", "配置解除") : GameLocalization.Choose("ASSIGN GUNNER", "砲員を配置");
                selectedCannonCrewAssignmentText.color = selectedCrewAssigned ? UiTheme.Mint : UiTheme.PrimaryText;
            }
            if (selectedCannonCrewPerkText != null)
            {
                selectedCannonCrewPerkText.text = !selectedCrewAssigned
                    ? "PERK —"
                    : selectedCrewPerk == CrewPerk.None
                        ? GameLocalization.Choose("NO PERK", "PERKなし")
                        : $"{CrewManagementModel.GetPerkName(selectedCrewPerk)}  {PerkRankModel.GetLabel(state.GetCannonCrewPerkRank(selectedCrewCannonSlot))}";
                selectedCannonCrewPerkText.color = selectedCrewAssigned && selectedCrewPerk != CrewPerk.None ? UiTheme.Brass : UiTheme.SecondaryText;
            }
            bool selectedMounted = ShipCustomizationModel.HasCannon(state, selectedCannonSlot);
            CannonRoundProfile round = CannonUpgradeModel.GetProfile(state.GetCannonRound(selectedCannonSlot));
            if (selectedCannonSummary != null)
                selectedCannonSummary.text = selectedMounted
                    ? GameLocalization.Choose(
                        $"SELECTED  {ShipCustomizationModel.GetDefinition(selectedCannonSlot).ShortName}  •  {GameLocalization.Text(round.Name)}\nDMG {CannonUpgradeModel.GetDamage(state, selectedCannonSlot)}   RNG {CannonUpgradeModel.GetRange(state, selectedCannonSlot):0.0}   RLD {CannonUpgradeModel.GetReloadSeconds(state, selectedCannonSlot):0.00}s",
                        $"選択中  {ShipCustomizationModel.GetDefinition(selectedCannonSlot).ShortName}  •  {GameLocalization.Text(round.Name)}\n威力 {CannonUpgradeModel.GetDamage(state, selectedCannonSlot)}   射程 {CannonUpgradeModel.GetRange(state, selectedCannonSlot):0.0}   装填 {CannonUpgradeModel.GetReloadSeconds(state, selectedCannonSlot):0.00}秒")
                    : GameLocalization.Choose($"SELECTED  {ShipCustomizationModel.GetDefinition(selectedCannonSlot).ShortName}  •  EMPTY", $"選択中  {ShipCustomizationModel.GetDefinition(selectedCannonSlot).ShortName}  •  空き砲座");
            if (cannonMountActionText != null) cannonMountActionText.text = selectedMounted
                ? GameLocalization.Choose("STORE", "倉庫へ")
                : state.SpareCannons > 0
                    ? GameLocalization.Choose($"MOUNT x{state.SpareCannons}", $"搭載 予備{state.SpareCannons}")
                    : GameLocalization.Choose($"BUY {ShipCustomizationModel.CannonPrice}G", $"購入 {ShipCustomizationModel.CannonPrice}G");
            if (cannonDamageSlotText != null) cannonDamageSlotText.text = CannonSlotUpgradeLabel("DMG", state.GetCannonDamageLevel(selectedCannonSlot));
            if (cannonReloadSlotText != null) cannonReloadSlotText.text = CannonSlotUpgradeLabel("RLD", state.GetCannonReloadLevel(selectedCannonSlot));
            if (cannonRangeSlotText != null) cannonRangeSlotText.text = CannonSlotUpgradeLabel("RNG", state.GetCannonRangeLevel(selectedCannonSlot));
            if (cannonAmmoText != null) cannonAmmoText.text = $"{GameLocalization.Text("AMMO")}\n{GameLocalization.Text(round.Name)}";
            int cap = ShipCustomizationModel.GetUpgradeCap(state);
            capacityUpgradeText.text = UpgradeLabel("CAPACITY", state.CapacityLevel, ShipCustomizationModel.GetCapacityUpgradeCost(state.CapacityLevel), cap);
            propulsionUpgradeText.text = UpgradeLabel("PROPULSION", state.EngineLevel, CruiseModel.GetUpgradeCost(state.EngineLevel), cap);
            armorUpgradeText.text = UpgradeLabel("ARMOR", state.ArmorLevel, ShipCustomizationModel.GetArmorUpgradeCost(state.ArmorLevel), cap);
            turningUpgradeText.text = UpgradeLabel("TURNING", state.TurningLevel, ShipCustomizationModel.GetTurningUpgradeCost(state.TurningLevel), cap);
            crewHireText.text = GameLocalization.Choose($"HIRE CREW  {ShipCustomizationModel.GetCrewHireCost(state)}G", $"船員を雇う  {ShipCustomizationModel.GetCrewHireCost(state)}G");
            ShipTierDefinition next = ShipProgressionModel.Get(Mathf.Min(state.ShipLevel + 1, ShipProgressionModel.TierCount - 1));
            shipLevelUpgradeText.text = ShipProgressionModel.IsMax(state.ShipLevel) ? "SHIP LEVEL  MAX" : $"SHIP → {next.Name}  {next.UpgradeCost}G";
            for (int i = 1; i < CrewManagementModel.RoleCount; i++)
            {
                CrewRole role = (CrewRole)i;
                crewRoleTexts[i].text = $"{GetCrewRoleLabel(role)}  {state.GetRoleCrew(role)}";
                int equipped = state.GetEquippedPerkTotal(role);
                crewPerkTexts[i].text = equipped <= 0
                    ? GameLocalization.Choose("NO PERK", "PERKなし")
                    : $"{PerkRankModel.GetLabel(CrewManagementModel.GetHighestEquippedRank(state, role))}  x{equipped}";
            }
        }

        private static string UpgradeLabel(string name, int level, int cost, int cap)
            => level >= cap ? $"{GameLocalization.Text(name)}  CAP {cap}" : $"{GameLocalization.Text(name)}  L{level}→{level + 1}  {cost}G";

        private static string CannonSlotUpgradeLabel(string name, int level)
            => level >= CannonUpgradeModel.MaxSlotUpgrade
                ? $"{GameLocalization.Text(name)}\nMAX"
                : $"{GameLocalization.Text(name)}  L{level}→{level + 1}\n{CannonUpgradeModel.GetUpgradeCost(level)}G";

        private static string GetCrewRoleLabel(CrewRole role)
        {
            if (!GameLocalization.IsJapanese) return role.ToString().ToUpperInvariant();
            return role switch
            {
                CrewRole.Cannons => "砲員",
                CrewRole.Helm => "操舵",
                CrewRole.Sails => "帆走",
                CrewRole.Anchor => "錨",
                CrewRole.Repairer => "修理工",
                _ => role.ToString().ToUpperInvariant()
            };
        }

        private static Texture2D GetCrewRoleIcon(CrewRole role) => role switch
        {
            CrewRole.Helm => UiTextureFactory.LoadPortIcon("turning"),
            CrewRole.Sails => UiTextureFactory.LoadFramelessPortIcon("sail"),
            CrewRole.Anchor => UiTextureFactory.LoadPortIcon("capacity"),
            CrewRole.Repairer => UiTextureFactory.LoadFramelessPortIcon("repair"),
            _ => UiTextureFactory.LoadFramelessPortIcon("hire_crew")
        };

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

        private void BuyBossCompass()
        {
            if (state.BossCompassOwned) { ShowMessage("BOSS COMPASS ALREADY INSTALLED"); return; }
            if (state.Gold < BossCompassModel.PurchaseCost) { ShowMessage($"BOSS COMPASS NEEDS {BossCompassModel.PurchaseCost}G"); return; }
            state.Gold -= BossCompassModel.PurchaseCost;
            state.BossCompassOwned = true;
            RefreshPortStatus();
            saves.Save(state);
            ShowMessage("BOSS COMPASS INSTALLED — FOLLOW THE AMBER NEEDLE");
        }

        private void RefreshPortStatus()
        {
            if (portFoodStockText != null) portFoodStockText.text = $"{GameLocalization.Text("FOOD")}  {state.Food}";
            if (portWaterStockText != null) portWaterStockText.text = $"{GameLocalization.Text("WATER")}  {state.Water}";
            if (portCrewStockText != null) portCrewStockText.text = $"{GameLocalization.Text("CREW")}  {state.Crew}";
            if (provisionText != null)
                provisionText.text = GameLocalization.Choose($"CREW USE  {ProvisionModel.GetUnitsPerInterval(state.Crew)} FOOD + WATER / 2 MIN", $"船員消費  食料・水 各{ProvisionModel.GetUnitsPerInterval(state.Crew)} / 2分");
            GetPortRepairQuote(out int repairAmount, out int repairCost);
            if (portRepairText != null) portRepairText.text = repairAmount <= 0 ? GameLocalization.Choose("REPAIR  FULL", "船体修理  不要") : GameLocalization.Choose($"REPAIR  +{repairAmount}  {repairCost}G", $"船体修理  +{repairAmount}  {repairCost}G");
            if (portFoodText != null) portFoodText.text = GameLocalization.Choose("BUY FOOD  +10  18G", "食料  +10  18G");
            if (portWaterText != null) portWaterText.text = GameLocalization.Choose("BUY WATER  +10  14G", "飲料水  +10  14G");
            if (portBossCompassText != null) portBossCompassText.text = state.BossCompassOwned
                ? GameLocalization.Choose("BOSS COMPASS  INSTALLED", "BOSS COMPASS  装備済み")
                : $"BOSS COMPASS  {BossCompassModel.PurchaseCost}G";
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
            if (shipyardRoot != null) RefreshShipyard();
            ShowMessage($"MAX SPEED {CruiseModel.GetMaxSpeed(state.EngineLevel):0.00}  {CruiseModel.GetMaxStep(state.EngineLevel)} STEPS");
            saves.Save(state);
        }

        private void OpenMap()
        {
            if (mapRoot != null) return;
            mapChartCenter = boat.LogicalPosition;
            RectTransform panel = CreateUiObject("Transparent Navigation Chart", canvas);
            mapRoot = panel.gameObject;
            // Keep the frameless live chart centered. The circular texture remains
            // transparent outside its rim so navigation is still visible around it.
            panel.anchoredPosition = new Vector2(UiLayoutMetrics.NavigationChartCenterX, -465f);
            panel.sizeDelta = new Vector2(380f, 490f);

            CreateMapStrip(panel, "Chart Title Strip", new Vector2(22f, 218f), new Vector2(222f, 34f));
            Text title = CreateText(panel, "Navigation Chart Title", new Vector2(22f, 218f), new Vector2(214f, 30f), 18, TextAnchor.MiddleCenter);
            title.text = GameLocalization.Choose("NAVIGATION CHART", "航海図");
            title.color = UiTheme.Brass; UiTheme.StyleText(title, 18);
            CreateSizedButton(panel, "BACK", new Vector2(-142f, 218f), new Vector2(72f, 34f), CloseMap);
            localZoomText = CreateSizedButton(panel, "LOCAL", new Vector2(-70f, 182f), new Vector2(128f, 32f), () => SetMapZoom(MapZoom.Local)).GetComponentInChildren<Text>();
            wideZoomText = CreateSizedButton(panel, "WIDE", new Vector2(70f, 182f), new Vector2(128f, 32f), () => SetMapZoom(MapZoom.Wide)).GetComponentInChildren<Text>();

            RectTransform clip = CreateUiObject("Live Circular Chart", panel); clip.anchoredPosition = new Vector2(0f, -5f); clip.sizeDelta = new Vector2(338f, 338f);
            RectTransform mapRect = CreateUiObject("Explored Waters", clip); mapRect.sizeDelta = new Vector2(338f, 338f);
            mapImage = mapRect.gameObject.AddComponent<RawImage>(); mapImage.color = new Color(1f, 1f, 1f, 0.92f); mapImage.raycastTarget = false;
            RectTransform markersRect = CreateUiObject("Chart Markers", clip);
            markersRect.sizeDelta = new Vector2(338f, 338f);
            mapMarkersRoot = markersRect.gameObject;

            CreateCardinalLabel(panel, "N", new Vector2(0f, 160f));
            CreateCardinalLabel(panel, "E", new Vector2(176f, -5f));
            CreateCardinalLabel(panel, "S", new Vector2(0f, -170f));
            CreateCardinalLabel(panel, "W", new Vector2(-176f, -5f));
            CreateMapStrip(panel, "Live Course Status Strip", new Vector2(0f, -187f), new Vector2(366f, 28f));
            mapStatus = CreateText(panel, "Chart Status", new Vector2(0f, -187f), new Vector2(354f, 24f), 13, TextAnchor.MiddleCenter);
            mapStatus.color = UiTheme.Brass; UiTheme.StyleText(mapStatus, 13);
            BuildMapLegend(panel);
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
            string region = SeaRegionModel.At(state.WorldSeed, boat.LogicalPosition).Name;
            mapStatus.text = GameLocalization.Choose(
                $"{region}  |  {(mapZoom == MapZoom.Local ? "LOCAL" : "WIDE")}  |  POS {chunkX:+0;-0;0},{chunkY:+0;-0;0}  |  HDG {boat.HeadingDegrees:000}°  |  {SpeedGaugeModel.GetMotionLabel(boat.CruiseStep, boat.MaxCruiseStep, boat.Speed, boat.TargetSpeed)}",
                $"{region}  |  {(mapZoom == MapZoom.Local ? "周辺" : "広域")}  |  海域 {chunkX:+0;-0;0},{chunkY:+0;-0;0}  |  方位 {boat.HeadingDegrees:000}°  |  {SpeedGaugeModel.GetMotionLabel(boat.CruiseStep, boat.MaxCruiseStep, boat.Speed, boat.TargetSpeed)}");
            mapStatus.resizeTextForBestFit = true;
            mapStatus.resizeTextMinSize = 9;
            mapStatus.resizeTextMaxSize = 13;
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
            Text text = CreateText(parent, label, position, new Vector2(28f, 24f), 15, TextAnchor.MiddleCenter);
            text.color = label == "N" ? UiTheme.Brass : UiTheme.PrimaryText; UiTheme.StyleText(text, 15);
        }

        private void BuildMapLegend(Transform parent)
        {
            PoiKind[] kinds = { PoiKind.Port, PoiKind.Enemy, PoiKind.Wreck, PoiKind.Treasure };
            string[] labels = GameLocalization.IsJapanese ? new[] { "港", "敵船", "残骸", "宝" } : new[] { "PORT", "ENEMY", "WRECK", "TREASURE" };
            for (int i = 0; i < kinds.Length; i++)
            {
                float x = -132f + i * 88f;
                RectTransform icon = CreateUiObject(labels[i] + " Legend Icon", parent); icon.anchoredPosition = new Vector2(x - 20f, -221f); icon.sizeDelta = Vector2.one * 27f;
                RawImage badge = icon.gameObject.AddComponent<RawImage>(); badge.texture = UiTextureFactory.LoadPoiBadge(kinds[i]); badge.raycastTarget = false;
                Text label = CreateText(parent, labels[i], new Vector2(x + 15f, -221f), new Vector2(54f, 22f), 11, TextAnchor.MiddleLeft);
                label.color = UiTheme.PrimaryText; UiTheme.StyleText(label, 11);
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
            captainLogRoot.SetActive(false);
            portRoot.SetActive(false);
            shipyardRoot.SetActive(false);
            inventory?.Close();
            CloseMap();
            closeExternalModal?.Invoke();
        }

        private void Autosave() => saves.Save(state);
        private void ShowMessage(string value)
        {
            if (Message != null) { Message.Invoke(value); return; }
            toast.text = value;
            toastUntil = Time.unscaledTime + 5f;
            toast.transform.parent.gameObject.SetActive(true);
        }

        private Button CreateButton(Transform parent, string label, Vector2 position, Action action)
        {
            RectTransform rect = CreateUiObject(label, parent);
            rect.anchoredPosition = position; rect.sizeDelta = new Vector2(80f, 80f);
            MenuGlyph glyph = label == "SAVE" ? MenuGlyph.Save
                : label == "BAG" ? MenuGlyph.Inventory
                : label == "LOG" ? MenuGlyph.Log
                : label == "BACK" ? MenuGlyph.Back
                : label == "EXIT" || label == "SAIL" ? MenuGlyph.Exit
                : MenuGlyph.Map;
            RawImage image = rect.gameObject.AddComponent<RawImage>(); image.texture = label == "SAIL" ? UiTextureFactory.LoadPortIcon("sail") : UiTextureFactory.LoadMenuButton(glyph, 128); image.color = Color.white;
            Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(() => action());
            Text caption = CreateText(rect, label + " Label", new Vector2(0f, -28f), new Vector2(56f, 16f), 10, TextAnchor.MiddleCenter);
            caption.text = GameLocalization.Text(label); caption.color = UiTheme.PrimaryText; UiTheme.StyleText(caption, 10); LocalizedUiText.Bind(caption, label);
            return button;
        }

        private RectTransform CreateMenuBranchPanel(Transform parent, string name, Vector2 position, Vector2 size, MenuGlyph glyph, string title)
        {
            RectTransform panel = CreateUiObject(name, parent); panel.anchoredPosition = position; panel.sizeDelta = size;
            Image background = panel.gameObject.AddComponent<Image>(); background.sprite = UiTextureFactory.LoadConceptSprite("Chrome", "button_fill"); background.color = Color.white;
            RectTransform frame = CreateUiObject(name + " Brass Frame", panel); frame.sizeDelta = size;
            Image frameImage = frame.gameObject.AddComponent<Image>(); frameImage.sprite = UiTextureFactory.LoadConceptSprite("Chrome", "service_button", 22f, true); frameImage.type = Image.Type.Sliced; frameImage.raycastTarget = false;
            RectTransform glyphRect = CreateUiObject(name + " Frameless Icon", panel); glyphRect.anchoredPosition = new Vector2(-112f, 0f); glyphRect.sizeDelta = Vector2.one * 32f;
            RawImage glyphImage = glyphRect.gameObject.AddComponent<RawImage>(); glyphImage.texture = UiTextureFactory.LoadFramelessMenuGlyph(glyph, 48); glyphImage.raycastTarget = false;
            Text heading = CreateText(panel, title, new Vector2(13f, 0f), new Vector2(210f, 36f), 15, TextAnchor.MiddleCenter);
            heading.color = UiTheme.PrimaryText; UiTheme.StyleText(heading, 15); LocalizedUiText.Bind(heading, title);
            return panel;
        }

        private RectTransform CreateMenuActionPanel(Transform parent, string name, Vector2 position, MenuGlyph glyph, string label, Action action, out Text labelText)
        {
            RectTransform panel = CreateMenuBranchPanel(parent, name, position, new Vector2(276f, 48f), glyph, label);
            labelText = panel.Find(label)?.GetComponent<Text>();
            RectTransform hitArea = CreateUiObject(name + " Full Row Hit Area", panel);
            hitArea.anchorMin = Vector2.zero; hitArea.anchorMax = Vector2.one;
            hitArea.offsetMin = Vector2.zero; hitArea.offsetMax = Vector2.zero;
            Image hitImage = hitArea.gameObject.AddComponent<Image>(); hitImage.sprite = null; hitImage.color = new Color(1f, 1f, 1f, 0.001f); hitImage.raycastTarget = true;
            Button button = hitArea.gameObject.AddComponent<Button>(); button.targetGraphic = hitImage; button.onClick.AddListener(() => action());
            return panel;
        }

        private RectTransform CreateMenuSliderPanel(Transform parent, string name, Vector2 position, MenuGlyph glyph, string label,
            float minimum, float maximum, float value, bool commitOnRelease, Action<float> changed, out Slider slider, out Text valueText)
        {
            RectTransform panel = CreateMenuBranchPanel(parent, name, position, new Vector2(276f, 48f), glyph, label);
            RectTransform heading = panel.Find(label) as RectTransform;
            heading.anchoredPosition = new Vector2(-70f, 0f); heading.sizeDelta = new Vector2(72f, 34f);
            RectTransform bar = CreateUiObject(name + " Bar", panel); bar.anchoredPosition = new Vector2(27f, 0f); bar.sizeDelta = new Vector2(112f, 10f);
            Image rail = bar.gameObject.AddComponent<Image>(); rail.sprite = UiTextureFactory.LoadConceptSprite("Chrome", "slider_rail", 14f); rail.type = Image.Type.Sliced; rail.color = Color.white;
            slider = bar.gameObject.AddComponent<Slider>(); slider.minValue = minimum; slider.maxValue = maximum; slider.value = value;
            RectTransform fill = CreateUiObject("Fill", bar); fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one; fill.offsetMin = Vector2.zero; fill.offsetMax = Vector2.zero;
            Image fillImage = fill.gameObject.AddComponent<Image>(); fillImage.color = Brass; fillImage.raycastTarget = false; slider.fillRect = fill;
            RectTransform handle = CreateUiObject("Handle", bar); handle.sizeDelta = new Vector2(16f, 16f);
            Image handleImage = handle.gameObject.AddComponent<Image>(); handleImage.sprite = diamondSprite; handleImage.color = Color.white; slider.handleRect = handle; slider.targetGraphic = handleImage;
            valueText = CreateText(panel, name + " Value", new Vector2(109f, 0f), new Vector2(46f, 30f), 13, TextAnchor.MiddleCenter);
            valueText.color = UiTheme.Brass; UiTheme.StyleText(valueText, 13);
            Text capturedValueText = valueText;
            if (commitOnRelease)
            {
                DeferredSliderCommit deferred = bar.gameObject.AddComponent<DeferredSliderCommit>();
                deferred.Initialize(slider, current => capturedValueText.text = $"{Mathf.RoundToInt(current * 100f)}%", changed);
            }
            else
            {
                slider.onValueChanged.AddListener(current =>
                {
                    capturedValueText.text = $"{Mathf.RoundToInt(current * 100f)}%";
                    changed(current);
                });
                capturedValueText.text = $"{Mathf.RoundToInt(value * 100f)}%";
            }
            return panel;
        }

        private static void CreateMenuBranchConnectors(Transform parent, float[] rowY)
        {
            if (rowY == null || rowY.Length == 0) return;
            float top = -132f;
            float bottom = rowY[rowY.Length - 1];
            AddMenuConnector(parent, "Menu Brass Downward Spine", new Vector2(0f, (top + bottom) * 0.5f), new Vector2(4f, top - bottom + 4f));
        }

        private static void AddMenuConnector(Transform parent, string name, Vector2 position, Vector2 size)
        {
            RectTransform rect = CreateUiObject(name, parent); rect.anchoredPosition = position; rect.sizeDelta = size;
            Image image = rect.gameObject.AddComponent<Image>(); image.sprite = null; image.color = new Color(0.86f, 0.56f, 0.15f, 0.95f); image.raycastTarget = false;
            rect.SetAsFirstSibling();
        }

        private Button CreateWideButton(Transform parent, string label, Vector2 position, Action action, string iconName = null)
        {
            RectTransform rect = CreateUiObject(label, parent); rect.anchoredPosition = position; rect.sizeDelta = new Vector2(310f, 50f);
            Image image = rect.gameObject.AddComponent<Image>(); image.sprite = UiTextureFactory.LoadConceptSprite("Chrome", "service_button", 22f); image.type = Image.Type.Sliced; image.color = Color.white;
            Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(() => action());
            Text text = CreateText(rect, label, new Vector2(26f, 0f), new Vector2(238f, 46f), 16, TextAnchor.MiddleCenter); text.color = UiTheme.PrimaryText; UiTheme.StyleText(text, 16); LocalizedUiText.Bind(text, label);
            text.resizeTextForBestFit = true; text.resizeTextMinSize = 13; text.resizeTextMaxSize = 16;
            Texture2D icon = string.IsNullOrEmpty(iconName) ? GetActionIcon(label) : UiTextureFactory.LoadPortIcon(iconName);
            if (icon != null) AddButtonIcon(rect, icon, new Vector2(-126f, 0f), UiLayoutMetrics.PrimaryIcon);
            return button;
        }

        private Text CreatePortStockChip(Transform parent, string label, string iconName, Vector2 position)
        {
            RectTransform chip = CreateUiObject(label + " Stock Chip", parent); chip.anchoredPosition = position; chip.sizeDelta = new Vector2(112f, 38f);
            Image frame = chip.gameObject.AddComponent<Image>(); frame.sprite = null; frame.color = new Color(0.025f, 0.100f, 0.125f, 1f); frame.raycastTarget = false;
            AddButtonIcon(chip, UiTextureFactory.LoadFramelessPortIcon(iconName), new Vector2(-37f, 0f), 26f);
            Text text = CreateText(chip, label, new Vector2(15f, 0f), new Vector2(72f, 30f), 12, TextAnchor.MiddleCenter);
            text.color = UiTheme.PrimaryText; UiTheme.StyleText(text, 12); text.resizeTextForBestFit = true; text.resizeTextMinSize = 10; text.resizeTextMaxSize = 12;
            return text;
        }

        private Button CreatePortServiceButton(Transform parent, string label, Vector2 position, Action action, string iconName, float width = UiLayoutMetrics.PortServiceButtonWidth, bool emphasized = false)
        {
            RectTransform rect = CreateUiObject(label, parent); rect.anchoredPosition = position; rect.sizeDelta = new Vector2(width, 43f);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = UiTextureFactory.LoadConceptSprite("Chrome", "service_button", 22f, true);
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(() => action());
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = emphasized ? new Color(1.08f, 1.03f, 0.90f, 1f) : new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.76f, 0.90f, 0.88f, 1f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            float iconX = -width * 0.5f + 27f;
            AddButtonIcon(rect, UiTextureFactory.LoadFramelessPortIcon(iconName), new Vector2(iconX, 0f), 34f);
            Text text = CreateText(rect, label, new Vector2(18f, 0f), new Vector2(width - 72f, 37f), 15, TextAnchor.MiddleCenter);
            text.color = UiTheme.PrimaryText; UiTheme.StyleText(text, 15); text.resizeTextForBestFit = true; text.resizeTextMinSize = 12; text.resizeTextMaxSize = 15;
            LocalizedUiText.Bind(text, label);
            return button;
        }

        private static void AddFlatDivider(Transform parent, string name, Vector2 position, float width, Color color)
        {
            RectTransform divider = CreateUiObject(name, parent); divider.anchoredPosition = position; divider.sizeDelta = new Vector2(width, 1f);
            Image image = divider.gameObject.AddComponent<Image>(); image.sprite = null; image.color = color; image.raycastTarget = false;
        }

        private Button CreateSizedButton(Transform parent, string label, Vector2 position, Vector2 size, Action action)
        {
            RectTransform rect = CreateUiObject(label, parent); rect.anchoredPosition = position; rect.sizeDelta = size;
            Image image = rect.gameObject.AddComponent<Image>(); image.sprite = UiTextureFactory.LoadConceptSprite("Chrome", "service_button", 22f, true); image.type = Image.Type.Sliced; image.color = Color.white;
            Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(() => action());
            Texture2D icon = GetActionIcon(label);
            float iconSize = Mathf.Min(30f, size.y - 8f);
            float textCenterX = icon != null ? 15f : 0f;
            float textWidth = Mathf.Max(28f, size.x - (icon != null ? 54f : 12f));
            Text text = CreateText(rect, label, new Vector2(textCenterX, 0f), new Vector2(textWidth, size.y - 4f), 14, TextAnchor.MiddleCenter);
            text.color = UiTheme.PrimaryText; UiTheme.StyleText(text, 14); LocalizedUiText.Bind(text, label);
            text.resizeTextForBestFit = true; text.resizeTextMinSize = 8; text.resizeTextMaxSize = 14;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            if (icon != null) AddButtonIcon(rect, icon, new Vector2(-size.x * 0.5f + 20f, 0f), iconSize);
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
            if (label.Contains("GUN DECK")) return UiTextureFactory.LoadShipyardCannonOverlay();
            if (label.Contains("SYSTEMS")) return UiTextureFactory.LoadPortIcon("systems");
            if (label.Contains("GUN")) return UiTextureFactory.LoadPortIcon("gun_upgrade");
            if (label.Contains("CREW")) return UiTextureFactory.LoadFramelessPortIcon("hire_crew");
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
