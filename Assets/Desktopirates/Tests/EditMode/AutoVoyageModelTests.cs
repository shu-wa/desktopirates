using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Desktopirates.Tests
{
    public sealed class AutoVoyageModelTests
    {
        [TestCase(0f, 90f, 1f)]
        [TestCase(0f, -90f, -1f)]
        [TestCase(350f, 10f, 0.5882353f)]
        public void PilotSteersAcrossTheShortestHeadingArc(float current, float desired, float expected)
        {
            Assert.That(AutoVoyageModel.GetSteeringInput(current, desired), Is.EqualTo(expected).Within(0.001f));
        }

        [Test]
        public void EscapeWaypointAlwaysLeadsAwayFromEnemy()
        {
            Vector2 player = new Vector2(2f, 1f);
            Vector2 enemy = new Vector2(4f, 2f);
            Vector2 waypoint = AutoVoyageModel.GetEscapeWaypoint(player, enemy);
            Assert.That(Vector2.Dot(waypoint - player, player - enemy), Is.GreaterThan(0f));
            Assert.That(Vector2.Distance(player, waypoint), Is.EqualTo(AutoVoyageModel.EscapeLegDistance).Within(0.001f));
        }

        [Test]
        public void CombatWaypointCreatesABroadsideOrbit()
        {
            Vector2 enemy = new Vector2(5f, 4f);
            Vector2 waypoint = AutoVoyageModel.GetCombatWaypoint(Vector2.zero, enemy, 24UL);
            Assert.That(Vector2.Distance(enemy, waypoint), Is.EqualTo(AutoVoyageModel.CombatOrbitRadius).Within(0.001f));
            Vector2 radial = (Vector2.zero - enemy).normalized;
            Vector2 course = (waypoint - enemy).normalized;
            Assert.That(Mathf.Abs(Vector2.Dot(radial, course)), Is.LessThan(0.35f));
        }

        [Test]
        public void LandmarkSearchUsesTheInfiniteWorldGenerator()
        {
            bool found = AutoVoyageModel.TryFindNearestLandmark(GameState.DefaultWorldSeed, Vector2.zero,
                new HashSet<ulong>(), true, true, out GeneratedEventData landmark);
            Assert.That(found, Is.True);
            Assert.That(landmark.Kind, Is.Not.EqualTo(PoiKind.Enemy));
        }

        [Test]
        public void BossHarborRouteEndsAtADeterministicPort()
        {
            bool found = AutoVoyageModel.TryFindBossHarbor(GameState.DefaultWorldSeed, Vector2.zero,
                new HashSet<ulong>(), out GeneratedEventData boss, out GeneratedEventData harbor);
            Assert.That(found, Is.True);
            Assert.That(boss.Boss, Is.Not.EqualTo(BossKind.None));
            Assert.That(harbor.Kind, Is.EqualTo(PoiKind.Port));
            Assert.That(AutoVoyageModel.TryFindBossHarbor(GameState.DefaultWorldSeed, Vector2.zero,
                new HashSet<ulong>(), out GeneratedEventData repeatedBoss, out GeneratedEventData repeatedHarbor), Is.True);
            Assert.That(repeatedBoss.Id, Is.EqualTo(boss.Id));
            Assert.That(repeatedHarbor.Id, Is.EqualTo(harbor.Id));
        }
    }
}
