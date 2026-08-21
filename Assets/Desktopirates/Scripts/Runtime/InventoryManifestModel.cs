using UnityEngine;

namespace Desktopirates
{
    public enum InventoryItemKind : byte
    {
        Food,
        Water,
        Supplies,
        SpareCannon,
        Timber,
        Canvas,
        Iron,
        Gear,
        Chart,
        Relic
    }

    public enum InventoryRarity : byte
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    /// <summary>A display-only manifest over every cargo-backed value in GameState.</summary>
    public static class InventoryManifestModel
    {
        public const int EntryCount = 10;
        public const float RowHeight = 62f;
        public const float RowStride = 66f;
        public const float ViewportHeight = 411f;
        public const float ReferenceCanvasWidth = 720f;
        public const float ReferenceCanvasHeight = 760f;
        public const float PanelWidth = 516f;
        public const float PanelHeight = 510f;
        public const float PanelCenterX = 0f;
        public const float PanelTopOffset = 495f;
        public const float PanelFillInset = 14f;
        public const float MenuCircleBottom = 132f;
        public const float HudDashboardBottom = 229f;
        public const float TitleCenterY = 226f;
        public const float TitleHeight = 64f;
        public const float SummaryCenterY = 181f;
        public const float ViewportCenterY = -46.5f;
        public const float IconCenterX = -174f;
        public const float IconSize = 54f;
        public const float TextCenterX = -1f;
        public const float TextWidth = 270f;
        public const float CountCenterX = 177f;
        public const float CountWidth = 62f;

        public static float ContentHeight => EntryCount * RowStride + 8f;

        public static bool RequiresScrolling => ContentHeight > ViewportHeight;

        public static string GetName(InventoryItemKind kind) => kind switch
        {
            InventoryItemKind.Food => "FOOD",
            InventoryItemKind.Water => "WATER",
            InventoryItemKind.Supplies => "SUPPLIES",
            InventoryItemKind.SpareCannon => "SPARE CANNONS",
            InventoryItemKind.Timber => "TIMBER",
            InventoryItemKind.Canvas => "CANVAS",
            InventoryItemKind.Iron => "IRON",
            InventoryItemKind.Gear => "GEAR",
            InventoryItemKind.Chart => "CHART",
            InventoryItemKind.Relic => "RELIC",
            _ => kind.ToString().ToUpperInvariant()
        };

        public static string GetDescription(InventoryItemKind kind) => kind switch
        {
            InventoryItemKind.Food => "Keeps the crew fed at sea.",
            InventoryItemKind.Water => "Keeps the crew hydrated.",
            InventoryItemKind.Supplies => "General stores for the voyage.",
            InventoryItemKind.SpareCannon => "Unmounted cannon ready for a hardpoint.",
            InventoryItemKind.Timber => "Salt-worn planks for hull repairs.",
            InventoryItemKind.Canvas => "Aged canvas for sails and rigging.",
            InventoryItemKind.Iron => "Fittings for reinforced ship parts.",
            InventoryItemKind.Gear => "Mechanisms for engines and workshops.",
            InventoryItemKind.Chart => "Fragments that reveal nearby waters.",
            InventoryItemKind.Relic => "A rare keepsake from a lost voyage.",
            _ => string.Empty
        };

        public static string GetLocalizedName(InventoryItemKind kind)
        {
            if (!GameLocalization.IsJapanese) return GetName(kind);
            return kind switch
            {
                InventoryItemKind.Food => "食料",
                InventoryItemKind.Water => "飲料水",
                InventoryItemKind.Supplies => "航海物資",
                InventoryItemKind.SpareCannon => "予備砲台",
                InventoryItemKind.Timber => "木材",
                InventoryItemKind.Canvas => "帆布",
                InventoryItemKind.Iron => "鉄材",
                InventoryItemKind.Gear => "歯車",
                InventoryItemKind.Chart => "海図",
                InventoryItemKind.Relic => "遺物",
                _ => GetName(kind)
            };
        }

