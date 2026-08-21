namespace Desktopirates
{
    /// <summary>Authored world scale shared by model loading, camera framing and tests.</summary>
    public static class WorldPresentationMetrics
    {
        public const float CameraOrthographicSize = 6.00f;
        public const float PlayerModelScale = 1.22f;
        public const float EnemyModelScale = 1.10f;
        // The authored harbor has a much larger native footprint than the ship FBXs.
        // This multiplier keeps it dominant without covering the whole navigable disc.
        public const float HarborModelScale = 0.80f;
        public const float WreckModelScale = 1.20f;
        public const float TreasureModelScale = 1.25f;
        public const float GangAdmiralModelScale = 1.06f;
        public const float GhostShipModelScale = 1.10f;
        public const float KrakenModelScale = 1.18f;
        public const float PoseidonModelScale = 1.12f;
        public const float MinimumOceanVerticalCoverage = 0.96f;
    }
}
