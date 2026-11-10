# Contracts Guide

How to share your feature with other teams, and how to use what other teams
share, without referencing each other's assemblies.

## How it fits together

| Piece | Where | What it does |
| ----- | ----- | ------------ |
| Contract | `Assets/src/Contracts` | An interface or event struct everyone can see |
| Provider | Your team folder | The class that implements the contract |
| `ServiceContainer` | `Assets/src/Composition` | Registers providers under their contract |
| `DependencyScope` | `Assets/src/Infrastructure/Unity` | Hands registered services to scene objects |
| Consumer | Any team folder | Implements `IDependent<T>` to receive a service |

At scene start, `ServiceContainer.Awake` registers every provider, injects them
into every component in the scene that implements `IDependent<T>`, and then
enables those components. Only objects in the same scene as `ServiceContainer`
are injected; objects created later need the step in Part 1, section 4.

## Available contracts

| Contract | Provided by | Use it to |
| -------- | ----------- | --------- |
| `IEventBus` | Infrastructure (`EventBus`) | Publish and subscribe to events |
| `IGameState` ([details](../../Assets/src/Contracts/TL1_Chris/README.md)) | TL1 (`GameStateManager`) | Read `CurrentStateType`, subscribe to `OnStateTypeChanged`, and request transitions |
| `IGameplaySessionState` | Composition (`GameStateEventBridge`) | Check `IsGameplayStarted` |
| `ILevelProgress` | TL2 (`LevelProgress`) | Read distance and difficulty, chunk spawn/recycle |
| `IPlayerPositionSource` | TL2 (`TransformPlayerPositionSource`) | Read the player position |
| `IPlayerCapabilities` | Not implemented yet | Read jump and speed limits |

Game-state event on `IGameState` (raised by TL1):

| Event | When |
| ----- | ---- |
| `OnStateTypeChanged(GameStateChange)` | After every transition. Carries `Previous`, `Current`, and `IsNewRun` (true on start and restart, false on resume) |

Events published on `IEventBus` (the game-state ones come from
`GameStateEventBridge`; prefer the `IGameState` events above for new code):

| Event | When |
| ----- | ---- |
| `GameStartedEvent` | Entering Playing from any state except Paused |
| `GameResumedEvent` | Entering Playing from Paused |
| `GamePausedEvent` | Entering Paused |
| `GameOverEvent` | Entering GameOver |
| `MenuEnteredEvent` | Entering Menu |
| `PlayerDiedEvent` | `NotifyPlayerDied()` was called |
| `UnityPlayerSpawnedEvent` | The player spawned (in `Infrastructure.Unity`; carries a `Transform`) |

## Part 1: Using a contract (consumer)

### 1. Reference the assemblies

In your team's `.asmdef`, add to `references`:

```json
"EscapeThe90s.Contracts",
"EscapeThe90s.Infrastructure.Unity"
```

`Infrastructure.Unity` is needed if you inherit `DependencyBehaviour` or inject
objects you spawn at runtime. Never reference `EscapeThe90s.Composition`:
Composition references the team assemblies, so that would create a cycle.

### 2. Implement `IDependent<T>`

Inherit from `DependencyBehaviour` and implement `IDependent<T>` once for each
service you need.

```csharp
using EscapeThe90s.Contracts;
using EscapeThe90s.Infrastructure.Unity;

namespace EscapeThe90s.Ui
{
    public class PauseButton : DependencyBehaviour, IDependent<IGameState>
    {
        private IGameState gameState;

        public void Inject(IGameState dependency)
        {
            gameState = dependency;
        }

        public void OnClick()
        {
            gameState.RequestPause();
        }
    }
}
```

The `Request*` methods ignore transitions that aren't valid from the current
state (for example, `RequestPause` does nothing unless the game is playing), so
you don't need to check the state first.

To react to state changes, subscribe to `IGameState.OnStateTypeChanged` when
the component is enabled and unsubscribe when it is disabled:

```csharp
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
```

Always unsubscribe. `GameStateManager` survives scene reloads, so a handler left
attached to a destroyed object will throw `MissingReferenceException`. Also,
calling a `Request*` method from inside the handler does nothing,
because the state machine ignores changes while it is already changing state.

