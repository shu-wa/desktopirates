using System;
using UnityEngine;
using UnityEngine.UI;

namespace Desktopirates
{
    /// <summary>Configurable unattended sailing, salvage, combat and docking pilot.</summary>
    public sealed class AutoVoyageController : MonoBehaviour
    {
        private RectTransform canvas;
        private GameState state;
        private BoatController boat;
        private PoiSystem pois;
        private MenuController menu;
        private SaveSystem saves;
        private GameObject panelRoot;
        private GameObject badgeRoot;
        private Text titleText;
        private Text collectHeading;
        private Text encounterHeading;
        private Text destinationHeading;
        private Text panelStatus;
        private Text badgeText;
        private Text startText;
        private Text stopText;
        private Text closeText;
        private Text wreckText;
        private Text treasureText;
        private readonly Text[] encounterTexts = new Text[3];
        private readonly Text[] destinationTexts = new Text[5];
        private Image wreckButton;
        private Image treasureButton;
        private readonly Image[] encounterButtons = new Image[3];
        private readonly Image[] destinationButtons = new Image[5];
        private bool draftWrecks;
        private bool draftTreasures;
        private AutoEncounterPolicy draftEncounter;
        private AutoDestinationMode draftDestination;
        private bool lastLanguage;
        private bool hasCourse;
        private Vector2 courseTarget;
        private ulong courseId;
        private PoiKind courseKind;
        private int freeRoamLeg;
        private float nextRoutePlan;
        private float nextCombatCourse;
        private string runtimeStatus = string.Empty;

        public bool IsPanelOpen => panelRoot != null && panelRoot.activeSelf;

        public void Initialize(RectTransform canvasRoot, GameState gameState, BoatController player,
            PoiSystem poiSystem, MenuController menuController, SaveSystem saveSystem)
        {
            canvas = canvasRoot;
            state = gameState;
            boat = player;
            pois = poiSystem;
            menu = menuController;
            saves = saveSystem;
            BuildPanel();
            BuildBadge();
            menu?.RegisterExternalModal(() => IsPanelOpen, ClosePanel);
            lastLanguage = GameLocalization.IsJapanese;

            if (Array.Exists(Environment.GetCommandLineArgs(), argument => argument == "--auto-preview")) OpenPanel();
            if (Array.Exists(Environment.GetCommandLineArgs(), argument => argument == "--auto-sailing-preview"))
            {
                state.AutoVoyageEnabled = true;
                state.AutoCollectWrecks = true;
                state.AutoCollectTreasures = true;
                state.AutoEncounterPolicy = AutoEncounterPolicy.Avoid;
                state.AutoDestinationMode = AutoDestinationMode.NearestLandmark;
            }
        }

