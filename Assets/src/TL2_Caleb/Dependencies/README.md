# TL2 Dependency Module

`EscapeThe90s.Dependencies.Unity` contains Unity lifecycle and injection helpers. Shared consumer contracts live in `Assets/src/Contracts` and do not reference Unity.

## Current game use cases

- **Camera target binding:** `CameraTargetBinder` declares `IDependent<TL2_Dependencies.IGameStateEventPublisher>`. The scope injects the publisher, then enables the binder; it subscribes to player-spawn events and follows the current player.
- **Player registration:** `PlayerSpawnNotifier` requests the same publisher. It reports its player transform from `OnSetupComplete`, after all scene dependencies have been injected and the publisher has been activated.
- **Runtime-created player:** `GeneratedWorldComposition` calls the existing game composition entry point. `ServiceContainer` delegates to `DependencyScope.Inject(GameObject)`, which injects and activates dependency-aware components beneath that object.

These demonstrate event subscription, a notification side effect that must happen after injection, and late injection for dynamically created objects.

## Using the scope

Composition owns concrete service creation and registration. A typical bootstrap registers the provider under its consumer-facing contract, then injects the scene:

```csharp
scope.Register<IGameStateEventPublisher>(publisher);
scope.Register<IPlayerPositionSource>(playerPositionSource);
scope.InjectScene();
```

Consumers implement `EscapeThe90s.Contracts.IDependent<T>` for each required service. A component that needs the activation gate derives from `DependencyBehaviour`; a plain class or component can use the contract without inheriting it. For runtime objects, call `scope.Inject(instance)` after construction and before relying on its injected components.

`DependencyScope` injects all registered dependencies before activating gated components. Missing dependencies keep a `DependencyBehaviour` disabled and are logged. Provider lifetime, construction, scene selection, and gameplay policy remain the responsibility of Composition.

## Contracts and Unity adapter boundary

`EscapeThe90s.Contracts.IGameStateEventPublisher` exposes engine-independent game lifecycle events. The old `TL2_Dependencies.IGameStateEventPublisher` extends it with the Unity `Transform` player-spawn event needed by Cinemachine. That extension is transitional: keep Unity objects out of Contracts, and replace the legacy interface with a current-game Unity adapter before removing TL6's archived assembly reference.
