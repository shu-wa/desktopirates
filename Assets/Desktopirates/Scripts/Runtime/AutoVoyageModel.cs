using System;
using System.Collections.Generic;
using UnityEngine;

namespace Desktopirates
{
    public enum AutoEncounterPolicy : byte
    {
        Avoid,
        Fight,
        Observe
    }

    public enum AutoDestinationMode : byte
    {
        FreeRoam,
        NearestLandmark,
        BossSignal,
        BossHarbor,
        NearestHarbor
    }

    /// <summary>Pure navigation decisions shared by the runtime pilot and EditMode tests.</summary>
    public static class AutoVoyageModel
    {
        public const float WaypointArrivalDistance = 0.85f;
        public const float AutoCollectRange = 2.10f;
        public const float AutoCollectAcquireRange = 5.40f;
        public const float SalvageCoastTurnAngle = 38f;
        public const float SalvageRudderFlowSpeed = 0.16f;
        public const float EnemyAwarenessRange = 6.6f;
        public const float EscapeLegDistance = 8.5f;
        public const float CombatOrbitRadius = 2.15f;
        public const float PortPilotRange = 3.30f;
        public const float SlowTurnAngle = 62f;
        public const int LandmarkSearchRadius = 9;
        public const int BossHarborSearchRadius = 12;

        public static float GetSteeringInput(float currentHeading, float desiredHeading)
            => Mathf.Clamp(Mathf.DeltaAngle(currentHeading, desiredHeading) / 34f, -1f, 1f);

        /// <summary>
        /// Salvage uses the ship's inertia instead of alternating full and minimum power.
        /// A short step-one pulse restores rudder flow only after the hull has almost stopped.
        /// </summary>
        public static int GetSalvageCruiseStep(float distance, float headingError, float speed, int maxCruiseStep)
        {
            if (distance <= PoiSystem.SalvageRange) return 0;
            if (Mathf.Abs(headingError) >= SalvageCoastTurnAngle)
                return speed > SalvageRudderFlowSpeed ? 0 : 1;
            if (distance <= 2.75f)
                return speed > 0.58f ? 0 : 1;
            return Mathf.Clamp(distance > 4.1f ? 2 : 1, 1, Mathf.Max(1, maxCruiseStep));
        }

        public static Vector2 GetEscapeWaypoint(Vector2 player, Vector2 enemy)
        {
            Vector2 away = player - enemy;
            if (away.sqrMagnitude < 0.0001f) away = Vector2.down;
            return player + away.normalized * EscapeLegDistance;
        }

        public static Vector2 GetCombatWaypoint(Vector2 player, Vector2 enemy, ulong enemyId)
        {
            Vector2 radial = player - enemy;
            if (radial.sqrMagnitude < 0.0001f) radial = Vector2.down;
            radial.Normalize();
            Vector2 tangent = (enemyId & 1UL) == 0UL
                ? new Vector2(-radial.y, radial.x)
                : new Vector2(radial.y, -radial.x);
            // A small radial component prevents a perfect circular stalemate while the
            // tangent presents the broadside batteries to the target.
            return enemy + (tangent * 0.82f + radial * 0.18f).normalized * CombatOrbitRadius;
        }

        public static bool TryFindNearestLandmark(int seed, Vector2 origin, ISet<ulong> resolved,
            bool includeWrecks, bool includeTreasure, out GeneratedEventData nearest)
        {
            return TryFindNearest(seed, origin, LandmarkSearchRadius, resolved, candidate =>
                (includeWrecks && candidate.Kind == PoiKind.Wreck)
                || (includeTreasure && candidate.Kind == PoiKind.Treasure), out nearest);
        }

        public static bool TryFindNearestHarbor(int seed, Vector2 origin, out GeneratedEventData harbor)
            => TryFindNearest(seed, origin, LandmarkSearchRadius, null,
                candidate => candidate.Kind == PoiKind.Port, out harbor);

        public static bool TryFindBossHarbor(int seed, Vector2 origin, ISet<ulong> resolved,
            out GeneratedEventData boss, out GeneratedEventData harbor)
        {
            harbor = default;
            if (!WorldGenerator.TryFindNearestBoss(seed, origin, resolved, out boss)) return false;
            return TryFindNearest(seed, boss.Position, BossHarborSearchRadius, null,
                candidate => candidate.Kind == PoiKind.Port, out harbor);
        }

        public static Vector2 CreateFreeRoamWaypoint(int seed, Vector2 origin, int leg)
        {
            Vector2Int chunk = WorldStreamingModel.GetCenterChunk(origin);
            ulong hash = WorldGenerator.Hash(seed, chunk.x, chunk.y, 18031 + leg * 17);
            float angle = (hash & 0xFFFFUL) / 65535f * Mathf.PI * 2f;
            float distance = 22f + ((hash >> 16) & 0xFFUL) / 255f * 16f;
            return origin + new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * distance;
        }

        private static bool TryFindNearest(int seed, Vector2 origin, int radius, ISet<ulong> resolved,
            Func<GeneratedEventData, bool> predicate, out GeneratedEventData nearest)
        {
            nearest = default;
            Vector2Int center = WorldStreamingModel.GetCenterChunk(origin);
            float bestDistanceSquared = float.MaxValue;
            bool found = false;
            var generated = new List<GeneratedEventData>();

            for (int ring = 0; ring <= radius; ring++)
            {
                for (int y = center.y - ring; y <= center.y + ring; y++)
                for (int x = center.x - ring; x <= center.x + ring; x++)
                {
                    if (Mathf.Max(Mathf.Abs(x - center.x), Mathf.Abs(y - center.y)) != ring) continue;
                    WorldGenerator.GenerateChunk(seed, x, y, generated);
                    foreach (GeneratedEventData candidate in generated)
                    {
                        if (!predicate(candidate) || (resolved != null && resolved.Contains(candidate.Id))) continue;
                        float distanceSquared = (candidate.Position - origin).sqrMagnitude;
                        if (distanceSquared >= bestDistanceSquared) continue;
                        nearest = candidate;
                        bestDistanceSquared = distanceSquared;
                        found = true;
                    }
                }

                // Once a full extra ring beyond the current winner has been searched,
                // no later chunk can contain a closer point than that winner.
                if (found && ring * WorldGenerator.ChunkSize > Mathf.Sqrt(bestDistanceSquared) + WorldGenerator.ChunkSize) break;
            }
            return found;
        }
    }
}
