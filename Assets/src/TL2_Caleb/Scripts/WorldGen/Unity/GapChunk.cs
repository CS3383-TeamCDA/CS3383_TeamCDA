using EscapeThe90s.Contracts;

namespace EscapeThe90s.WorldGen
{
    public sealed class GapChunk : Chunk
    {
        public override ChunkKind Kind { get { return ChunkKind.Gap; } }
        protected override bool CanRenderColumn(StreetTerrain terrain, int worldX) { return !terrain.IsGap(worldX); }
    }
}
