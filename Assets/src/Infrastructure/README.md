# Shared Infrastructure

Infrastructure is shared technical support, independent of feature-team ownership.

- `EscapeThe90s.Infrastructure` (`Messaging`) implements the engine-neutral `IEventBus` contract. It supports typed publish/subscribe and optional replay of the latest event of each type.
- `EscapeThe90s.Infrastructure.Unity` contains `DependencyBehaviour`, `DependencyScope`, and Unity-only messages such as `UnityPlayerSpawnedEvent`.
- `EscapeThe90s.Contracts` defines consumer-facing interfaces and engine-neutral lifecycle messages. It has no Unity dependency.
- Deprecated `IGameStateEventPublisher` and `IUnityGameStateEventPublisher` remain as compatibility interfaces. Composition adapts them through `GameStateEventBridge`; new consumers should use `IEventBus`.
- The former `EscapeThe90s.Dependencies.Unity` assembly and namespace remain as obsolete aliases in `Infrastructure/Compatibility`, so existing team assemblies can keep compiling while they migrate on their schedule.

## Active use cases

- `ServiceContainer` in `Assets/src/Composition` constructs the bus and registers it under `IEventBus`.
- `GameStateEventBridge` adapts TL1's state machine to `GameStartedEvent`, `MenuEnteredEvent`, `GamePausedEvent`, and `GameOverEvent`.
- `PlayerSpawnNotifier` publishes `UnityPlayerSpawnedEvent`; `CameraTargetBinder` subscribes with replay so a late camera still finds the current player.
- `DependencyScope` injects registered services into scene and runtime-created objects before enabling dependency-gated components.

Features depend on Contracts. Composition connects concrete systems, while Infrastructure provides reusable mechanisms and does not reference feature assemblies.
