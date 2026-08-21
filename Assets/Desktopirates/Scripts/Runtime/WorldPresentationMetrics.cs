namespace Desktopirates
{
    /// <summary>Authored world scale shared by model loading, camera framing and tests.</summary>
    public static class WorldPresentationMetrics
    {
        // Return the sea to its earlier, calmer desktop footprint. Model scales compensate for
        // the wider camera so ships and landmarks remain readable while rim badges gain air.
        public const float CameraOrthographicSize = 8.25f;
        public const float PlayerModelScale = 1.67f;
        public const float EnemyModelScale = 1.51f;
        public const float HarborModelScale = 1.14f;
        public const float WreckModelScale = 1.65f;
        public const float TreasureModelScale = 1.71f;
        public const float GangAdmiralModelScale = 1.46f;
        public const float GhostShipModelScale = 1.51f;
        public const float KrakenModelScale = 1.63f;
        public const float PoseidonModelScale = 1.53f;
        public const float MinimumOceanVerticalCoverage = 0.75f;
    }
}
