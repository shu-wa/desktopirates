using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Desktopirates.Tests
{
    public sealed class InfiniteWorldAndSaveTests
    {
        [Test]
        public void ChunkGenerationIsDeterministic()
        {
            var first = new List<GeneratedEventData>();
            var second = new List<GeneratedEventData>();
            WorldGenerator.GenerateChunk(12345, -18, 27, first);
            WorldGenerator.GenerateChunk(12345, -18, 27, second);
            Assert.That(second.Count, Is.EqualTo(first.Count));
            for (int i = 0; i < first.Count; i++)
            {
                Assert.That(second[i].Id, Is.EqualTo(first[i].Id));
                Assert.That(second[i].Kind, Is.EqualTo(first[i].Kind));
                Assert.That(second[i].Position, Is.EqualTo(first[i].Position));
            }
        }

        [Test]
        public void OriginAlwaysContainsSafeHarbor()
        {
            var events = new List<GeneratedEventData>();
            WorldGenerator.GenerateChunk(GameState.DefaultWorldSeed, 0, 0, events);
            Assert.That(events.Exists(item => item.Kind == PoiKind.Port), Is.True);
        }

        [Test]
        public void ExplorationChartKeepsPlayerCenteredAndNorthUp()
        {
            Vector2 center = new Vector2(73f, -41f);
            Assert.That(MapChartModel.WorldToMap(center, center, MapZoom.Local), Is.EqualTo(Vector2.zero));
            Assert.That(MapChartModel.WorldToMap(center + Vector2.up * 10f, center, MapZoom.Local).y, Is.GreaterThan(0f));
            Assert.That(MapChartModel.WorldToMap(center + Vector2.right * 10f, center, MapZoom.Local).x, Is.GreaterThan(0f));
        }

        [Test]
        public void WideChartShowsMoreWorldWithoutMovingKnownLocations()
        {
            Vector2 center = new Vector2(-18f, 27f);
            Vector2 point = center + new Vector2(36f, 18f);
            Vector2 local = MapChartModel.WorldToMap(point, center, MapZoom.Local);
            Vector2 wide = MapChartModel.WorldToMap(point, center, MapZoom.Wide);

            Assert.That(local.normalized.x, Is.EqualTo(wide.normalized.x).Within(0.001f));
            Assert.That(local.normalized.y, Is.EqualTo(wide.normalized.y).Within(0.001f));
            Assert.That(wide.magnitude, Is.LessThan(local.magnitude));
        }

        [Test]
        public void LiveChartMarkerMovesUntilItNeedsRecentering()
        {
            Vector2 chartCenter = new Vector2(20f, -8f);
            Vector2 nearbyShip = chartCenter + Vector2.right * 12f;
            Vector2 farShip = chartCenter + Vector2.right * (MapChartModel.GetWorldRadius(MapZoom.Local) * 0.8f);

            Assert.That(MapChartModel.WorldToMap(nearbyShip, chartCenter, MapZoom.Local).x, Is.GreaterThan(0f));
            Assert.That(MapChartModel.ShouldRecenter(nearbyShip, chartCenter, MapZoom.Local), Is.False);
            Assert.That(MapChartModel.ShouldRecenter(farShip, chartCenter, MapZoom.Local), Is.True);
        }

        [Test]
        public void ExplorationChartUsesATransparentCircularBoundary()
        {
            Texture2D chart = MapChartModel.CreateTexture(new GameState(), Vector2.zero, MapZoom.Local, 96);
            try
            {
                Assert.That(chart.GetPixel(0, 0).a, Is.EqualTo(0f).Within(0.001f));
                Assert.That(chart.GetPixel(chart.width / 2, chart.height / 2).a, Is.GreaterThan(0.8f));
                Assert.That(chart.filterMode, Is.EqualTo(FilterMode.Point));
            }
            finally
            {
                Object.DestroyImmediate(chart);
            }
        }

        [Test]
        public void CompactSaveRoundTripsAllGameplayState()
        {
            var state = new GameState
            {
                WorldSeed = 7812,
                PlayerPosition = new Vector2(-123.375f, 456.625f),
                HeadingDegrees = 271.4f,
                Gold = 9021,
                Supplies = 28,
                EngineLevel = 4,
                CannonLevel = 3,
                Crew = 7,
                CapacityLevel = 2,
                ArmorLevel = 3,
                TurningLevel = 1,
                CannonMountMask = 0b101101,
                SpareCannons = 2,
                ShipLevel = 3,
                Food = 47,
                Water = 39,
                ProvisionClock = 71.4f
            };
            state.MaxHull = ShipProgressionModel.GetMaxHull(state);
            state.Hull = state.MaxHull - 91;
            state.SetRoleCrew(CrewRole.Cannons, 3);
            state.SetRoleCrew(CrewRole.Helm, 1);
            state.SetRoleCrew(CrewRole.Sails, 2);
            state.SetRoleCrew(CrewRole.Anchor, 1);
            state.SetRoleCrew(CrewRole.Repairer, 1);
            state.AddPerk(CrewPerk.FastHands, 2);
            state.TryEquipPerkStack(CrewRole.Cannons, CrewPerk.FastHands);
            state.TryEquipPerkStack(CrewRole.Cannons, CrewPerk.FastHands);
            state.AddPerk(CrewPerk.PowderExpert, PerkRank.IV);
            state.TryEquipPerkStack(CrewRole.Cannons, CrewPerk.PowderExpert);
            state.AddPerk(CrewPerk.RapidRepair);
            state.TryEquipPerkStack(CrewRole.Repairer, CrewPerk.RapidRepair);
            for (int y = -10; y <= 10; y++)
            for (int x = -22; x <= 22; x++) state.ExploredChunks.Add(GameState.PackChunk(x, y));
            for (ulong i = 0; i < 300; i++) state.ResolvedEvents.Add(1000000UL + i * 17UL);
            for (int i = 0; i < SalvageInventory.PartKindCount; i++) state.SetPartCount((SalvagePartKind)i, i * 7 + 2);

            byte[] bytes = CompactSaveCodec.Serialize(state);
            GameState restored = CompactSaveCodec.Deserialize(bytes);

            Assert.That(restored.WorldSeed, Is.EqualTo(state.WorldSeed));
            Assert.That(restored.PlayerPosition.x, Is.EqualTo(state.PlayerPosition.x).Within(1f / 16f));
            Assert.That(restored.PlayerPosition.y, Is.EqualTo(state.PlayerPosition.y).Within(1f / 16f));
            Assert.That(restored.HeadingDegrees, Is.EqualTo(state.HeadingDegrees).Within(0.02f));
            Assert.That(restored.Gold, Is.EqualTo(state.Gold));
            Assert.That(restored.MaxHull, Is.EqualTo(state.MaxHull));
            Assert.That(restored.Crew, Is.EqualTo(state.Crew));
            Assert.That(restored.CapacityLevel, Is.EqualTo(state.CapacityLevel));
            Assert.That(restored.ArmorLevel, Is.EqualTo(state.ArmorLevel));
            Assert.That(restored.TurningLevel, Is.EqualTo(state.TurningLevel));
            Assert.That(restored.CannonMountMask, Is.EqualTo(state.CannonMountMask));
            Assert.That(restored.SpareCannons, Is.EqualTo(state.SpareCannons));
            Assert.That(restored.ShipLevel, Is.EqualTo(3));
            Assert.That(restored.Food, Is.EqualTo(47));
            Assert.That(restored.Water, Is.EqualTo(39));
            Assert.That(restored.ProvisionClock, Is.EqualTo(71.4f).Within(0.11f));
            Assert.That(restored.GetRoleCrew(CrewRole.Cannons), Is.EqualTo(3));
            Assert.That(restored.GetRoleCrew(CrewRole.Repairer), Is.EqualTo(1));
            Assert.That(restored.GetEquippedPerk(CrewRole.Cannons), Is.EqualTo(CrewPerk.PowderExpert));
            Assert.That(restored.GetEquippedPerkCount(CrewRole.Cannons, CrewPerk.FastHands), Is.EqualTo(2));
            Assert.That(restored.GetPerkCount(CrewPerk.PowderExpert, PerkRank.IV), Is.EqualTo(1));
            Assert.That(restored.GetEquippedPerkCount(CrewRole.Cannons, CrewPerk.PowderExpert, PerkRank.IV), Is.EqualTo(1));
            Assert.That(restored.GetEquippedPerkCount(CrewRole.Repairer, CrewPerk.RapidRepair), Is.EqualTo(1));
            Assert.That(restored.ExploredChunks.SetEquals(state.ExploredChunks), Is.True);
            Assert.That(restored.ResolvedEvents.SetEquals(state.ResolvedEvents), Is.True);
            for (int i = 0; i < SalvageInventory.PartKindCount; i++)
                Assert.That(restored.GetPartCount((SalvagePartKind)i), Is.EqualTo(state.GetPartCount((SalvagePartKind)i)));
            Assert.That(bytes.Length, Is.LessThan(1600), "Row spans and delta varints should keep a large voyage tiny.");
        }

        [Test]
        public void MenuCircleSeparatesClickFromDragAtSevenPixels()
        {
            Assert.That(Vector2.Distance(Vector2.zero, new Vector2(3f, 4f)), Is.LessThan(MenuCircleController.DragThreshold));
            Assert.That(Vector2.Distance(Vector2.zero, new Vector2(8f, 0f)), Is.GreaterThan(MenuCircleController.DragThreshold));
        }

        [Test]
        public void CannonsOnlyJoinASalvoInsideTheirInstalledArc()
        {
            var state = new GameState
            {
                Crew = 6,
                CannonMountMask = (1 << (int)CannonSlot.Bow) | (1 << (int)CannonSlot.PortFore) | (1 << (int)CannonSlot.PortAft)
            };
            state.SetRoleCrew(CrewRole.Cannons, 6);

            SalvoSolution bow = ShipCustomizationModel.GetSalvo(state, 4f);
            SalvoSolution port = ShipCustomizationModel.GetSalvo(state, -88f);
            SalvoSolution starboard = ShipCustomizationModel.GetSalvo(state, 90f);

            Assert.That(bow.CannonsFiring, Is.EqualTo(1));
            Assert.That(bow.ArcName, Is.EqualTo("BOW"));
            Assert.That(port.CannonsFiring, Is.EqualTo(2));
            Assert.That(port.ArcName, Is.EqualTo("PORT"));
            Assert.That(starboard.CannonsFiring, Is.EqualTo(0));
        }

        [Test]
        public void OneCrewMemberIsRequiredForEveryFiringCannon()
        {
            var state = new GameState
            {
                Crew = 1,
                CannonMountMask = (1 << (int)CannonSlot.StarboardFore) | (1 << (int)CannonSlot.StarboardAft)
            };
            state.SetRoleCrew(CrewRole.Sails, 0);
            state.SetRoleCrew(CrewRole.Anchor, 0);
            state.SetRoleCrew(CrewRole.Cannons, 1);

            SalvoSolution salvo = ShipCustomizationModel.GetSalvo(state, 90f);
            Assert.That(salvo.CannonsInArc, Is.EqualTo(2));
            Assert.That(salvo.CannonsFiring, Is.EqualTo(1));
        }

        [Test]
        public void FiringSlotListMatchesCrewAndInstalledArc()
        {
            var state = new GameState
            {
                Crew = 1,
                CannonMountMask = (1 << (int)CannonSlot.PortFore) | (1 << (int)CannonSlot.PortAft) | (1 << (int)CannonSlot.StarboardFore)
            };
            state.SetRoleCrew(CrewRole.Sails, 0);
            state.SetRoleCrew(CrewRole.Anchor, 0);
            state.SetRoleCrew(CrewRole.Cannons, 1);
            var slots = new List<CannonSlot>();

            int count = ShipCustomizationModel.GetFiringSlots(state, -90f, slots);

            Assert.That(count, Is.EqualTo(1));
            Assert.That(slots[0] == CannonSlot.PortFore || slots[0] == CannonSlot.PortAft, Is.True);
        }

        [Test]
        public void HarborPilotUsesAnOuterApproachThenAnAlignedBerth()
        {
            Vector2 port = new Vector2(18f, -7f);
            Vector2 approach = DockingModel.GetApproach(port);
            Vector2 berth = DockingModel.GetBerth(port);

            Assert.That(approach.x, Is.EqualTo(berth.x).Within(0.001f));
            Assert.That(Vector2.Distance(approach, port), Is.GreaterThan(Vector2.Distance(berth, port)));
            Assert.That(DockingModel.GetHeading(approach, berth), Is.EqualTo(DockingModel.FinalHeading).Within(0.001f));
            Assert.That(DockingModel.GetPilotSpeed(0.2f, true), Is.LessThan(DockingModel.GetPilotSpeed(2f, false)));
        }

        [Test]
        public void CannonFlightTimeIsReadableAndBoundedAtDesktopScale()
        {
            Assert.That(CombatVfxMath.GetProjectileDuration(0.1f), Is.EqualTo(0.28f).Within(0.001f));
            Assert.That(CombatVfxMath.GetProjectileDuration(3f), Is.InRange(0.35f, 0.45f));
            Assert.That(CombatVfxMath.GetProjectileDuration(99f), Is.EqualTo(0.68f).Within(0.001f));
        }

        [Test]
        public void InstalledMassReducesSpeedAndTurning()
        {
            var light = new GameState { Crew = 2, Supplies = 0, CannonMountMask = 0 };
            var heavy = new GameState
            {
                Crew = 6,
                Supplies = 20,
                ArmorLevel = 2,
                CannonMountMask = (1 << ShipCustomizationModel.CannonSlotCount) - 1
            };

            Assert.That(ShipCustomizationModel.GetMass(heavy), Is.GreaterThan(ShipCustomizationModel.GetMass(light)));
            Assert.That(ShipCustomizationModel.GetSpeedMultiplier(heavy), Is.LessThan(ShipCustomizationModel.GetSpeedMultiplier(light)));
            Assert.That(ShipCustomizationModel.GetTurningMultiplier(heavy), Is.LessThan(ShipCustomizationModel.GetTurningMultiplier(light)));
        }

        [Test]
        public void CapacityUpgradeCreatesRoomForHeavierLayouts()
        {
            var state = new GameState
            {
                Crew = 3,
                Supplies = 50,
                ShipLevel = 1,
                CannonMountMask = 0b000111
            };
            bool before = ShipCustomizationModel.CanAddMass(state, ShipCustomizationModel.CannonMass);
            state.CapacityLevel++;
            bool after = ShipCustomizationModel.CanAddMass(state, ShipCustomizationModel.CannonMass);

            Assert.That(before, Is.False);
            Assert.That(after, Is.True);
        }

        [Test]
        public void RelativeBearingTracksShipHeading()
        {
            float ahead = ShipCustomizationModel.GetRelativeBearing(90f, Vector2.zero, Vector2.right * 4f);
            float port = ShipCustomizationModel.GetRelativeBearing(90f, Vector2.zero, Vector2.up * 4f);
            Assert.That(ahead, Is.EqualTo(0f).Within(0.001f));
            Assert.That(port, Is.EqualTo(-90f).Within(0.001f));
        }

        [Test]
        public void CruiseStepsAcceleratePersistentlyAndNeverReverse()
        {
            int step = 0;
            step = CruiseModel.ChangeStep(step, 1, 0);
            Assert.That(step, Is.EqualTo(1));
            Assert.That(CruiseModel.GetTargetSpeed(step, 0), Is.EqualTo(CruiseModel.SpeedPerStep));

            step = CruiseModel.ChangeStep(step, -1, 0);
            step = CruiseModel.ChangeStep(step, -1, 0);
            Assert.That(step, Is.EqualTo(0));
            Assert.That(CruiseModel.GetTargetSpeed(step, 0), Is.EqualTo(0f));
        }

        [Test]
        public void EngineUpgradeUnlocksOneAdditionalCruiseStep()
        {
            Assert.That(CruiseModel.GetMaxStep(1), Is.EqualTo(CruiseModel.GetMaxStep(0) + 1));
            Assert.That(CruiseModel.GetMaxSpeed(1), Is.GreaterThan(CruiseModel.GetMaxSpeed(0)));
            Assert.That(CruiseModel.ChangeStep(99, 0, 0), Is.EqualTo(CruiseModel.BaseCruiseSteps));
        }

        [Test]
        public void StopOrderLeavesAVisibleWaterborneCoast()
        {
            float fullSpeed = CruiseModel.GetMaxSpeed(0);
            float afterOneSecond = CruiseModel.IntegrateForwardSpeed(fullSpeed, 0f, 1f);

            Assert.That(afterOneSecond, Is.GreaterThan(fullSpeed * 0.5f));
            Assert.That(CruiseModel.IsCoasting(afterOneSecond, 0f), Is.True);
        }

        [Test]
        public void WaterDragEventuallyBringsTheBoatToRest()
        {
            float speed = CruiseModel.GetMaxSpeed(0);
            float elapsed = 0f;
            while (speed > 0f && elapsed < 30f)
            {
                speed = CruiseModel.IntegrateForwardSpeed(speed, 0f, 0.02f);
                elapsed += 0.02f;
            }

            Assert.That(speed, Is.EqualTo(0f));
            Assert.That(elapsed, Is.GreaterThan(8f).And.LessThan(25f));
        }

        [Test]
        public void CoastGaugeShowsActualMotionAfterStopIsOrdered()
        {
            Assert.That(SpeedGaugeModel.GetMotionLabel(0, 3, 1.2f, 0f), Is.EqualTo("COAST"));
            Assert.That(SpeedGaugeModel.GetActualNeedle01(1.725f, 3.45f), Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(SpeedGaugeModel.GetMotionLabel(0, 3, 0f, 0f), Is.EqualTo("STOP"));
        }

        [Test]
        public void BoatKeepsMovingAfterTheAccelerationKeyIsReleased()
        {
            var controllerObject = new GameObject("Cruise Test Controller");
            var visualObject = new GameObject("Cruise Test Visual");
            try
            {
                BoatController controller = controllerObject.AddComponent<BoatController>();
                controller.Initialize(visualObject.transform, new GameState());
                controller.IncreaseCruiseStep();

                controller.Advance(1f, 0f);
                Vector2 afterFirstSecond = controller.LogicalPosition;
                controller.Advance(1f, 0f);

                Assert.That(afterFirstSecond.magnitude, Is.GreaterThan(0f));
                Assert.That(controller.LogicalPosition.magnitude, Is.GreaterThan(afterFirstSecond.magnitude));
                Assert.That(controller.CruiseStep, Is.EqualTo(1));
                Assert.That(controller.Speed, Is.GreaterThan(0f));
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(visualObject);
            }
        }

        [Test]
        public void WreckSalvageIsDeterministicAndAlwaysIncludesTimber()
        {
            SalvageDrop[] first = SalvageInventory.RollWreck(0xABCDEFUL, 37);
            SalvageDrop[] second = SalvageInventory.RollWreck(0xABCDEFUL, 37);
            Assert.That(first.Length, Is.EqualTo(2));
            Assert.That(first[0].Kind, Is.EqualTo(SalvagePartKind.Timber));
            Assert.That(first[0].Amount, Is.GreaterThan(0));
            Assert.That(second[0].Kind, Is.EqualTo(first[0].Kind));
            Assert.That(second[0].Amount, Is.EqualTo(first[0].Amount));
            Assert.That(second[1].Kind, Is.EqualTo(first[1].Kind));
            Assert.That(second[1].Amount, Is.EqualTo(first[1].Amount));
        }

        [Test]
        public void TelegraphLabelsExposeSpeedAtAGlance()
        {
            Assert.That(SpeedGaugeModel.GetLabel(0, 8), Is.EqualTo("STOP"));
            Assert.That(SpeedGaugeModel.GetLabel(1, 8), Is.EqualTo("SLOW"));
            Assert.That(SpeedGaugeModel.GetLabel(4, 8), Is.EqualTo("CRUISE"));
            Assert.That(SpeedGaugeModel.GetLabel(6, 8), Is.EqualTo("HALF"));
            Assert.That(SpeedGaugeModel.GetLabel(8, 8), Is.EqualTo("FULL"));
        }

        [Test]
        public void WindowResizeKeepsTheMenuCircleUnderTheSameDesktopPoint()
        {
            Vector2Int position = WindowScaleMath.KeepMenuCircleFixed(1200, 220, 720, 760, 900, 950);
            int oldAnchorX = 1200 + 720 / 2;
            int oldAnchorY = 220 + Mathf.RoundToInt(760 * WindowScaleMath.MenuCircleTopRatio);
            int newAnchorX = position.x + 900 / 2;
            int newAnchorY = position.y + Mathf.RoundToInt(950 * WindowScaleMath.MenuCircleTopRatio);
            Assert.That(newAnchorX, Is.EqualTo(oldAnchorX));
            Assert.That(newAnchorY, Is.EqualTo(oldAnchorY));
        }

        [Test]
        public void DeferredSizeSliderCommitsOnlyAfterPointerRelease()
        {
            var sliderObject = new GameObject("Deferred Slider Test", typeof(UnityEngine.UI.Slider));
            try
            {
                UnityEngine.UI.Slider slider = sliderObject.GetComponent<UnityEngine.UI.Slider>();
                slider.minValue = 0.72f;
                slider.maxValue = 1.28f;
                slider.value = 1f;
                int previews = 0;
                int commits = 0;
                float committed = 0f;
                DeferredSliderCommit deferred = sliderObject.AddComponent<DeferredSliderCommit>();
                deferred.Initialize(slider, _ => previews++, value => { commits++; committed = value; });

                deferred.OnPointerDown(null);
                slider.value = 1.24f;
                Assert.That(previews, Is.GreaterThan(1));
                Assert.That(commits, Is.Zero);

                deferred.OnPointerUp(null);
                Assert.That(commits, Is.EqualTo(1));
                Assert.That(committed, Is.EqualTo(1.24f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(sliderObject);
            }
        }

        [Test]
        public void ShipLevelsUnlockSixClearProgressionTiers()
        {
            Assert.That(ShipProgressionModel.TierCount, Is.EqualTo(6));
            Assert.That(ShipProgressionModel.Get(0).Name, Is.EqualTo("RAFT"));
            Assert.That(ShipProgressionModel.Get(5).Name, Is.EqualTo("LARGE SHIP"));
            Assert.That(ShipProgressionModel.Get(5).BaseHull, Is.EqualTo(10000));
            for (int i = 1; i < ShipProgressionModel.TierCount; i++)
            {
                Assert.That(ShipProgressionModel.Get(i).BaseHull, Is.GreaterThan(ShipProgressionModel.Get(i - 1).BaseHull));
                Assert.That(ShipProgressionModel.Get(i).MaxCrew, Is.GreaterThan(ShipProgressionModel.Get(i - 1).MaxCrew));
                Assert.That(ShipProgressionModel.Get(i).MaxCannons, Is.GreaterThan(ShipProgressionModel.Get(i - 1).MaxCannons));
            }
        }

        [Test]
        public void CrewMustBeExplicitlyAssignedToFireCannons()
        {
            var state = new GameState { Crew = 4, ShipLevel = 3, CannonMountMask = 0b001110 };
            Assert.That(ShipCustomizationModel.GetSalvo(state, -90f).CannonsFiring, Is.Zero);
            Assert.That(CrewManagementModel.AssignOne(state, CrewRole.Cannons), Is.True);
            Assert.That(ShipCustomizationModel.GetSalvo(state, -90f).CannonsFiring, Is.EqualTo(1));
        }

        [Test]
        public void SinkingSequenceBurnsFiveSecondsThenSinksForThree()
        {
            Assert.That(CombatVfxMath.GetSinkProgress(4.99f), Is.Zero);
            Assert.That(CombatVfxMath.GetSinkProgress(6.5f), Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(CombatVfxMath.GetSinkProgress(8f), Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void BossPerkRollIsDeterministicAndCompatibleWithAUseCase()
        {
            CrewPerk first = BossModel.RollPerk(BossKind.Kraken, 0x12345678UL);
            CrewPerk second = BossModel.RollPerk(BossKind.Kraken, 0x12345678UL);
            Assert.That(second, Is.EqualTo(first));
            if (first != CrewPerk.None)
                Assert.That(CrewManagementModel.IsCompatible(CrewRole.Anchor, first)
                    || CrewManagementModel.IsCompatible(CrewRole.Helm, first)
                    || CrewManagementModel.IsCompatible(CrewRole.Cannons, first)
                    || CrewManagementModel.IsCompatible(CrewRole.Repairer, first), Is.True);
        }

        [Test]
        public void EveryBossHasAGuaranteedDeterministicWorldEncounter()
        {
            for (int value = (int)BossKind.GangAdmiral; value <= (int)BossKind.Poseidon; value++)
            {
                BossKind boss = (BossKind)value;
                WorldGenerator.GetGuaranteedBossChunk(7919, boss, out int x, out int y);
                var events = new List<GeneratedEventData>();
                WorldGenerator.GenerateChunk(7919, x, y, events);
                Assert.That(events.Exists(item => item.Boss == boss), Is.True, boss.ToString());
            }
        }

        [Test]
        public void InfiniteSectorsKeepProducingBossAnchorsFarFromTheOrigin()
        {
            int found = 0;
            var events = new List<GeneratedEventData>();
            for (int y = 24; y < 48; y++)
            for (int x = 24; x < 48; x++)
            {
                WorldGenerator.GenerateChunk(173, x, y, events);
                if (events.Exists(item => item.Boss != BossKind.None)) found++;
            }
            Assert.That(found, Is.GreaterThanOrEqualTo(4));
        }

        [Test]
        public void HigherRankPerksGiveStrongerEffectsAndStatusChance()
        {
            var low = new GameState { Crew = 1 };
            low.SetRoleCrew(CrewRole.Sails, 0);
            low.SetRoleCrew(CrewRole.Anchor, 0);
            low.SetRoleCrew(CrewRole.Cannons, 1);
            low.AddPerk(CrewPerk.PowderExpert, PerkRank.I);
            low.AddPerk(CrewPerk.Firebrand, PerkRank.I);
            Assert.That(low.TryEquipPerkStack(CrewRole.Cannons, CrewPerk.PowderExpert), Is.True);
            low.SetRoleCrew(CrewRole.Cannons, 2);
            Assert.That(low.TryEquipPerkStack(CrewRole.Cannons, CrewPerk.Firebrand), Is.True);

            var high = new GameState { Crew = 1 };
            high.SetRoleCrew(CrewRole.Sails, 0);
            high.SetRoleCrew(CrewRole.Anchor, 0);
            high.SetRoleCrew(CrewRole.Cannons, 1);
            high.AddPerk(CrewPerk.PowderExpert, PerkRank.IV);
            high.AddPerk(CrewPerk.Firebrand, PerkRank.IV);
            Assert.That(high.TryEquipPerkStack(CrewRole.Cannons, CrewPerk.PowderExpert), Is.True);
            high.SetRoleCrew(CrewRole.Cannons, 2);
            Assert.That(high.TryEquipPerkStack(CrewRole.Cannons, CrewPerk.Firebrand), Is.True);

            Assert.That(CrewManagementModel.GetCannonDamageMultiplier(high), Is.GreaterThan(CrewManagementModel.GetCannonDamageMultiplier(low)));
            Assert.That(CrewManagementModel.GetStatusProcChance(high, CrewPerk.Firebrand), Is.GreaterThan(CrewManagementModel.GetStatusProcChance(low, CrewPerk.Firebrand)));
        }

        [Test]
        public void BossPerkRankTableMakesTopRanksRarest()
        {
            int[] counts = new int[PerkRankModel.RankCount];
            for (ulong id = 1; id <= 20000; id++)
            {
                PerkDrop drop = BossModel.RollPerkDrop(BossKind.Poseidon, id);
                if (!drop.IsEmpty) counts[(int)drop.Rank]++;
            }
            Assert.That(counts[0], Is.GreaterThan(counts[1]));
            Assert.That(counts[1], Is.GreaterThan(counts[2]));
            Assert.That(counts[2], Is.GreaterThan(counts[3]));
            Assert.That(counts[3], Is.GreaterThan(0));
        }

        [Test]
        public void EnemyIdentityIsStableAndBossesKeepClearNamesAndLevels()
        {
            const ulong id = 0xA123B456C789D012UL;
            string first = EnemyIdentityModel.GetName(id, BossKind.None);
            string second = EnemyIdentityModel.GetName(id, BossKind.None);
            Assert.That(second, Is.EqualTo(first));
            Assert.That(first, Is.Not.Empty);
            Assert.That(EnemyIdentityModel.GetLevel(BossKind.None, 42, new Vector2(180f, -90f)), Is.GreaterThan(1));
            Assert.That(EnemyIdentityModel.GetName(id, BossKind.Kraken), Is.EqualTo("KRAKEN"));
            Assert.That(EnemyIdentityModel.GetLevel(BossKind.Poseidon, 0, Vector2.zero), Is.EqualTo(80));
        }

        [Test]
        public void RepeatedUiPicturesUseOnePrimaryPixelSize()
        {
            Assert.That(UiLayoutMetrics.PrimaryIcon, Is.EqualTo(40f));
            Assert.That(UiLayoutMetrics.HudCardWidth, Is.GreaterThan(160f));
            Assert.That(UiLayoutMetrics.HudCardSpacing, Is.GreaterThan(UiLayoutMetrics.HudCardWidth));
            Assert.That(UiLayoutMetrics.HudCardSpacing * 3f + UiLayoutMetrics.HudCardWidth, Is.LessThanOrEqualTo(UiLayoutMetrics.HudDashboardWidth));
            Assert.That(UiLayoutMetrics.HudCardSafeWidth, Is.LessThan(UiLayoutMetrics.HudCardWidth - UiLayoutMetrics.PrimaryIcon));
            Assert.That(UiLayoutMetrics.PortServiceButtonWidth, Is.LessThan(UiLayoutMetrics.PortServiceBoardWidth));
        }

        [Test]
        public void WakeRecordsSailedCoordinatesAndFadesInsteadOfFollowingTheShip()
        {
            Vector2 stern = WakeTrailMath.GetSternPosition(Vector2.zero, 0f, 1);
            Assert.That(stern.y, Is.LessThan(0f));
            Assert.That(WakeTrailMath.ShouldEmit(Vector2.zero, Vector2.up * WakeTrailMath.SegmentSpacing, 1f), Is.True);
            Assert.That(WakeTrailMath.ShouldEmit(Vector2.zero, Vector2.up, 0f), Is.False);
            Assert.That(WakeTrailMath.IsTeleport(Vector2.zero, Vector2.right * 10f), Is.True);

            Vector2 recorded = new Vector2(2f, 4f);
            Assert.That(WakeTrailMath.ToCameraRelative(recorded, new Vector2(1f, 1f)), Is.EqualTo(new Vector2(1f, 3f)));
            Assert.That(WakeTrailMath.GetOpacity(0.5f, WakeTrailMath.Lifetime), Is.GreaterThan(0.5f));
            Assert.That(WakeTrailMath.GetOpacity(WakeTrailMath.Lifetime, WakeTrailMath.Lifetime), Is.Zero);
        }

        [Test]
        public void CannonDamageUsesCombatScaleAndStackedPerks()
        {
            var state = new GameState { Crew = 2, ShipLevel = 3, CannonLevel = 2 };
            state.SetRoleCrew(CrewRole.Sails, 0);
            state.SetRoleCrew(CrewRole.Anchor, 0);
            state.SetRoleCrew(CrewRole.Cannons, 2);
            state.AddPerk(CrewPerk.PowderExpert, 2);
            Assert.That(state.TryEquipPerkStack(CrewRole.Cannons, CrewPerk.PowderExpert), Is.True);
            Assert.That(state.TryEquipPerkStack(CrewRole.Cannons, CrewPerk.PowderExpert), Is.True);

            var salvo = new SalvoSolution(2, 2, "PORT");
            Assert.That(ShipCustomizationModel.GetSalvoDamage(state, salvo), Is.EqualTo(225));
            state.CrewPerformanceMultiplier = 0.5f;
            Assert.That(ShipCustomizationModel.GetSalvoDamage(state, salvo), Is.EqualTo(188));
        }

        [Test]
        public void StatusRuntimeDealsScaledBurningDamageAndCuresDangerFirst()
        {
            var statuses = new ShipStatusRuntime();
            statuses.Apply(ShipStatus.Sticky, 10f);
            statuses.Apply(ShipStatus.Poisoned, 10f);
            statuses.Apply(ShipStatus.Burning, 10f);

            Assert.That(statuses.Tick(1f, 10000), Is.EqualTo(60));
            Assert.That(statuses.CurePriority(), Is.EqualTo(ShipStatus.Burning));
            Assert.That(statuses.CurePriority(), Is.EqualTo(ShipStatus.Poisoned));
            Assert.That(statuses.IsActive(ShipStatus.Sticky), Is.True);
        }

        [Test]
        public void PerkSlotsFollowAssignedCrewAndAllowDuplicateStacks()
        {
            var state = new GameState { Crew = 3 };
            state.SetRoleCrew(CrewRole.Sails, 0);
            state.SetRoleCrew(CrewRole.Anchor, 0);
            state.SetRoleCrew(CrewRole.Repairer, 2);
            state.AddPerk(CrewPerk.RapidRepair, 3);

            Assert.That(state.TryEquipPerkStack(CrewRole.Repairer, CrewPerk.RapidRepair), Is.True);
            Assert.That(state.TryEquipPerkStack(CrewRole.Repairer, CrewPerk.RapidRepair), Is.True);
            Assert.That(state.TryEquipPerkStack(CrewRole.Repairer, CrewPerk.RapidRepair), Is.False);
            Assert.That(state.GetEquippedPerkCount(CrewRole.Repairer, CrewPerk.RapidRepair), Is.EqualTo(2));
        }

        [Test]
        public void CaptainRecordRoundTripsInVersionSevenSave()
        {
            var state = new GameState();
            state.Captain.AddDistance(123.456f);
            state.Captain.DamageDealt = 98765;
            state.Captain.GoldEarned = 4321;
            state.Captain.WrecksSalvaged = 12;
            state.Captain.TreasuresFound = 7;
            state.Captain.PortCalls = 9;
            state.Captain.RecordEnemy(EnemyArchetype.Ironclad);
            state.Captain.RecordBoss(BossKind.Kraken, BossMutation.Corrupted);
            state.Captain.DiscoverRegion(SeaRegionKind.Frostwake);

            GameState restored = CompactSaveCodec.Deserialize(CompactSaveCodec.Serialize(state));
            Assert.That(restored.Captain.DistanceSailed, Is.EqualTo(123.45f).Within(0.02f));
            Assert.That(restored.Captain.DamageDealt, Is.EqualTo(98765));
            Assert.That(restored.Captain.GoldEarned, Is.EqualTo(4321));
            Assert.That(restored.Captain.WrecksSalvaged, Is.EqualTo(12));
            Assert.That(restored.Captain.GetEnemyCount(EnemyArchetype.Ironclad), Is.EqualTo(1));
            Assert.That(restored.Captain.GetBossCount(BossKind.Kraken), Is.EqualTo(1));
            Assert.That(restored.Captain.GetMutationCount(BossMutation.Corrupted), Is.EqualTo(1));
            Assert.That(restored.Captain.HasDiscoveredRegion(SeaRegionKind.Frostwake), Is.True);
        }

        [Test]
        public void InfiniteWorldProducesSeveralEnemyArchetypesDeterministically()
        {
            var events = new List<GeneratedEventData>();
            var found = new HashSet<EnemyArchetype>();
            for (int y = -18; y <= 18; y++)
            for (int x = -18; x <= 18; x++)
            {
                WorldGenerator.GenerateChunk(GameState.DefaultWorldSeed, x, y, events);
                foreach (GeneratedEventData item in events)
                    if (item.Kind == PoiKind.Enemy && item.Boss == BossKind.None) found.Add(item.EnemyArchetype);
            }
            Assert.That(found.Count, Is.EqualTo(EnemyArchetypeModel.Count));

            ulong entropy = 0x123456789ABCDEF0UL;
            Vector2 position = new Vector2(900f, -450f);
            Assert.That(EnemyArchetypeModel.Roll(entropy, position), Is.EqualTo(EnemyArchetypeModel.Roll(entropy, position)));
            Assert.That(EnemyArchetypeModel.Get(EnemyArchetype.Skirmisher).SpeedMultiplier,
                Is.GreaterThan(EnemyArchetypeModel.Get(EnemyArchetype.Ironclad).SpeedMultiplier));
            Assert.That(EnemyArchetypeModel.GetHull(EnemyArchetype.Ironclad, 40, 10),
                Is.GreaterThan(EnemyArchetypeModel.GetHull(EnemyArchetype.Skirmisher, 40, 10)));
        }

        [Test]
        public void SeaRegionsAreStableAndChangeNavigationRules()
        {
            Assert.That(SeaRegionModel.At(GameState.DefaultWorldSeed, Vector2.zero).Kind, Is.EqualTo(SeaRegionKind.Calm));
            var found = new HashSet<SeaRegionKind>();
            for (int y = -12; y <= 12; y++)
            for (int x = -12; x <= 12; x++)
            {
                Vector2 position = new Vector2(x, y) * WorldGenerator.ChunkSize * SeaRegionModel.RegionChunkSpan;
                SeaRegionProfile first = SeaRegionModel.At(GameState.DefaultWorldSeed, position);
                SeaRegionProfile second = SeaRegionModel.At(GameState.DefaultWorldSeed, position);
                Assert.That(second.Kind, Is.EqualTo(first.Kind));
                found.Add(first.Kind);
            }
            Assert.That(found.Count, Is.EqualTo(SeaRegionModel.Count));
            Assert.That(SeaRegionModel.Get(SeaRegionKind.TarSea).TurnMultiplier, Is.LessThan(1f));
            Assert.That(SeaRegionModel.Get(SeaRegionKind.EmberCurrent).SpeedMultiplier, Is.GreaterThan(1f));
        }

        [Test]
        public void EveryBossReceivesAVisibleGameplayMutation()
        {
            for (int bossValue = 1; bossValue < BossMutationModel.BossCount; bossValue++)
            {
                BossKind boss = (BossKind)bossValue;
                BossMutation first = BossMutationModel.Roll((ulong)(1000 + bossValue), boss, new Vector2(200f, 100f));
                BossMutation second = BossMutationModel.Roll((ulong)(1000 + bossValue), boss, new Vector2(200f, 100f));
                Assert.That(first, Is.Not.EqualTo(BossMutation.None));
                Assert.That(second, Is.EqualTo(first));
                Assert.That(BossMutationModel.GetLabel(first), Is.Not.Empty);
            }
            Assert.That(BossMutationModel.ApplyHull(1000, BossMutation.Armored), Is.GreaterThan(1000));
            Assert.That(BossMutationModel.DamageMultiplier(BossMutation.Frenzied), Is.GreaterThan(1f));
            Assert.That(BossMutationModel.SpeedMultiplier(BossMutation.Swift), Is.GreaterThan(1f));
            Assert.That(BossMutationModel.StatusPotency(BossMutation.Corrupted), Is.GreaterThan(1f));
        }
    }
}
