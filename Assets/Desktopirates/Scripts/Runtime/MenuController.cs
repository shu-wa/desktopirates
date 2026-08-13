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
        private GameObject portRoot;
        private GameObject shipyardRoot;
        private Text hud;
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
        }

        private void Update()
        {
            hud.text = $"HULL {state.Hull}/{state.MaxHull}  {state.Gold}G  CREW {state.Crew}  CARGO {state.TotalSalvageCount}";
            bool normalHud = (inventory == null || !inventory.IsOpen) && !IsModalOpen;
            hud.transform.parent.gameObject.SetActive(normalHud);
            if (compassRoot != null) compassRoot.SetActive(normalHud);
            prompt.text = pois.InteractionPrompt;
            prompt.transform.parent.gameObject.SetActive(normalHud && !string.IsNullOrEmpty(prompt.text));
            toast.transform.parent.gameObject.SetActive((inventory == null || !inventory.IsOpen) && Time.unscaledTime < toastUntil);
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
            hud = CreatePillText(canvas, "Ship Status", new Vector2(0f, -315f), new Vector2(390f, 28f), 15);
            hud.color = new Color(0.96f, 0.83f, 0.56f, 0.95f);
            prompt = CreatePillText(canvas, "Context Action", new Vector2(0f, -695f), new Vector2(360f, 30f), 17);
            prompt.color = Color.white;
            toast = CreatePillText(canvas, "Event Message", new Vector2(0f, -352f), new Vector2(380f, 32f), 16);
            toast.color = new Color(1f, 0.78f, 0.32f, 1f);
            toast.transform.parent.gameObject.SetActive(false);
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
            CreateText(shipyardRoot.transform, "SHIPYARD", new Vector2(0f, 214f), new Vector2(260f, 30f), 22, TextAnchor.MiddleCenter).color = Brass;
            shipyardSummary = CreateText(shipyardRoot.transform, "Shipyard Summary", new Vector2(0f, 178f), new Vector2(470f, 36f), 12, TextAnchor.MiddleCenter);
            shipyardSummary.color = new Color(0.78f, 0.91f, 0.90f, 1f);

            CreateText(shipyardRoot.transform, "GUN DECK — CLICK TO INSTALL / STORE", new Vector2(0f, 142f), new Vector2(420f, 22f), 13, TextAnchor.MiddleCenter).color = Brass;
            CreateCannonSlotButton(CannonSlot.Bow, new Vector2(0f, 108f));
            CreateCannonSlotButton(CannonSlot.PortFore, new Vector2(-130f, 70f));
            CreateCannonSlotButton(CannonSlot.StarboardFore, new Vector2(130f, 70f));
            CreateCannonSlotButton(CannonSlot.PortAft, new Vector2(-130f, 32f));
            CreateCannonSlotButton(CannonSlot.StarboardAft, new Vector2(130f, 32f));
            CreateCannonSlotButton(CannonSlot.Stern, new Vector2(0f, -6f));

            capacityUpgradeText = CreateCompactButton("CAPACITY", new Vector2(-132f, -55f), UpgradeCapacity);
            propulsionUpgradeText = CreateCompactButton("PROPULSION", new Vector2(132f, -55f), UpgradeEngine);
            armorUpgradeText = CreateCompactButton("ARMOR", new Vector2(-132f, -94f), UpgradeArmor);
            turningUpgradeText = CreateCompactButton("TURNING", new Vector2(132f, -94f), UpgradeTurning);
            gunUpgradeText = CreateCompactButton("GUNS", new Vector2(-132f, -133f), UpgradeCannon);
            crewHireText = CreateCompactButton("HIRE CREW", new Vector2(132f, -133f), HireCrew);
            CreateCompactButton("BACK TO PORT", new Vector2(-132f, -188f), () => { shipyardRoot.SetActive(false); portRoot.SetActive(true); });
            CreateCompactButton("SAIL", new Vector2(132f, -188f), () => { shipyardRoot.SetActive(false); saves.Save(state); });
            shipyardRoot.SetActive(false);
        }

        private void CreateCannonSlotButton(CannonSlot slot, Vector2 position)
        {
            Button button = CreateSizedButton(shipyardRoot.transform, ShipCustomizationModel.GetDefinition(slot).ShortName, position, new Vector2(150f, 30f), () => ToggleCannon(slot));
            cannonSlotTexts[(int)slot] = button.GetComponentInChildren<Text>();
        }

        private Text CreateCompactButton(string label, Vector2 position, Action action)
            => CreateSizedButton(shipyardRoot.transform, label, position, new Vector2(220f, 32f), action).GetComponentInChildren<Text>();

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
            shipyardSummary.text = $"MASS {mass:0.0}/{capacity:0.0}   SPEED {ShipCustomizationModel.GetSpeedMultiplier(state) * 100f:0}%   TURN {ShipCustomizationModel.GetTurningMultiplier(state) * 100f:0}%\nCREW {state.Crew}   GUNS {ShipCustomizationModel.GetInstalledCannonCount(state)}   DOCK STORAGE {state.SpareCannons}";
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
            mapRoot = CreateUiObject("Exploration Chart", canvas).gameObject;
            var imageObject = CreateUiObject("Circular Map", mapRoot.transform);
            var frameObject = CreateUiObject("Map Brass Bezel", mapRoot.transform);
            Image frame = frameObject.gameObject.AddComponent<Image>(); frame.sprite = panelSprite; frame.color = Color.white; frame.raycastTarget = false;
            frameObject.sizeDelta = new Vector2(340f, 340f); frameObject.anchoredPosition = new Vector2(0f, -405f);
            frameObject.SetSiblingIndex(imageObject.GetSiblingIndex());
            var map = imageObject.gameObject.AddComponent<RawImage>();
            map.texture = CreateMapTexture(240);
            imageObject.sizeDelta = new Vector2(320f, 320f);
            imageObject.anchoredPosition = new Vector2(0f, -405f);
            CreateButton(mapRoot.transform, "BACK", new Vector2(0f, -610f), () => { Destroy(mapRoot); mapRoot = null; });
        }

        private Texture2D CreateMapTexture(int size)
        {
            var pixels = new Color32[size * size];
            float c = (size - 1) * 0.5f;
            int currentX = Mathf.FloorToInt(boat.LogicalPosition.x / WorldGenerator.ChunkSize);
            int currentY = Mathf.FloorToInt(boat.LogicalPosition.y / WorldGenerator.ChunkSize);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - c, dy = y - c;
                if (dx * dx + dy * dy > c * c) { pixels[y * size + x] = Color.clear; continue; }
                int cx = currentX + Mathf.FloorToInt((x - c) / 14f);
                int cy = currentY + Mathf.FloorToInt((y - c) / 14f);
                bool known = state.ExploredChunks.Contains(GameState.PackChunk(cx, cy));
                bool grid = x % 14 == 0 || y % 14 == 0;
                pixels[y * size + x] = known ? (grid ? new Color(0.18f, 0.52f, 0.58f) : new Color(0.08f, 0.27f, 0.34f)) : new Color(0.025f, 0.06f, 0.11f, 0.94f);
            }
            for (int y = size / 2 - 3; y <= size / 2 + 3; y++)
            for (int x = size / 2 - 3; x <= size / 2 + 3; x++) pixels[y * size + x] = Brass;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(pixels); texture.Apply(); return texture;
        }

        private void CloseAll()
        {
            menuRoot.SetActive(false);
            portRoot.SetActive(false);
            shipyardRoot.SetActive(false);
            inventory?.Close();
            if (mapRoot != null) { Destroy(mapRoot); mapRoot = null; }
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
            Text text = CreateText(rect, label, Vector2.zero, rect.sizeDelta, 13, TextAnchor.MiddleCenter); text.color = Color.white;
            return button;
        }

        private Button CreateSizedButton(Transform parent, string label, Vector2 position, Vector2 size, Action action)
        {
            RectTransform rect = CreateUiObject(label, parent); rect.anchoredPosition = position; rect.sizeDelta = size;
            Image image = rect.gameObject.AddComponent<Image>(); image.sprite = pillSprite; image.type = Image.Type.Sliced; image.color = Color.white;
            Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(() => action());
            Text text = CreateText(rect, label, Vector2.zero, size - new Vector2(8f, 0f), 12, TextAnchor.MiddleCenter); text.color = Color.white;
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
