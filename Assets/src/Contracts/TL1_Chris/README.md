# IGameState (TL1)

`IGameState` is how other teammates track and control the game's state (menu,
playing, paused, game over) without referencing TL1's assembly. It is
implemented by `GameStateManager` in `Assets/src/TL1_Chris`.

For the general setup (asmdef references, `DependencyBehaviour`, registering
providers), see [docs/TL1_Chris/CONTRACTS_GUIDE.md](../../../../docs/TL1_Chris/CONTRACTS_GUIDE.md).

## The contract

```csharp
public interface IGameState
{
    GameStateType CurrentStateType { get; }
    event Action<GameStateChange> OnStateTypeChanged;

    void RequestStart();
    void RequestPause();
    void RequestResume();
    void RequestGameOver();
    void RequestRestart();
    void RequestReturnToMenu();
}
```

| Type | Values / members |
| ---- | ---------------- |
| `GameStateType` | `None` (before the first state), `Menu`, `Playing`, `Paused`, `GameOver` |
| `GameStateChange` | `Previous`, `Current`, `IsNewRun` |

## Reading the state

`CurrentStateType` is the state right now. Read it whenever you need to know
the state without waiting for a change, for example when an object spawns
mid-run or a panel is enabled late:

```csharp
protected override void OnSetupComplete()
{
    if (gameState.CurrentStateType != GameStateType.Playing)
    {
        Freeze();
    }
}
```

## Reacting to changes

`OnStateTypeChanged` is raised once after every transition. The
`GameStateChange` tells you where the game came from, where it is now, and
whether a new run just began.

| Transition | `Previous` → `Current` | `IsNewRun` |
| ---------- | ---------------------- | ---------- |
| Startup | `None` → `Menu` | false |
| Start from menu | `Menu` → `Playing` | **true** |
| Pause | `Playing` → `Paused` | false |
| Resume | `Paused` → `Playing` | false |
| Restart from pause | `Paused` → `Playing` | **true** |
| Restart from game over | `GameOver` → `Playing` | **true** |
| Game over | `Playing` → `GameOver` | false |
| Return to menu | `Playing`/`Paused`/`GameOver` → `Menu` | false |

Use `IsNewRun` to tell a new run apart from a resume. Both enter `Playing`,
and a restart from the pause menu even comes from `Paused`.

## Requests

Call these to change the state. A request that isn't valid from the current
state is ignored, so you don't need to check the state first.

| Method | Works from | Result |
| ------ | ---------- | ------ |
| `RequestStart()` | Menu | Playing (new run) |
| `RequestPause()` | Playing | Paused |
| `RequestResume()` | Paused | Playing |
| `RequestGameOver()` | Playing | GameOver |
| `RequestRestart()` | Paused, GameOver | Reloads the scene, then Playing (new run) |
| `RequestReturnToMenu()` | Playing, Paused, GameOver | Menu |

## Setup

Your team's `.asmdef` needs these references:

```json
"EscapeThe90s.Contracts",
"EscapeThe90s.Infrastructure.Unity"
```

`ServiceContainer` already registers `GameStateManager` as `IGameState`, so
you only need to ask for it.

## Example: reacting to state changes

```csharp
using EscapeThe90s.Contracts;
using EscapeThe90s.Infrastructure.Unity;

namespace EscapeThe90s.Waves
{
    public class WaveSpawner : DependencyBehaviour, IDependent<IGameState>
    {
        private IGameState gameState;

        public bool IsSpawning { get; private set; }

        public void Inject(IGameState dependency)
        {
            gameState = dependency;
        }

        protected override void OnEnabledAfterInitialization()
        {
            gameState.OnStateTypeChanged += HandleStateChanged;
        }

        protected override void OnDisabledAfterInitialization()
        {
            gameState.OnStateTypeChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(GameStateChange change)
        {
            if (change.IsNewRun)
            {
                BeginSpawning();
            }
            else if (change.Current == GameStateType.GameOver)
            {
                StopSpawning();
            }
        }

        private void BeginSpawning()
        {
            IsSpawning = true;
        }

        private void StopSpawning()
        {
            IsSpawning = false;
        }
    }
}
```

## Example: requesting a change

```csharp
public class LivesCounter : DependencyBehaviour, IDependent<IGameState>
{
    private IGameState gameState;
    private int lives = 3;

    public void Inject(IGameState dependency)
    {
        gameState = dependency;
    }

    public void LoseLife()
    {
        lives -= 1;
        if (lives <= 0)
        {
            gameState.RequestGameOver();
        }
    }
}
```

## Rules

1. **Always unsubscribe.** `GameStateManager` survives scene reloads. A handler
   left attached to an object that was destroyed by a reload will throw
   `MissingReferenceException` on the next state change. Subscribe in
   `OnEnabledAfterInitialization` and unsubscribe in
   `OnDisabledAfterInitialization`, as shown above.
2. **Don't call a request from inside the event handler.** The state machine
   ignores changes while it is already changing state, so the request does
   nothing.
3. **Events aren't replayed.** If your object subscribes after a change
   happened, it won't hear about it. Objects that exist when the scene loads
   are subscribed before the first state change. Objects spawned later in the
   run should read `CurrentStateType` when they start.
4. **Use the interface, not the singleton.** Ask for `IGameState` through
   `IDependent<IGameState>`. Don't use `GameStateManager.Instance`, which ties
   your code to TL1's assembly.
5. **Restart reloads the scene.** Your objects are destroyed and recreated, and
   the new ones receive a change with `IsNewRun` set. Don't keep anything on
   them that must survive a restart.

## Testing

Test components that use `IGameState` in a **PlayMode** test (`[UnityTest]`),
with a fake registered on a `DependencyScope`. Calling `Inject` yourself never
opens the `DependencyBehaviour` activation gate, so your subscription in
`OnEnabledAfterInitialization` would never run.

The full test example and the `FakeGameState` class are in
[CONTRACTS_GUIDE.md → Testing](../../../../docs/TL1_Chris/CONTRACTS_GUIDE.md#testing).
Use `fakeGameState.RaiseStateChanged(GameStateType.Playing, true)` to simulate a
new run, or check `fakeGameState.PauseRequested` after your code runs.
