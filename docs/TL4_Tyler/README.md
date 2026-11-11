# TL4 enemies and combat

## Shared integration

The public enemy API lives in
[Contracts/TL4_Tyler](../../Assets/src/Contracts/TL4_Tyler/README.md).
`EnemySpawner` implements `IEnemySpawner`; Composition registers the scene's
spawner and adds `EnemyGameStateBinding` before dependency injection/activation.
No final scene or prefab wiring is changed by this migration.

Other teams should receive `IEnemySpawner` through `IDependent<IEnemySpawner>`
rather than referencing TL4Assembly. Subscribe to its defeat event to receive
`EnemyDefeatedInfo` with type, level, score, coin reward, and engine-independent
position. EnemyType is now in EscapeThe90s.Contracts; serialized enum values
remain Ground=0, Flying=1, Turret=2, Aim=3.

Score defaults to zero and is configured independently on the enemy prefab;
existing level-scaled coin values are unchanged. TL1/TL5 should agree score values
and migrate their consumers. Legacy callbacks remain for existing consumers;
never award from both the legacy and contract events.

Example consumer (assembly references Contracts and Infrastructure.Unity):

```csharp
using EscapeThe90s.Contracts;
using EscapeThe90s.Infrastructure.Unity;

public sealed class EnemyScoreListener : DependencyBehaviour, IDependent<IEnemySpawner>
{
    private IEnemySpawner enemies;
    public int Score { get; private set; }
    public void Inject(IEnemySpawner dependency) { enemies = dependency; }
    protected override void OnEnabledAfterInitialization()
    {
        enemies.OnEnemyDefeated += HandleDefeat;
    }
    protected override void OnDisabledAfterInitialization()
    {
        enemies.OnEnemyDefeated -= HandleDefeat;
    }
    private void HandleDefeat(EnemyDefeatedInfo info) { Score += info.ScoreValue; }
}
```

The state binding permits automatic spawning only during Playing. It leaves the
spawn rate intact on pause/resume and clears accumulated spawn time when stopped.
Manual factory calls remain available for demos/tests. TL1 freezes existing
movement/projectiles using scaled Unity time; restart recreates scene objects.
Standalone TL4 scenes do not need game-state services unless the binding is added.

## Remaining dependencies

TL6's shared player-health contract and TL3's shared power-up contract are not yet
present. Damage and drop implementations retain their existing working concrete
calls and assembly references pending those interfaces. This is not yet a complete
removal of TL4's cross-team dependencies.

## Verification

- Existing TL4 EditMode tests remain, with EnemyType imports updated.
- TL4ContractTests covers one defeat notification, independent score/coin values,
  position and level payloads, and no notification for living despawns.
- TL4.Contracts.PlayMode covers real DependencyScope injection, late subscription,
  pause/resume without spawn debt, unsubscribe on disable, menu and game over.
- Compiler verification using Unity's cached response files passes for Contracts,
  Chris.Game, TL4Assembly, Composition, TL4Tyler_Tests, and TL4.Contracts.PlayMode.
  This is a compile check, not a fresh Unity build or test execution.
- Batch tests could not execute: the working project is open in another Unity
  instance; an isolated copy was blocked by sandbox socket permissions in Unity
  Package Manager. Run EditMode and PlayMode in Unity Test Runner before declaring
  the suite green and moving to final-scene assembly.