`DependencyBehaviour` keeps the component disabled until every `IDependent<T>`
it declares has been injected, so you can use the service in `Start`,
`Update`, and event handlers without null checks. Don't use it in `Awake`,
which runs before injection. If something is never provided, the console logs
`... is missing <Type>`.

Lifecycle hooks:

| Override | Runs when |
| -------- | --------- |
| `OnSetupComplete()` | Once, after all dependencies arrive |
| `OnEnabledAfterInitialization()` | Each time the component is enabled after setup |
| `OnDisabledAfterInitialization()` | Each time it is disabled after setup |

If you override `Awake`, `OnEnable`, or `OnDisable`, call the `base` method
first. Skipping it breaks the activation gate.

### 3. Subscribe to event-bus events


Use `IEventBus` for events that aren't on a contract interface, such as
`UnityPlayerSpawnedEvent`. `Subscribe` returns an `IDisposable`; dispose it when
the component is disabled.

```csharp
using System;
using EscapeThe90s.Contracts;
using EscapeThe90s.Infrastructure.Unity;
using UnityEngine;

public class PlayerTracker : DependencyBehaviour, IDependent<IEventBus>
{
    private IEventBus events;
    private IDisposable subscription;
    private Transform player;

    public void Inject(IEventBus dependency)
    {
        events = dependency;
    }

    protected override void OnEnabledAfterInitialization()
    {
        subscription = events.Subscribe<UnityPlayerSpawnedEvent>(HandlePlayerSpawned, true);
    }

    protected override void OnDisabledAfterInitialization()
    {
        subscription?.Dispose();
        subscription = null;
    }

    private void HandlePlayerSpawned(UnityPlayerSpawnedEvent message)
    {
        player = message.Player;
    }
}
```

The `true` is `replayLatest`: you also receive the most recent event published
before you subscribed. Use it when your object might be created after the event
fired, like here, where the player may spawn first. `TL6_Matteo/CameraTargetBinder.cs`
is a working example.

### 4. Objects spawned at runtime

`DependencyScope` only injects objects that exist when the scene loads. For
anything you `Instantiate` later, pass it to the scene's `DependencyScope`
(`ServiceContainer` adds one to its own GameObject):

```csharp
using EscapeThe90s.Infrastructure.Unity;

GameObject enemy = Instantiate(enemyPrefab);
DependencyScope scope = FindAnyObjectByType<DependencyScope>();
scope.Inject(enemy);
```

Find the scope once (for example in `Start`) and keep it if you spawn often.
`ServiceContainer.InjectDynamicObject` does the same thing, but team code can't
call it because it lives in Composition.

## Part 2: Providing a contract (provider)

### 1. Add the interface to Contracts

Create a file in your team's folder under Contracts, for example
`Assets/src/Contracts/TLx_Name/IScoreSource.cs`:

```csharp
namespace EscapeThe90s.Contracts
{
    /// <summary>
    /// One sentence on what consumers get from this.
    /// </summary>
    public interface IScoreSource
    {
        int CurrentScore { get; }
    }
}
```

Rules for contract files:

- No `using UnityEngine`. The assembly has `noEngineReferences` turned on, so
  `Transform`, `Vector2`, and `GameObject` will not compile. Use plain C# types
  or shared structs such as `WorldPosition2D`.
- Do not use types from your team folder (such as TL1's `GameState` classes)
  in a contract. If consumers need an enum or data type, move it into
  Contracts, like TL1's `GameStateType`.
- For notifications, either add C# `event` members to your interface (like
  `IGameState.OnStateTypeChanged`) or add a `readonly struct` event (see
  `GameplayEvents.cs`) and publish it on `IEventBus`. Use `Action` or
  `Action<T>` where `T` is a Contracts or plain C# type. Prefer one event
  with a payload struct (like `GameStateChange`) over several small events:
  you can add fields to the struct later without breaking subscribers.
- Keep the interface small. Expose what consumers need, not your whole class.

### 2. Implement it in your feature

Your assembly must already reference `EscapeThe90s.Contracts`.

```csharp
using EscapeThe90s.Contracts;
using UnityEngine;

namespace EscapeThe90s.Score
{
    public class ScoreManager : MonoBehaviour, IScoreSource
    {
        public int CurrentScore { get; private set; }
    }
}
```

### 3. Register it in `ServiceContainer`

