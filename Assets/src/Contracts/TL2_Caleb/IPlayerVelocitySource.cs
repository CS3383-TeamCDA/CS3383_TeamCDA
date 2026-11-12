namespace EscapeThe90s.Contracts
{
    /// <summary>Boundary through which stream policies read player velocity.</summary>
    public interface IPlayerVelocitySource
    {
        WorldPosition2D GetVelocity();
    }
}