        private void Update()
        {
            if (state == null || boat == null || pois == null) return;
            if (Input.GetKeyDown(KeyCode.O))
            {
                if (IsPanelOpen) ClosePanel();
                else OpenPanel();
            }

            if (lastLanguage != GameLocalization.IsJapanese)
            {
                lastLanguage = GameLocalization.IsJapanese;
                RefreshPanel();
            }

            UpdateBadge();
            if (!state.AutoVoyageEnabled)
            {
                if (boat.IsVoyageAutopilot) boat.DisengageVoyageAutopilot();
                return;
            }
            if (boat.IsDocking) return;
            if (boat.IsMoored)
            {
                StopAutopilot(GameLocalization.Choose("ARRIVED — MOORED SAFELY", "目的地到着 — 停泊完了"));
                return;
            }

            bool collected = pois.TryAutoCollectNearby(state.AutoCollectWrecks, state.AutoCollectTreasures);
            if (!collected && pois.TryGetAutoCollectTarget(state.AutoCollectWrecks, state.AutoCollectTreasures,
                AutoVoyageModel.AutoCollectAcquireRange, out PoiRecord salvage, out float salvageDistance))
            {
                Vector2 delta = salvage.LogicalPosition - boat.LogicalPosition;
                float desiredHeading = Mathf.Atan2(delta.x, delta.y) * Mathf.Rad2Deg;
                float headingError = Mathf.DeltaAngle(boat.HeadingDegrees, desiredHeading);
                int salvageStep = AutoVoyageModel.GetSalvageCruiseStep(salvageDistance, headingError, boat.Speed, boat.MaxCruiseStep);
                boat.SetVoyageCourse(salvage.LogicalPosition, salvageStep, true);
                string order = salvageStep == 0
                    ? GameLocalization.Choose("STOP • COASTING TURN", "STOP • 惰性旋回")
                    : GameLocalization.Choose("DEAD SLOW APPROACH", "微速接近");
                runtimeStatus = GameLocalization.Choose($"SALVAGE → {salvage.Kind.ToString().ToUpperInvariant()}  {salvageDistance:0.0}m • {order}",
                    $"回収 → {(salvage.Kind == PoiKind.Wreck ? "残骸" : "宝物")}  {salvageDistance:0.0}m • {order}");
                return;
            }
            PoiRecord enemy = pois.FindNearestActiveEnemy(AutoVoyageModel.EnemyAwarenessRange);
            if (enemy != null && enemy.Boss != BossKind.None && enemy.Id == courseId
                && state.AutoDestinationMode == AutoDestinationMode.BossSignal
                && state.AutoEncounterPolicy != AutoEncounterPolicy.Fight)
            {
                StopAutopilot(GameLocalization.Choose("BOSS SIGNAL REACHED — MANUAL CONTROL", "ボス信号へ到着 — 手動操船に切替"));
                return;
            }
            if (enemy != null && state.AutoEncounterPolicy == AutoEncounterPolicy.Avoid)
            {
                pois.SetAttackMode(false, false);
                courseTarget = AutoVoyageModel.GetEscapeWaypoint(boat.LogicalPosition, enemy.LogicalPosition);
                boat.SetVoyageCourse(courseTarget, boat.MaxCruiseStep);
                runtimeStatus = GameLocalization.Choose($"EVADING {enemy.DisplayName}", $"{enemy.DisplayName} から退避中");
                return;
            }

            if (state.AutoEncounterPolicy == AutoEncounterPolicy.Fight)
            {
                pois.SetAttackMode(true, false);
                if (enemy != null)
                {
                    if (Time.unscaledTime >= nextCombatCourse)
                    {
                        nextCombatCourse = Time.unscaledTime + 0.35f;
                        courseTarget = AutoVoyageModel.GetCombatWaypoint(boat.LogicalPosition, enemy.LogicalPosition, enemy.Id);
                        boat.SetVoyageCourse(courseTarget, Mathf.Max(1, boat.MaxCruiseStep - 1));
                    }
                    runtimeStatus = GameLocalization.Choose($"ENGAGING {enemy.DisplayName}", $"{enemy.DisplayName} と交戦中");
                    return;
                }
            }
            else pois.SetAttackMode(false, false);

            bool courseResolved = hasCourse && courseId != 0UL && state.ResolvedEvents.Contains(courseId);
            bool arrived = hasCourse && Vector2.Distance(boat.LogicalPosition, courseTarget) <= AutoVoyageModel.WaypointArrivalDistance;
            if (!hasCourse || courseResolved || arrived || Time.unscaledTime >= nextRoutePlan)
                PlanRoute(arrived || courseResolved);

            if (!hasCourse)
            {
                boat.DisengageVoyageAutopilot(false);
                return;
            }

            float distance = Vector2.Distance(boat.LogicalPosition, courseTarget);
            if (courseKind == PoiKind.Port && distance <= AutoVoyageModel.PortPilotRange && pois.TryAutoDock(courseId))
            {
                hasCourse = false;
                runtimeStatus = GameLocalization.Choose("HARBOR PILOT TAKING CONTROL", "水先案内人へ操船を引き継ぎ中");
                return;
            }

            boat.SetVoyageCourse(courseTarget, boat.MaxCruiseStep);
            runtimeStatus = GetCourseLabel(distance);
        }

