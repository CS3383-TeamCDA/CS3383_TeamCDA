namespace EscapeThe90s.Contracts
{
    /// <summary>Engine-independent two-dimensional world position.</summary>
    public readonly struct WorldPosition2D
    {
        public readonly float X, Y;
        public WorldPosition2D(float x, float y) { X = x; Y = y; }
    }
}
