# TL3 Power-Up & Ability System

Power-ups for Escape the 90s. Pickups are placed in the level, the player
collects them by touching them, and each type applies its own effect. Timed
effects restore the player's original stat when they run out.

The design is in [Champion_TL3_Joe.docx](Champion_TL3_Joe.docx) and
[ClassDiagram.pdf](ClassDiagram.pdf). This README describes the code as it is now.

## The power-ups

| Type | Effect | Duration | Repeat pickup while active |
| ---- | ------ | -------- | -------------------------- |
| Speed Boost | Speed x(1.0 + 0.5 per stack): 1.5, 2.0, 2.5 | 6 s | Adds a stack (max 3) and restarts the timer |
| Magnet | Pickup radius 1.5 to 6.0 units | 8 s | Restarts the timer only |
| Shield | Heals 25 | Instant, permanent | Heals another 25 |

All stacks of one type share a single timer. When it ends, the stat goes back
to the value it had before the first pickup. Speed Boost and Magnet run
independently of each other.

Durations, stack limits and amounts are set in each effect's constructor or
constants (for example `base(6f, false, 3)` in `SpeedBoostEffect`). The
`PowerUpDefinition` assets supply the prefab for each type; their duration and
stack fields are validated but not used yet.

## How it works

1. `PowerUpManager.SpawnPowerUp(position, type)` creates a `PowerUpItem` from
   that type's `PowerUpDefinition` prefab. At most 200 items can be active.
2. When a collider tagged `Player` enters the item's trigger, the item reports
   to `PowerUpManager.HandlePickup` and is destroyed.
3. The manager looks up the effect for that type and applies it to the player
   through `IPowerUpTarget`.
4. For a timed effect's first pickup, the manager first saves the stat it will
   change. Later pickups never overwrite that saved value.
5. Each frame, `Tick` counts timers down. An expired effect restores its saved
   value.
6. Every successful pickup raises `OnPowerUpCollected` once.

Every frame, the manager also pulls items within the player's pickup radius
toward the player, and removes items more than 20 units behind the player.

## Design patterns

**Strategy (choosing the effect).** `PowerUpEffect` is the abstract strategy
with virtual `Apply`, `Remove` and `CreateSnapshot`. `SpeedBoostEffect`,
`MagnetEffect` and `ShieldEffect` override them. `PowerUpManager` (the client)
keeps a `Dictionary<PowerUpType, PowerUpEffect>` and only ever calls through
the base type, so dynamic binding picks the right `Apply`. Adding a power-up
means one new subclass and one dictionary entry, with no change to the manager.

**Memento (undoing timed effects).** The effect is the originator:
`CreateSnapshot` saves the one stat it changes in a `StatSnapshot` (the
memento). `PowerUpManager` is the caretaker: it holds one snapshot per active
type, never reads inside it, and hands it back to `Remove` on expiry. There is
one snapshot per stat, so a Magnet picked up during a Speed Boost can't save
the boosted speed.

Both patterns are also explained in the header comments of `PowerUpEffect.cs`
and `StatSnapshot.cs`.

## Using it from another feature

Other features use the `IPowerUpSpawner` contract and don't reference
`Joe.Game`. The details and an example are in
[Contracts/TL3_Joe/README.md](../../Assets/src/Contracts/TL3_Joe/README.md).

```csharp
public interface IPowerUpSpawner
{
    void SpawnPowerUp(WorldPosition2D position, PowerUpType type);
    event Action<PowerUpCollectedInfo> OnPowerUpCollected;
}
```

`ServiceContainer` registers the scene's `PowerUpManager` as
`IPowerUpSpawner`. It's currently used by:

- **TL4:** `EnemyBase` drops a random power-up when an enemy dies.
- **TL1:** `PowerUpHUD` shows active power-ups and their timers.
- **TL2:** world generation spawns pickups along the street. This still calls
  `SpawnPowerUp` through reflection rather than the contract.

## The player side

The effects never touch the player directly. They call `IPowerUpTarget`,
an adapter on the player:

