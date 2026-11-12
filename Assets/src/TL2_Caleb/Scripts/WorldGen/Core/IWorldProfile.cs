namespace EscapeThe90s.WorldGen
{
    /// <summary>Creates deterministic world chunks and exposes ground-tile occupancy through ICellSource.</summary>
    public interface IWorldProfile : ICellSource
    {
        WorldChunkData GenerateChunk(WorldChunkCoord coordinate);
        WorldChunkKind GetChunkKind(WorldChunkCoord coordinate);
        TileId GetTileAtCell(int worldCellX, int worldCellY);
        int GetSurfaceHeight(int worldCellX);
    }
}
