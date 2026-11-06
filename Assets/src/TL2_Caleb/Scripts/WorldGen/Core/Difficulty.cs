namespace EscapeThe90s.WorldGen
{
    public static class Difficulty
    {
        /// <summary>Integer difficulty tier from world-space distance to the origin (not graph distance, so subways can't shortcut it).</summary>
        public static int TierAt(float worldDistanceFromOrigin)
        {
            if (worldDistanceFromOrigin <= 0f) return 0;
            return (int)(worldDistanceFromOrigin / WorldConstants.DistancePerTier);
        }
    }
}
