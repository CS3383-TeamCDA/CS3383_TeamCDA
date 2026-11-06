using EscapeThe90s.Contracts;

namespace EscapeThe90s.WorldGen
{
    public sealed class RampChunk : Chunk
    {
        public override ChunkKind Kind { get { return ChunkKind.Ramp; } }
    }
}
