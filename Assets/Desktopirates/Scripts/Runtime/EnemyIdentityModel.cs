using UnityEngine;

namespace Desktopirates
{
    public static class EnemyIdentityModel
    {
        private static readonly string[] Adjectives =
        {
            "ASHEN", "BLACKTIDE", "BRINE", "CINDER", "IRON", "MOONLIT", "RED", "STORM"
        };

        public static int GetLevel(BossKind boss, int reward, Vector2 position)
        {
            if (boss != BossKind.None) return boss switch
            {
                BossKind.GangAdmiral => 12,
                BossKind.GhostShip => 28,
                BossKind.Kraken => 50,
                BossKind.Poseidon => 80,
                _ => 1
            };

            int chunkRange = Mathf.FloorToInt(Mathf.Max(Mathf.Abs(position.x), Mathf.Abs(position.y)) / WorldGenerator.ChunkSize);
            int distanceTier = chunkRange / 4;
            int rewardTier = Mathf.Max(0, reward - 18) / 12;
            return Mathf.Clamp(1 + distanceTier + rewardTier, 1, 99);
        }

        public static string GetName(ulong id, BossKind boss, EnemyArchetype archetype = EnemyArchetype.Corsair)
        {
            if (boss != BossKind.None) return boss switch
            {
                BossKind.GangAdmiral => "GANG ADMIRAL",
                BossKind.GhostShip => "GHOST SHIP",
                BossKind.Kraken => "KRAKEN",
                BossKind.Poseidon => "POSEIDON",
                _ => "BOSS"
            };

            ulong hash = WorldGenerator.Hash(unchecked((int)id), unchecked((int)(id >> 32)), 0, 4103);
            string adjective = Adjectives[(int)(hash % (ulong)Adjectives.Length)];
            string vesselClass = EnemyArchetypeModel.Get(archetype).ClassName;
            return $"{adjective} {vesselClass}";
        }
    }
}
