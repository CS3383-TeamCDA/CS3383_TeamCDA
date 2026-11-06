namespace EscapeThe90s.Contracts
{
    /// <summary>Boundary through which world generation reads player position.</summary>
    public interface IPlayerPositionSource
    {
        WorldPosition2D GetPosition();
    }
}
