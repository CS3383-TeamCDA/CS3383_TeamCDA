namespace EscapeThe90s.WorldGen
{
    /// <summary>Compact classification of a world-coordinate chunk.</summary>
    public enum WorldChunkKind
    {
        Sky = 0,
        Solid = 1,
        Surface = 2,
        Void = 3,
        Air = Sky,
        Ground = Solid,
        Mixed = Surface
    }
}
