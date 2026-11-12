namespace EscapeThe90s.Contracts
{
    /// <summary>Player movement limits needed by terrain-generation playability rules.</summary>
    public interface IPlayerCapabilities
    {
        float MaxJumpHeight { get; }
        float MaxJumpDistance { get; }
        float BaselineSpeed { get; }
    }
}
