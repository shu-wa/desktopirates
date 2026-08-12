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
        public void CompactSaveRoundTripsAllGameplayState()
        {
            var state = new GameState
            {
                WorldSeed = 7812,
                PlayerPosition = new Vector2(-123.375f, 456.625f),
                HeadingDegrees = 271.4f,
                Gold = 9021,
                Hull = 13,
                MaxHull = 17,
                Supplies = 28,
                EngineLevel = 4,
                CannonLevel = 3
            };
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
    }
}
