namespace EscapeThe90s.Contracts
{
    /// <summary>
    /// Identifies a TL1 game state for features that cannot reference TL1's state classes.
    /// </summary>
    public enum GameStateType
    {
        None,
        Menu,
        Playing,
        Paused,
        GameOver
    }
}
