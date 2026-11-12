using NUnit.Framework;
using EscapeThe90s.Contracts;
using EscapeThe90s.GameStates;

/// <summary>
/// Each state must report the right GameStateType, since that is all other features can see through IGameState.
/// </summary>
public class GameStateTypeTests
{
    // Test-only state that does not override Type, so the base GameState value is used.
    private class NoOverrideState : GameState { }

    [TestCase(typeof(MenuState), GameStateType.Menu)]
    [TestCase(typeof(PlayingState), GameStateType.Playing)]
    [TestCase(typeof(PauseState), GameStateType.Paused)]
    [TestCase(typeof(GameOverState), GameStateType.GameOver)]
    [TestCase(typeof(NoOverrideState), GameStateType.None)]
    public void State_ReportsMatchingType(System.Type stateClass, GameStateType expected)
    {
        GameState state = (GameState)System.Activator.CreateInstance(stateClass);

        Assert.AreEqual(expected, state.Type);
    }
}