        private void PlanRoute(bool advanceLeg)
        {
            nextRoutePlan = Time.unscaledTime + 2.5f;
            if (advanceLeg) hasCourse = false;
            GeneratedEventData target;
            switch (state.AutoDestinationMode)
            {
                case AutoDestinationMode.NearestLandmark:
                    if (AutoVoyageModel.TryFindNearestLandmark(state.WorldSeed, boat.LogicalPosition, state.ResolvedEvents,
                        state.AutoCollectWrecks, state.AutoCollectTreasures, out target)) SetCourse(target);
                    break;
                case AutoDestinationMode.BossSignal:
                    if (state.BossCompassOwned && WorldGenerator.TryFindNearestBoss(state.WorldSeed, boat.LogicalPosition, state.ResolvedEvents, out target)) SetCourse(target);
                    break;
                case AutoDestinationMode.BossHarbor:
                    if (state.BossCompassOwned && AutoVoyageModel.TryFindBossHarbor(state.WorldSeed, boat.LogicalPosition,
                        state.ResolvedEvents, out _, out target)) SetCourse(target);
                    break;
                case AutoDestinationMode.NearestHarbor:
                    if (AutoVoyageModel.TryFindNearestHarbor(state.WorldSeed, boat.LogicalPosition, out target)) SetCourse(target);
                    break;
                default:
                    if (!hasCourse || advanceLeg)
                    {
                        courseTarget = AutoVoyageModel.CreateFreeRoamWaypoint(state.WorldSeed, boat.LogicalPosition, freeRoamLeg++);
                        courseId = 0UL;
                        courseKind = PoiKind.Treasure;
                        hasCourse = true;
                    }
                    break;
            }

            if (!hasCourse) runtimeStatus = GameLocalization.Choose("SCANNING FOR A SAFE ROUTE…", "安全な航路を探索中…");
        }

        private void SetCourse(GeneratedEventData target)
        {
            courseTarget = target.Position;
            courseId = target.Id;
            courseKind = target.Kind;
            hasCourse = true;
        }

        private string GetCourseLabel(float distance)
        {
            string destination = state.AutoDestinationMode switch
            {
                AutoDestinationMode.NearestLandmark => GameLocalization.Choose("LANDMARK", "ランドマーク"),
                AutoDestinationMode.BossSignal => GameLocalization.Choose("BOSS SIGNAL", "ボス信号"),
                AutoDestinationMode.BossHarbor => GameLocalization.Choose("BOSS HARBOR", "ボス直近港"),
                AutoDestinationMode.NearestHarbor => GameLocalization.Choose("NEAREST HARBOR", "最寄り港"),
                _ => GameLocalization.Choose("UNCHARTED WATERS", "未踏海域")
            };
            return GameLocalization.Choose($"AUTO → {destination}  {distance:0}m", $"AUTO → {destination}  {distance:0}m");
        }

        private void OpenPanel()
        {
            menu?.PrepareExternalModal();
            draftWrecks = state.AutoCollectWrecks;
            draftTreasures = state.AutoCollectTreasures;
            draftEncounter = state.AutoEncounterPolicy;
            draftDestination = state.AutoDestinationMode;
            panelRoot.SetActive(true);
            panelRoot.transform.SetAsLastSibling();
            RefreshPanel();
        }

        private void ClosePanel()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void ApplyAndStart()
        {
            if ((draftDestination == AutoDestinationMode.BossSignal || draftDestination == AutoDestinationMode.BossHarbor)
                && !state.BossCompassOwned)
            {
                panelStatus.text = GameLocalization.Choose("BOSS COMPASS REQUIRED — AVAILABLE AT PORT", "BOSS COMPASSが必要です — 港で購入できます");
                panelStatus.color = UiTheme.Warning;
                return;
            }

            state.AutoCollectWrecks = draftWrecks;
            state.AutoCollectTreasures = draftTreasures;
            state.AutoEncounterPolicy = draftEncounter;
            state.AutoDestinationMode = draftDestination;
            state.AutoVoyageEnabled = true;
            boat.ReleaseMooring();
            hasCourse = false;
            nextRoutePlan = 0f;
            saves?.Save(state);
            ClosePanel();
        }

        private void StopAutopilot(string status = null, bool save = true)
        {
            state.AutoVoyageEnabled = false;
            hasCourse = false;
            boat.DisengageVoyageAutopilot();
            pois.SetAttackMode(false, false);
            if (!string.IsNullOrEmpty(status)) runtimeStatus = status;
            if (save) saves?.Save(state);
            RefreshPanel();
        }

