namespace EscapeThe90s.WorldGen
{
    /// <summary>Shared continuous ramp boundary, in world units. Cell centers retain their tile occupancy.</summary>
    public static class CoordinateTerrainGeometry
    {
        public static float SurfaceTopAtEdge(IWorldProfile profile, int rightColumn)
        { return (profile.GetSurfaceHeight(rightColumn - 1) + profile.GetSurfaceHeight(rightColumn)) * 0.5f + 0.5f; }

        public static float SurfaceTopAtCell(IWorldProfile profile, int column)
        { return profile.GetSurfaceHeight(column) + 0.5f; }
    }
}