In `Assets/src/Composition/ServiceContainer.cs`, find your object and register
it under the contract in `Awake`, before the `_scope.Register` loop:

```csharp
ScoreManager scoreManager = FindAnyObjectByType<ScoreManager>(FindObjectsInactive.Include);
if (scoreManager != null)
{
    Register<IScoreSource>(scoreManager);
}
```

Then add your assembly to the `references` of
`EscapeThe90s.Composition.asmdef`. Composition is the only assembly allowed to
know about concrete team classes.

### 4. Publish events (optional)

Define a `readonly struct` for the event in Contracts (for example
`ScoreChangedEvent`), have your provider implement `IDependent<IEventBus>`, and
call `events.Publish(new ScoreChangedEvent(newScore))`. Consumers then subscribe
as shown in Part 1, section 3. Usually an event on your contract interface is
simpler; use the bus when the publisher and subscribers shouldn't share an
interface.

### 5. Tell the team

Contracts and Composition are shared folders. Post in the team channel when you
add a contract. Never rename or remove an existing member without checking with
everyone who uses it; add a new member or interface instead.

## Testing

Components that inherit `DependencyBehaviour` only subscribe and activate when
Unity runs `Awake` and `OnEnable`, so test them in a **PlayMode** test
(`[UnityTest]`). Calling `Inject` yourself is not enough: it never opens the
activation gate, so `OnSetupComplete` and `OnEnabledAfterInitialization` don't
run. Register a fake on a `DependencyScope` instead and let the scope inject it:

```csharp
using System;
using System.Collections;
using EscapeThe90s.Contracts;
using EscapeThe90s.Infrastructure.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class WaveSpawnerTests
{
    [UnityTest]
    public IEnumerator NewRun_StartsSpawning()
    {
        DependencyScope scope = new GameObject("Scope").AddComponent<DependencyScope>();
        FakeGameState fakeGameState = new FakeGameState();
        scope.Register<IGameState>(fakeGameState);

        GameObject spawnerObject = new GameObject("Spawner");
        WaveSpawner spawner = spawnerObject.AddComponent<WaveSpawner>();
        scope.Inject(spawnerObject);
        yield return null;

        fakeGameState.RaiseStateChanged(GameStateType.Playing, true);

        Assert.IsTrue(spawner.IsSpawning);
    }
}
```

The fake, nested inside the test class:

```csharp
private class FakeGameState : IGameState
{
    public bool PauseRequested { get; private set; }

    public GameStateType CurrentStateType { get; private set; } = GameStateType.Menu;

    public event Action<GameStateChange> OnStateTypeChanged;

    public void RaiseStateChanged(GameStateType next, bool isNewRun)
    {
        GameStateChange change = new GameStateChange(CurrentStateType, next, isNewRun);
        CurrentStateType = next;
        OnStateTypeChanged?.Invoke(change);
    }

    public void RequestStart()
    {
    }

    public void RequestPause()
    {
        PauseRequested = true;
    }

    public void RequestResume()
    {
    }

    public void RequestGameOver()
    {
    }

    public void RequestRestart()
    {
    }

    public void RequestReturnToMenu()
    {
    }
}
```

Your test assembly needs references to `EscapeThe90s.Contracts` and
`EscapeThe90s.Infrastructure.Unity`. None of the team test asmdefs reference
`EscapeThe90s.Infrastructure.Unity` yet, so add it.

Plain classes and methods that don't depend on the activation gate can still be
tested in EditMode by calling them directly.

## Troubleshooting

| Symptom | Cause |
| ------- | ----- |
| Component stays disabled, console says `missing <Type>` | Nobody registered that contract in `ServiceContainer`, or the scene has no `ServiceContainer` |
| Spawned prefab never gets its service | It was created after scene load. Pass it to `DependencyScope.Inject` |
| Test component never reacts to events | Called `Inject` directly, or used an EditMode test. Use a PlayMode test with `DependencyScope.Inject` |
| `The type or namespace 'UnityEngine' could not be found` in Contracts | Contracts cannot use Unity types |
| `The type or namespace 'EscapeThe90s.Contracts' could not be found` | Add `EscapeThe90s.Contracts` to your `.asmdef` references |
| Events stop after disable/enable | Overrode `OnEnable`/`OnDisable` without calling `base` |
