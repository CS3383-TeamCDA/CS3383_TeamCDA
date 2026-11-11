# TL4 enemy contracts

Request `IEnemySpawner` using `DependencyBehaviour, IDependent<IEnemySpawner>`.
Reference `EscapeThe90s.Contracts` and `EscapeThe90s.Infrastructure.Unity` in your
assembly; a consumer does not need `TL4Assembly`.

- `SetSpawnRate(float)` uses enemies per second; zero stops automatic spawning.
- `SetSpawnLevel(int)` sets the level of future enemies (minimum 1).
- `AliveCount` counts tracked enemies not defeated or destroyed.
- `OnEnemyDefeated(EnemyDefeatedInfo)` reports one defeat, never an ordinary despawn.
  Payload: `Type`, `Level`, `ScoreValue`, `CoinReward`, `Position` (`WorldPosition2D`).
- Score is independent of currency. Configure `scoreValue` on enemy prefabs;
  default is zero until the team agrees scoring values. Coins retain level scaling.

Subscribe in `OnEnabledAfterInitialization`; unsubscribe in
`OnDisabledAfterInitialization`. TL1 can use ScoreValue; TL5 can use CoinReward.
Do not subscribe to both legacy reward events and this event or rewards will double.

Composition registers one scene-owned EnemySpawner and adds EnemyGameStateBinding.
That adapter receives IGameState and permits automatic spawning only in Playing.
It reads the current state on enable, including late injection, and preserves the
configured spawn rate across pause/resume. Existing enemies and projectiles use
scaled Unity time, frozen by TL1's states. Restart reloads scene-owned objects.
Manual CreateEnemy remains a TL4 factory API and is not gated by state.

Standalone TL4 demos remain usable without a ServiceContainer. For a custom scope,
register `IEnemySpawner`, add EnemyGameStateBinding to the spawner, register
`IGameState`, and inject/activate through DependencyScope. Inject any newly spawned
consumer with `scope.Inject(instance)` before it uses services. A spawner created
after ServiceContainer.Awake must also be registered explicitly on the scope.

The concrete EnemySpawner retains its old two-argument OnEnemyDefeated and
OnCoinsAwarded events for existing consumers. New consumers use the interface event.
EnemyType now lives in EscapeThe90s.Contracts; import that namespace.

EnemyBase consumes TL3 IPowerUpSpawner through IDependent injection. EnemySpawner
injects each runtime-created enemy through the scene DependencyScope before
publishing OnEnemySpawned. Standalone scenes need a scope/service to enable drops.
TL4 no longer references Joe.Game. EnemyProjectile/EnemyAttackAnimations still use
TL6 PlayerHealth for collider-based damage pending TL6's health contract.