        private void BuildPanel()
        {
            panelRoot = new GameObject("Auto Voyage Settings", typeof(RectTransform)).gameObject;
            panelRoot.transform.SetParent(canvas, false);
            RectTransform panel = panelRoot.GetComponent<RectTransform>();
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = new Vector2(0f, -18f);
            panel.sizeDelta = new Vector2(646f, 610f);

            RawImage backdrop = panelRoot.AddComponent<RawImage>();
            backdrop.texture = UiTextureFactory.LoadScreenBackground("captain_log");
            backdrop.color = new Color(0.80f, 0.88f, 0.90f, 0.98f);

            titleText = CreateText(panel, "AUTO VOYAGE", new Vector2(0f, 166f), new Vector2(560f, 42f), 24, TextAnchor.MiddleCenter);
            titleText.color = UiTheme.Brass;
            collectHeading = CreateText(panel, "SALVAGE ORDERS", new Vector2(0f, 124f), new Vector2(560f, 26f), 14, TextAnchor.MiddleCenter);
            wreckButton = CreateChoiceButton(panel, new Vector2(-145f, 82f), new Vector2(258f, 51f), () => { draftWrecks = !draftWrecks; RefreshPanel(); }, out wreckText);
            treasureButton = CreateChoiceButton(panel, new Vector2(145f, 82f), new Vector2(258f, 51f), () => { draftTreasures = !draftTreasures; RefreshPanel(); }, out treasureText);

            encounterHeading = CreateText(panel, "ENCOUNTER ORDERS", new Vector2(0f, 35f), new Vector2(560f, 26f), 14, TextAnchor.MiddleCenter);
            for (int i = 0; i < encounterButtons.Length; i++)
            {
                int captured = i;
                encounterButtons[i] = CreateChoiceButton(panel, new Vector2(-190f + i * 190f, -3f), new Vector2(170f, 45f),
                    () => { draftEncounter = (AutoEncounterPolicy)captured; RefreshPanel(); }, out encounterTexts[i]);
            }

            destinationHeading = CreateText(panel, "DESTINATION", new Vector2(0f, -57f), new Vector2(560f, 26f), 14, TextAnchor.MiddleCenter);
            Vector2[] destinationPositions =
            {
                new Vector2(-145f, -96f), new Vector2(145f, -96f),
                new Vector2(-145f, -143f), new Vector2(145f, -143f),
                new Vector2(0f, -190f)
            };
            for (int i = 0; i < destinationButtons.Length; i++)
            {
                int captured = i;
                destinationButtons[i] = CreateChoiceButton(panel, destinationPositions[i], new Vector2(258f, 43f),
                    () => { draftDestination = (AutoDestinationMode)captured; RefreshPanel(); }, out destinationTexts[i]);
            }

            panelStatus = CreateText(panel, string.Empty, new Vector2(0f, -226f), new Vector2(560f, 32f), 12, TextAnchor.MiddleCenter);
            panelStatus.color = UiTheme.SecondaryText;
            Image startButton = CreateChoiceButton(panel, new Vector2(145f, -263f), new Vector2(258f, 46f), ApplyAndStart, out startText);
            startButton.color = new Color(0.72f, 1f, 0.90f, 1f);
            Image stopButton = CreateChoiceButton(panel, new Vector2(-145f, -263f), new Vector2(258f, 46f), () => StopAutopilot(), out stopText);
            stopButton.color = new Color(1f, 0.62f, 0.52f, 1f);
            CreateChoiceButton(panel, new Vector2(0f, -294f), new Vector2(220f, 24f), ClosePanel, out closeText);
            panelRoot.SetActive(false);
        }

        private void BuildBadge()
        {
            badgeRoot = new GameObject("Auto Voyage Status", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            badgeRoot.transform.SetParent(canvas, false);
            RectTransform rect = badgeRoot.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 166f);
            rect.sizeDelta = new Vector2(410f, 34f);
            Image image = badgeRoot.GetComponent<Image>();
            image.sprite = UiTextureFactory.LoadPillSprite();
            image.type = Image.Type.Sliced;
            image.color = new Color(0.012f, 0.047f, 0.072f, 0.94f);
            image.raycastTarget = false;
            badgeText = CreateText(rect, string.Empty, Vector2.zero, new Vector2(390f, 28f), 12, TextAnchor.MiddleCenter);
            badgeText.color = UiTheme.Mint;
            badgeRoot.SetActive(false);
        }

        private Image CreateChoiceButton(Transform parent, Vector2 position, Vector2 size, Action action, out Text label)
        {
            var gameObject = new GameObject("Auto Voyage Choice", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = gameObject.GetComponent<Image>();
            image.sprite = UiTextureFactory.LoadConceptSprite("Chrome", "service_button", 18f, true);
            image.type = Image.Type.Sliced;
            Button button = gameObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => action());
            label = CreateText(rect, string.Empty, Vector2.zero, size - new Vector2(18f, 10f), 13, TextAnchor.MiddleCenter);
            label.raycastTarget = false;
            return image;
        }

