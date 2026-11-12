namespace EscapeThe90s.WorldGen
{
    /// <summary>Continuous, forward-only progress measured in generated terrain cells.</summary>
    public readonly struct RunProgressPosition
    {
        public readonly long GlobalChunkIndex;
        public readonly float CellsIntoChunk;
        public readonly double DistanceInCells;

        internal RunProgressPosition(double distanceInCells)
        {
            DistanceInCells = distanceInCells;
            double width = WorldConstants.ChunkWidthCells;
            double chunk = System.Math.Floor(distanceInCells / width);
            GlobalChunkIndex = chunk >= long.MaxValue ? long.MaxValue : (long)chunk;
            CellsIntoChunk = (float)(distanceInCells - chunk * width);
        }
    }
}
