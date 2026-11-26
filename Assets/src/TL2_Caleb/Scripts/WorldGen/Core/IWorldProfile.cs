namespace EscapeThe90s.WorldGen
{
    /// <summary>Creates deterministic world chunks and exposes ground-tile occupancy through ICellSource.</summary>
    public interface IWorldProfile : ICellSource
    {
        WorldChunkData GenerateChunk(WorldChunkCoord coordinate);
        WorldChunkKind GetChunkKind(WorldChunkCoord coordinate);
        TileId GetTileAtCell(int worldCellX, int worldCellY);
        /// <summary>Nominal road elevation, including at gaps. Use tile occupancy to determine whether ground exists.</summary>
        int GetSurfaceHeight(int worldCellX);
    }
}
