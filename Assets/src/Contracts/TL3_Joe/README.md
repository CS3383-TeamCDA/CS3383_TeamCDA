# TL3 power-up contracts

Request `IPowerUpSpawner` using `IDependent<IPowerUpSpawner>`. Reference
`EscapeThe90s.Contracts` and `EscapeThe90s.Infrastructure.Unity` in your
assembly; a consumer does not need `Joe.Game`.

```csharp
public interface IPowerUpSpawner
{
    void SpawnPowerUp(WorldPosition2D position, PowerUpType type);
    event Action<PowerUpCollectedInfo> OnPowerUpCollected;
}
```

- `SpawnPowerUp` places a pickup at `(X, Y, 0)`. The effect is applied by TL3
  when the player touches it; callers never apply effects themselves.
- `PowerUpType` is `SpeedBoost`, `Shield`, or `Magnet`.
- A non-finite position or an undefined type throws. Spawns past 200 active
  pickups, or for a type with no `PowerUpDefinition` assigned, are logged and skipped.
- `OnPowerUpCollected(PowerUpCollectedInfo)` fires once per pickup.
  Payload: `Type`, `Position`, `StackCount`, `RemainingSeconds` (0 when permanent),
  `IsPermanent`. A handler that throws is logged and does not stop the others.

Subscribe in `OnEnabledAfterInitialization`; unsubscribe in
`OnDisabledAfterInitialization`.

## Setup

Composition registers the one scene-owned `PowerUpManager` (from the
`PowerUpSystem` prefab) as `IPowerUpSpawner`. The player needs the `Player` tag
and an `IPowerUpTarget` component for pickups to apply.

Objects created after scene load, such as spawned enemies, are not injected
automatically. Pass each one to `DependencyScope.Inject(instance)` right after
`Instantiate`. A plain `MonoBehaviour` implementing `IDependent<T>` is injected
too, but it is not gated, so null-check the service before using it.

## Example: dropping a power-up

```csharp
using EscapeThe90s.Contracts;
using UnityEngine;

public class EnemyDrop : MonoBehaviour, IDependent<IPowerUpSpawner>
{
    private IPowerUpSpawner powerUps;

    public void Inject(IPowerUpSpawner dependency)
    {
        powerUps = dependency;
    }

    public void Drop(PowerUpType type)
    {
        if (powerUps == null) return;   // not injected, e.g. a standalone demo scene
        Vector3 p = transform.position;
        powerUps.SpawnPowerUp(new WorldPosition2D(p.x, p.y), type);
    }
}
```
