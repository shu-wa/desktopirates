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
    }
}