        private static Text CreateText(Transform parent, string value, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment)
        {
            var gameObject = new GameObject(value + " Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Text text = gameObject.GetComponent<Text>();
            text.font = GameLocalization.IsJapanese
                ? Font.CreateDynamicFontFromOSFont(new[] { "Yu Gothic UI", "Meiryo UI", "Meiryo", "Arial" }, fontSize)
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = UiTheme.PrimaryText;
            text.text = value;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(9, fontSize - 4);
            text.resizeTextMaxSize = fontSize;
            text.raycastTarget = false;
            UiTheme.StyleText(text, fontSize);
            return text;
        }

        private void RefreshPanel()
        {
            if (panelRoot == null) return;
            titleText.text = GameLocalization.Choose("AUTO VOYAGE", "AUTO VOYAGE — 自動航海");
            collectHeading.text = GameLocalization.Choose("SALVAGE ORDERS", "回収指示");
            encounterHeading.text = GameLocalization.Choose("ENCOUNTER ORDERS", "敵船との遭遇時");
            destinationHeading.text = GameLocalization.Choose("DESTINATION", "航海目標");
            wreckText.text = ToggleLabel(GameLocalization.Choose("WRECKS", "残骸を回収"), draftWrecks);
            treasureText.text = ToggleLabel(GameLocalization.Choose("TREASURE", "宝物を回収"), draftTreasures);
            wreckButton.color = ChoiceColor(draftWrecks);
            treasureButton.color = ChoiceColor(draftTreasures);

            string[] encounters = GameLocalization.IsJapanese
                ? new[] { "AVOID / 退避", "ENGAGE / 交戦", "OBSERVE / 不干渉" }
                : new[] { "AVOID", "ENGAGE", "OBSERVE" };
            for (int i = 0; i < encounterTexts.Length; i++)
            {
                encounterTexts[i].text = encounters[i];
                encounterButtons[i].color = ChoiceColor((int)draftEncounter == i);
            }

            string[] destinations = GameLocalization.IsJapanese
                ? new[] { "FREE ROAM / 自由航海", "LANDMARK / 近隣地点", "BOSS SIGNAL / ボス追跡", "BOSS HARBOR / ボス直近港", "HARBOR / 最寄り港" }
                : new[] { "FREE ROAM", "NEAREST LANDMARK", "BOSS SIGNAL", "BOSS HARBOR", "NEAREST HARBOR" };
            for (int i = 0; i < destinationTexts.Length; i++)
            {
                destinationTexts[i].text = destinations[i];
                destinationButtons[i].color = ChoiceColor((int)draftDestination == i);
            }

            bool compassNeeded = draftDestination == AutoDestinationMode.BossSignal || draftDestination == AutoDestinationMode.BossHarbor;
            panelStatus.text = compassNeeded && !state.BossCompassOwned
                ? GameLocalization.Choose("BOSS COMPASS REQUIRED — BUY ONE AT PORT", "BOSS COMPASSが必要です — 港で購入できます")
                : GameLocalization.Choose("O: OPEN/CLOSE  •  SETTINGS APPLY WHEN VOYAGE STARTS", "O: 開閉  •  決定すると設定を保存して出航します");
            panelStatus.color = compassNeeded && !state.BossCompassOwned ? UiTheme.Warning : UiTheme.SecondaryText;
            startText.text = state.AutoVoyageEnabled
                ? GameLocalization.Choose("APPLY COURSE", "航路を更新")
                : GameLocalization.Choose("START AUTO VOYAGE", "自動航海を開始");
            stopText.text = GameLocalization.Choose("ALL STOP", "自動航海を停止");
            closeText.text = GameLocalization.Choose("CLOSE  [O]", "閉じる  [O]");
        }

        private void UpdateBadge()
        {
            bool visible = state.AutoVoyageEnabled && !boat.IsDocking && !IsPanelOpen && (menu == null || !menu.IsModalOpen);
            badgeRoot.SetActive(visible);
            if (!visible) return;
            badgeText.text = string.IsNullOrEmpty(runtimeStatus)
                ? GameLocalization.Choose("AUTO VOYAGE — PLOTTING COURSE", "AUTO VOYAGE — 航路計算中")
                : runtimeStatus;
        }

        private static string ToggleLabel(string label, bool enabled)
            => $"{(enabled ? "◆" : "◇")}  {label}  {(enabled ? "ON" : "OFF")}";

        private static Color ChoiceColor(bool selected)
            => selected ? Color.white : new Color(0.48f, 0.55f, 0.58f, 0.92f);
    }
}