        public static string GetLocalizedDescription(InventoryItemKind kind)
        {
            if (!GameLocalization.IsJapanese) return GetDescription(kind);
            return kind switch
            {
                InventoryItemKind.Food => "航海中に船員が消費する食糧。",
                InventoryItemKind.Water => "船員のための新鮮な飲料水。",
                InventoryItemKind.Supplies => "長い航海を支える船用品。",
                InventoryItemKind.SpareCannon => "砲座へ搭載できる予備の艦砲。",
                InventoryItemKind.Timber => "船体修理に使う潮焼けした木材。",
                InventoryItemKind.Canvas => "帆や索具に使える丈夫な帆布。",
                InventoryItemKind.Iron => "船体設備を補強する鉄材。",
                InventoryItemKind.Gear => "推進装置や工房で使う機構部品。",
                InventoryItemKind.Chart => "周辺海域を記す海図の断片。",
                InventoryItemKind.Relic => "失われた航海から残った希少な遺物。",
                _ => string.Empty
            };
        }

        public static InventoryRarity GetRarity(InventoryItemKind kind) => kind switch
        {
            InventoryItemKind.Food or InventoryItemKind.Water => InventoryRarity.Common,
            InventoryItemKind.Supplies or InventoryItemKind.SpareCannon => InventoryRarity.Uncommon,
            InventoryItemKind.Timber or InventoryItemKind.Canvas => InventoryRarity.Rare,
            InventoryItemKind.Iron or InventoryItemKind.Gear => InventoryRarity.Epic,
            _ => InventoryRarity.Legendary
        };

        public static string GetRarityName(InventoryRarity rarity) => rarity.ToString().ToUpperInvariant();

        public static string GetLocalizedRarityName(InventoryRarity rarity)
            => !GameLocalization.IsJapanese ? GetRarityName(rarity) : rarity switch
            {
                InventoryRarity.Common => "一般",
                InventoryRarity.Uncommon => "上質",
                InventoryRarity.Rare => "希少",
                InventoryRarity.Epic => "秘宝",
                InventoryRarity.Legendary => "伝説",
                _ => GetRarityName(rarity)
            };

        public static Color GetRarityColor(InventoryRarity rarity) => rarity switch
        {
            InventoryRarity.Common => new Color(0.68f, 0.72f, 0.73f, 1f),
            InventoryRarity.Uncommon => new Color(0.43f, 0.82f, 0.20f, 1f),
            InventoryRarity.Rare => new Color(0.08f, 0.78f, 0.94f, 1f),
            InventoryRarity.Epic => new Color(0.87f, 0.25f, 0.88f, 1f),
            InventoryRarity.Legendary => new Color(1f, 0.57f, 0.06f, 1f),
            _ => Color.white
        };

        public static int GetCount(GameState state, InventoryItemKind kind)
        {
            if (state == null) return 0;
            return kind switch
            {
                InventoryItemKind.Food => state.Food,
                InventoryItemKind.Water => state.Water,
                InventoryItemKind.Supplies => state.Supplies,
                InventoryItemKind.SpareCannon => state.SpareCannons,
                InventoryItemKind.Timber => state.GetPartCount(SalvagePartKind.Timber),
                InventoryItemKind.Canvas => state.GetPartCount(SalvagePartKind.Canvas),
                InventoryItemKind.Iron => state.GetPartCount(SalvagePartKind.Iron),
                InventoryItemKind.Gear => state.GetPartCount(SalvagePartKind.Gear),
                InventoryItemKind.Chart => state.GetPartCount(SalvagePartKind.Chart),
                InventoryItemKind.Relic => state.GetPartCount(SalvagePartKind.Relic),
                _ => 0
            };
        }

        public static int GetTotalCount(GameState state)
        {
            int total = 0;
            for (int index = 0; index < EntryCount; index++) total += GetCount(state, (InventoryItemKind)index);
            return total;
        }

        public static string GetTextureResource(InventoryItemKind kind)
            => $"Textures/UI/ConceptV04/Inventory/inventory_{GetTextureSlug(kind)}_v04";

        private static string GetTextureSlug(InventoryItemKind kind)
            => kind == InventoryItemKind.SpareCannon ? "spare_cannon" : kind.ToString().ToLowerInvariant();
    }
}
