using EscapeThe90s.Contracts;

namespace EscapeThe90s.WorldGen
{
    public sealed class GroundChunk : Chunk
    {
        public override ChunkKind Kind { get { return ChunkKind.Ground; } }
    }
}
