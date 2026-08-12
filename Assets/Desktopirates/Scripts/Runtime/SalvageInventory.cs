using System;

namespace Desktopirates
{
    public enum SalvagePartKind : byte
    {
        Timber,
        Canvas,
        Iron,
        Gear,
        Chart,
        Relic
    }

    public readonly struct SalvageDrop
    {
        public readonly SalvagePartKind Kind;
        public readonly int Amount;

        public SalvageDrop(SalvagePartKind kind, int amount)
        {
            Kind = kind;
            Amount = amount;
        }
    }

    public static class SalvageInventory
    {
        public const int PartKindCount = 6;

        public static SalvageDrop[] RollWreck(ulong eventId, int reward)
        {
            ulong mixed = Mix(eventId ^ ((ulong)(uint)reward << 32));
            int timber = 1 + (int)(mixed & 3UL);
            SalvagePartKind secondary = (SalvagePartKind)(1 + (int)((mixed >> 8) % 4UL));
            int secondaryAmount = 1 + (int)((mixed >> 12) & 1UL);
            return new[]
            {
                new SalvageDrop(SalvagePartKind.Timber, timber),
                new SalvageDrop(secondary, secondaryAmount)
            };
        }

        public static SalvageDrop RollTreasure(ulong eventId)
        {
            return (Mix(eventId) & 3UL) == 0UL
                ? new SalvageDrop(SalvagePartKind.Relic, 1)
                : new SalvageDrop(SalvagePartKind.Chart, 1);
        }

        public static string GetDisplayName(SalvagePartKind kind)
        {
            return kind switch
            {
                SalvagePartKind.Timber => "TIMBER",
                SalvagePartKind.Canvas => "CANVAS",
                SalvagePartKind.Iron => "IRON",
                SalvagePartKind.Gear => "GEAR",
                SalvagePartKind.Chart => "CHART",
                SalvagePartKind.Relic => "RELIC",
                _ => kind.ToString().ToUpperInvariant()
            };
        }

        public static string GetDescription(SalvagePartKind kind)
        {
            return kind switch
            {
                SalvagePartKind.Timber => "Salt-worn planks for hull repairs.",
                SalvagePartKind.Canvas => "Aged canvas for sails and rigging.",
                SalvagePartKind.Iron => "Fittings for reinforced ship parts.",
                SalvagePartKind.Gear => "Mechanisms for engines and workshops.",
                SalvagePartKind.Chart => "Fragments that reveal nearby waters.",
                SalvagePartKind.Relic => "A rare keepsake from a lost voyage.",
                _ => string.Empty
            };
        }

        private static ulong Mix(ulong value)
        {
            value ^= value >> 30;
            value *= 0xbf58476d1ce4e5b9UL;
            value ^= value >> 27;
            value *= 0x94d049bb133111ebUL;
            return value ^ (value >> 31);
        }
    }
}
