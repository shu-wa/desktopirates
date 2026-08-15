using UnityEngine;

namespace Desktopirates
{
    public enum EnemyArchetype : byte
    {
        Corsair,
        Skirmisher,
        Gunboat,
        FireRaider,
        PlagueRaider,
        FrostCutter,
        Ironclad,
        Hunter
    }

    public readonly struct EnemyProfile
    {
        public readonly string ClassName;
        public readonly float HullMultiplier;
        public readonly float SpeedMultiplier;
        public readonly float FireRange;
        public readonly float PreferredRange;
        public readonly float ReloadSeconds;
        public readonly int BaseDamage;
        public readonly ShipStatus Status;
        public readonly float StatusChance;

        public EnemyProfile(string className, float hull, float speed, float fireRange, float preferredRange, float reload, int damage, ShipStatus status, float statusChance)
        {
            ClassName = className;
            HullMultiplier = hull;
            SpeedMultiplier = speed;
            FireRange = fireRange;
            PreferredRange = preferredRange;
            ReloadSeconds = reload;
            BaseDamage = damage;
            Status = status;
            StatusChance = statusChance;
        }
    }

    public static class EnemyArchetypeModel
    {
        public const int Count = 8;

        public static EnemyArchetype Roll(ulong entropy, Vector2 position)
        {
            int range = Mathf.FloorToInt(Mathf.Max(Mathf.Abs(position.x), Mathf.Abs(position.y)) / WorldGenerator.ChunkSize);
            int unlocked = Mathf.Clamp(3 + range / 3, 3, Count);
            return (EnemyArchetype)((entropy >> 9) % (ulong)unlocked);
        }

        public static EnemyProfile Get(EnemyArchetype archetype) => archetype switch
        {
            EnemyArchetype.Skirmisher => new EnemyProfile("SKIRMISHER", 0.72f, 1.48f, 2.15f, 1.55f, 1.75f, 18, ShipStatus.None, 0f),
            EnemyArchetype.Gunboat => new EnemyProfile("GUNBOAT", 1.24f, 0.78f, 3.35f, 2.75f, 3.15f, 42, ShipStatus.None, 0f),
            EnemyArchetype.FireRaider => new EnemyProfile("FIRE RAIDER", 0.92f, 1.12f, 2.30f, 1.65f, 2.65f, 25, ShipStatus.Burning, 0.48f),
            EnemyArchetype.PlagueRaider => new EnemyProfile("PLAGUE RAIDER", 0.96f, 1.02f, 2.55f, 1.90f, 2.90f, 23, ShipStatus.Poisoned, 0.44f),
            EnemyArchetype.FrostCutter => new EnemyProfile("FROST CUTTER", 1.02f, 1.10f, 2.65f, 2.00f, 3.05f, 24, ShipStatus.Frozen, 0.40f),
            EnemyArchetype.Ironclad => new EnemyProfile("IRONCLAD", 1.86f, 0.58f, 2.75f, 2.15f, 3.80f, 50, ShipStatus.None, 0f),
            EnemyArchetype.Hunter => new EnemyProfile("HUNTER", 1.30f, 1.24f, 3.05f, 2.35f, 2.35f, 35, ShipStatus.Sticky, 0.34f),
            _ => new EnemyProfile("CORSAIR", 1.00f, 1.00f, 2.35f, 1.45f, 2.70f, 30, ShipStatus.None, 0f)
        };

        public static int GetHull(EnemyArchetype archetype, int reward, int level)
            => Mathf.Max(80, Mathf.RoundToInt((140 + reward * 8 + level * 6) * Get(archetype).HullMultiplier));

        public static int GetDamage(EnemyArchetype archetype, int level)
            => Mathf.RoundToInt(Get(archetype).BaseDamage * (1f + Mathf.Max(0, level - 1) * 0.025f));
    }
}
