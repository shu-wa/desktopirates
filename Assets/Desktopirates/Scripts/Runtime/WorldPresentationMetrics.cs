namespace Desktopirates
{
    /// <summary>Authored world scale shared by model loading, camera framing and tests.</summary>
    public static class WorldPresentationMetrics
    {
        // Preserve the original desktop footprint of the circular sea. Readability comes
        // from authored object scale, never by enlarging the ocean over the desktop.
        public const float CameraOrthographicSize = 7.20f;
        public const float PlayerModelScale = 1.46f;
        public const float EnemyModelScale = 1.32f;
        public const float HarborModelScale = 1.00f;
        public const float WreckModelScale = 1.44f;
        public const float TreasureModelScale = 1.50f;
        public const float GangAdmiralModelScale = 1.27f;
        public const float GhostShipModelScale = 1.32f;
        public const float KrakenModelScale = 1.42f;
        public const float PoseidonModelScale = 1.34f;
        public const float MinimumOceanVerticalCoverage = 0.86f;
    }
}
