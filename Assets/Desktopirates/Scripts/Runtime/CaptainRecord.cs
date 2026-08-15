using UnityEngine;

namespace Desktopirates
{
    public sealed class CaptainRecord
    {
        private readonly int[] enemiesSunk = new int[EnemyArchetypeModel.Count];
        private readonly int[] bossesDefeated = new int[BossMutationModel.BossCount];
        private readonly int[] mutationsDefeated = new int[BossMutationModel.MutationCount];
        private double distanceRemainder;

        public long DistanceHundredths { get; private set; }
        public long DamageDealt { get; set; }
        public long GoldEarned { get; set; }
        public int WrecksSalvaged { get; set; }
        public int TreasuresFound { get; set; }
        public int PortCalls { get; set; }
        public uint DiscoveredRegionMask { get; private set; }

        public float DistanceSailed => DistanceHundredths / 100f;

        public int TotalEnemiesSunk
        {
            get
            {
                int total = 0;
                for (int i = 0; i < enemiesSunk.Length; i++) total += enemiesSunk[i];
                return total;
            }
        }

        public int TotalBossesDefeated
        {
            get
            {
                int total = 0;
                for (int i = 1; i < bossesDefeated.Length; i++) total += bossesDefeated[i];
                return total;
            }
        }

        public void AddDistance(float logicalUnits)
        {
            if (logicalUnits <= 0f) return;
            distanceRemainder += logicalUnits * 100.0;
            long whole = (long)distanceRemainder;
            DistanceHundredths += whole;
            distanceRemainder -= whole;
        }

        public void SetDistanceHundredths(long value) => DistanceHundredths = System.Math.Max(0L, value);

        public void RecordEnemy(EnemyArchetype archetype)
            => enemiesSunk[Mathf.Clamp((int)archetype, 0, enemiesSunk.Length - 1)]++;

        public int GetEnemyCount(EnemyArchetype archetype)
            => enemiesSunk[Mathf.Clamp((int)archetype, 0, enemiesSunk.Length - 1)];

        public void SetEnemyCount(EnemyArchetype archetype, int value)
            => enemiesSunk[Mathf.Clamp((int)archetype, 0, enemiesSunk.Length - 1)] = Mathf.Max(0, value);

        public void RecordBoss(BossKind boss, BossMutation mutation)
        {
            if (boss != BossKind.None) bossesDefeated[Mathf.Clamp((int)boss, 0, bossesDefeated.Length - 1)]++;
            if (mutation != BossMutation.None) mutationsDefeated[Mathf.Clamp((int)mutation, 0, mutationsDefeated.Length - 1)]++;
        }

        public int GetBossCount(BossKind boss)
            => bossesDefeated[Mathf.Clamp((int)boss, 0, bossesDefeated.Length - 1)];

        public void SetBossCount(BossKind boss, int value)
            => bossesDefeated[Mathf.Clamp((int)boss, 0, bossesDefeated.Length - 1)] = Mathf.Max(0, value);

        public int GetMutationCount(BossMutation mutation)
            => mutationsDefeated[Mathf.Clamp((int)mutation, 0, mutationsDefeated.Length - 1)];

        public void SetMutationCount(BossMutation mutation, int value)
            => mutationsDefeated[Mathf.Clamp((int)mutation, 0, mutationsDefeated.Length - 1)] = Mathf.Max(0, value);

        public bool DiscoverRegion(SeaRegionKind kind)
        {
            uint bit = 1u << Mathf.Clamp((int)kind, 0, 31);
            bool first = (DiscoveredRegionMask & bit) == 0;
            DiscoveredRegionMask |= bit;
            return first;
        }

        public bool HasDiscoveredRegion(SeaRegionKind kind) => (DiscoveredRegionMask & (1u << (int)kind)) != 0;
        public void SetDiscoveredRegionMask(uint value) => DiscoveredRegionMask = value;
    }
}
