using UnityEngine;

namespace Desktopirates
{
    /// <summary>Logical-world hull footprints; rendering scale never substitutes for collision size.</summary>
    public static class ShipCollisionModel
    {
        public static float GetPlayerRadius(int shipLevel)
            => 0.34f + ShipProgressionModel.Get(shipLevel).HullScale * 0.30f;

        public static float GetEnemyRadius(EnemyArchetype archetype, BossKind boss)
        {
            if (boss != BossKind.None)
            {
                return boss switch
                {
                    BossKind.Kraken => 1.22f,
                    BossKind.Poseidon => 1.10f,
                    BossKind.GhostShip => 0.96f,
                    _ => 0.90f
                };
            }
            return archetype switch
            {
                EnemyArchetype.Skirmisher => 0.46f,
                EnemyArchetype.FireRaider => 0.54f,
                EnemyArchetype.PlagueRaider => 0.55f,
                EnemyArchetype.FrostCutter => 0.52f,
                EnemyArchetype.Gunboat => 0.68f,
                EnemyArchetype.Hunter => 0.62f,
                EnemyArchetype.Ironclad => 0.82f,
                _ => 0.59f
            };
        }

        public static bool TryGetSeparation(Vector2 first, float firstRadius, Vector2 second, float secondRadius,
            out Vector2 firstDisplacement, out Vector2 secondDisplacement)
        {
            firstDisplacement = Vector2.zero;
            secondDisplacement = Vector2.zero;
            float minimumDistance = Mathf.Max(0f, firstRadius) + Mathf.Max(0f, secondRadius);
            Vector2 delta = second - first;
            float distance = delta.magnitude;
            if (distance >= minimumDistance || minimumDistance <= 0f) return false;
            Vector2 normal = distance > 0.0001f ? delta / distance : Vector2.right;
            float overlap = minimumDistance - distance;
            float totalRadius = Mathf.Max(0.0001f, firstRadius + secondRadius);
            // Larger hulls carry more momentum; the smaller hull yields farther.
            firstDisplacement = -normal * overlap * (secondRadius / totalRadius);
            secondDisplacement = normal * overlap * (firstRadius / totalRadius);
            return true;
        }
    }
}
