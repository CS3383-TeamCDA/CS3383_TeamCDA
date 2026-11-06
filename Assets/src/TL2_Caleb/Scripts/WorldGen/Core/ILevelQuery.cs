namespace EscapeThe90s.WorldGen
{
    /// <summary>Queries generated layouts, biome rules, and distance-based difficulty for one seeded world.</summary>
    public interface ILevelQuery
    {
        StreetLayout GetStreet(StreetRequest request);
        BiomeRules GetBiomeRules(int biome);
        int GetDifficultyTier(float worldDistanceFromOrigin);
    }
}