| Member | Implemented by `PlayerPowerUpTarget` |
| ------ | ------------------------------------ |
| `SetSpeedMultiplier(float)` | TL6 `IPlayerControllerTL6.SetTemporarySpeedMultiplier`. TL6 multiplies it on top of TL5's permanent speed upgrade. |
| `Heal(int)` | TL6 `PlayerHealth.Heal` |
| `SetPickupRadius(float)` / `PickupRadius` | Stored on the adapter. TL3 owns the pickup radius. |
| `Position` | The player's transform |

The manager finds the adapter by looking for the GameObject tagged `Player`
and calling `GetComponent<IPowerUpTarget>()`. In the generated world, TL2
adds its own adapter, `GeneratedPlayerPowerUpTarget`.

## Scene setup

1. Add the `PowerUpSystem` prefab (`Assets/prefabs/TL3_Joe/PowerUps/`), which
   holds `PowerUpManager` with the three definitions assigned.
2. Tag the player `Player`, give it a 2D collider, and add
   `PlayerPowerUpTarget` (plus TL6's `PlayerHealth` and
   `PlayerControllerTL6` for healing and speed).
3. For other features to receive `IPowerUpSpawner`, the scene needs the
   `ServiceContainer` prefab.

Demo scenes: `Assets/Scenes/TL3_Joe/SpawnTest.unity`,
`Assets/tst/TL3_Joe/Test_Scene_TL3.unity`, and the stress test
`Assets/tst/TL3_Joe/Stress_Test_TL3.unity`.

## Tests

Open **Window > General > Test Runner**, choose the **PlayMode** tab, and run
`Tests_TL3`. Tests call `PowerUpManager.Tick(seconds)` directly instead of
waiting, so the suite runs instantly.

| File | Covers |
| ---- | ------ |
| `SpeedBoostEffectTests.cs` | Multiplier per stack, the 2.5x cap even with bad settings, invalid input |
| `EffectTestsTL3.cs` | Magnet and Shield settings, apply, snapshot and restore |
| `BindingTestsTL3.cs` | Dynamic binding through the `PowerUpEffect` base type |
| `PowerUpManagerTests.cs` | Stacking, timer refresh, expiry and restore, overlapping effects, the `IPowerUpSpawner` event and spawn validation |
| `PlayerPowerUpTargetTests.cs` | Heal reaching `PlayerHealth`, speed reaching the controller, pickup-to-expiry through the manager |

`MockPowerUpTarget` and `FakePlayerController` stand in for the player so
effects can be checked without TL6's full player.

## Files

| Path | Contents |
| ---- | -------- |
| `Assets/src/TL3_Joe/` | `PowerUpManager`, `IPowerUpTarget`, `PlayerPowerUpTarget`, `StatSnapshot` |
| `Assets/src/TL3_Joe/PowerUps/` | `PowerUpEffect` and its three subclasses, `PowerUpItem` |
| `Assets/src/TL3_Joe/Definitions/` | `PowerUpDefinition` and the three definition assets |
| `Assets/src/TL3_Joe/demo/` | Demo-only scripts (`TestPlayerRunner`, `BindingDemoJoe`) |
| `Assets/src/Contracts/TL3_Joe/` | `IPowerUpSpawner`, `PowerUpType`, `PowerUpCollectedInfo` |
| `Assets/tst/TL3_Joe/` | Tests and test scenes |

## Known issues

- **Speed Boost in the generated world:** TL2's `GeneratedPlayerPowerUpTarget`
  writes TL6's old `_maxSpeedX` field through reflection, and that field no
  longer exists, so the boost has no effect there. The fix is either to use
  `PlayerPowerUpTarget` on the TL6 player prefab, or to have TL2's adapter call
  `SetTemporarySpeedMultiplier`.
- **Magnet range upgrade:** TL5's `MagnetRangeEffect` is empty because there is
  no agreed way yet for a permanent upgrade to raise TL3's base pickup radius.
