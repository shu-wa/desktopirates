using UnityEngine;

namespace Desktopirates
{
    public enum BossMutation : byte { None, Armored, Frenzied, Swift, Corrupted }

    public static class BossMutationModel
    {
        public const int MutationCount = 5;
        public const int BossCount = 5;

        public static BossMutation Roll(ulong encounterId, BossKind boss, Vector2 position)
        {
            if (boss == BossKind.None) return BossMutation.None;
            ulong hash = WorldGenerator.Hash(unchecked((int)encounterId), unchecked((int)(encounterId >> 32)), (int)boss, 7727);
            return (BossMutation)(1 + (int)(hash % (ulong)(MutationCount - 1)));
        }

        public static string GetLabel(BossMutation mutation) => mutation switch
        {
            BossMutation.Armored => "ARMORED",
            BossMutation.Frenzied => "FRENZIED",
            BossMutation.Swift => "SWIFT",
            BossMutation.Corrupted => "CORRUPTED",
            _ => string.Empty
        };

        public static int ApplyHull(int baseHull, BossMutation mutation)
            => Mathf.RoundToInt(baseHull * (mutation == BossMutation.Armored ? 1.55f : mutation == BossMutation.Corrupted ? 1.18f : 1f));

        public static float DamageMultiplier(BossMutation mutation)
            => mutation == BossMutation.Frenzied ? 1.45f : mutation == BossMutation.Corrupted ? 1.18f : 1f;

        public static float SpeedMultiplier(BossMutation mutation)
            => mutation == BossMutation.Swift ? 1.48f : mutation == BossMutation.Frenzied ? 1.12f : 1f;

        public static float ReloadMultiplier(BossMutation mutation)
            => mutation == BossMutation.Frenzied ? 0.68f : mutation == BossMutation.Swift ? 0.82f : 1f;

        public static float StatusPotency(BossMutation mutation)
            => mutation == BossMutation.Corrupted ? 1.65f : 1f;

        public static float RewardMultiplier(BossMutation mutation)
            => mutation == BossMutation.None ? 1f : mutation == BossMutation.Corrupted ? 1.55f : 1.35f;

        public static Color GetColor(BossMutation mutation) => mutation switch
        {
            BossMutation.Armored => new Color(0.72f, 0.78f, 0.82f),
            BossMutation.Frenzied => new Color(1f, 0.18f, 0.06f),
            BossMutation.Swift => new Color(0.16f, 0.92f, 1f),
            BossMutation.Corrupted => new Color(0.72f, 0.16f, 0.95f),
            _ => Color.white
        };
    }
}
