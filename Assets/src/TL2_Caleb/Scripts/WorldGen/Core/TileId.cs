namespace EscapeThe90s.WorldGen
{
    /// <summary>Engine-independent tile meaning emitted by a world profile.</summary>
    public enum TileId : byte
    {
        Sky = 0,
        Ground = 1,
        OneWayPlatform = 2,
        Hazard = 3,
        Void = 4
    }
}
