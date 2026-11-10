# Integration Checklist

Everything left to get every feature talking through `EscapeThe90s.Contracts`
and the project building and running end to end. Each item names an owner.
Check items off as they land on `main`.

How to add a contract: [CONTRACTS_GUIDE.md](CONTRACTS_GUIDE.md).
Example of a finished one: [Contracts/TL1_Chris/README.md](../../Assets/src/Contracts/TL1_Chris/README.md).

## Order of work

1. **Contracts first.** Each team adds its interfaces and payload types to
   `Assets/src/Contracts/TLx_Name/`. These are small and have no behavior, so
   merge them quickly. Everyone else codes against them.
2. **Implement and register.** Each team implements its own contracts, then
   it gets registered in `ServiceContainer`.
3. **Switch consumers.** Replace direct references to other teams' classes
   with `IDependent<IContract>`, then remove the cross-team asmdef references.
4. **Wire, test, document.**

To avoid merge conflicts, one person makes all `ServiceContainer.cs` and
`EscapeThe90s.Composition.asmdef` edits (see Composition below). Send them your
provider class and contract name.

## Rules for every contract

- [ ] No `UnityEngine` types (`Vector3`, `Transform`, `GameObject`). Use
      `WorldPosition2D`, plain floats/ints, or your own struct in Contracts.
- [ ] Enums other teams need (for example `PowerUpType`, `EnemyType`) move into
      Contracts.
- [ ] Events use `Action` or `Action<T>`, where `T` is a Contracts or plain C# type.

## 0. Build is green (today)

- [ ] **Everyone:** pull `main`, open Unity, and confirm the console shows no
      compile errors.
- [ ] **TL1:** let Unity recompile today's `IGameState` changes (the last
      successful compile was before them) and fix anything it reports.
- [ ] **Everyone:** CI (`Run Unity Tests`) passes on `main` before starting step 3.
- [ ] **TL4 (optional):** replace obsolete `FindFirstObjectByType` in
      `EnemyBase.cs:236` and the `FindObjectsSortMode` overload in
      `DeleteExeAttack.cs:31`. These are warnings, not errors.

## TL1 — Chris (UI/HUD, Game State)

- [x] `IGameState` contract: `CurrentStateType`, `OnStateTypeChanged(GameStateChange)`,
      and `Request*` methods. `GameStateType` and `GameStateChange` in Contracts.
- [x] `GameStateManager` implements it; registered in `ServiceContainer`.
- [x] Docs: `CONTRACTS_GUIDE.md`, `Contracts/TL1_Chris/README.md`, coding standard.
- [ ] Commit and push the `IGameState` work.
- [ ] EditMode test: each transition raises exactly one `OnStateTypeChanged`
      with the right `Previous`/`Current`, and `IsNewRun` is true on start and
      restart (including restart from pause) and false on resume.
- [ ] Subscribe to TL6 `IPlayerHealth.OnPlayerDied` and call
      `RequestGameOver()`. This is the champion-doc rule "OnPlayerDied causes
      exactly one transition to GameOver." It is not wired up today.
- [ ] `HUDController` subscribes to:
  - [ ] TL6 health changed → `UpdateHealth`
  - [ ] TL4 enemy defeated → `UpdateScore`
  - [ ] TL5 currency changed → `UpdateCurrency`
  - [ ] TL3 power-up collected → power-up display
- [ ] Decide the entry scene: either add `Scenes/TL1_Chris/StartMenu.unity`
      to Build Settings as scene 0 and load the game scene on Start, or put the
      `GameStateManager` prefab in `GeneratedWorldGameDemo.unity`. Today only
      the game scene is in the build, and `ServiceContainer` spawns a fallback
      manager.
- [ ] Update the TL1 champion doc Interface Contract section to list
      `IGameState` and its events.

## TL2 — Caleb (Level Generation)

- [x] `ILevelProgress` and `IPlayerPositionSource` already in Contracts.
- [ ] Move them into `Contracts/TL2_Caleb/` (optional, for consistency).
- [ ] `WorldGen.Integration` asmdef references `Joe.Game` and `TL6Assembly`
      directly. Switch `GeneratedPlayerPowerUpTarget`,
      `GeneratedStreetContent`, and `GeneratedWorldComposition` to the TL3,
      TL4, and TL6 contracts once they exist, then remove those references.
- [ ] Stop reading TL6 private fields by reflection
      (`_groundLayer`, `_maxSpeedX` in `GeneratedWorldComposition.ConfigurePlayer`).
      Ask TL6 for contract members instead. The RFP forbids editing another
      feature's private internals.
- [ ] Retire `IGameplaySessionState` and `GameStateEventBridge`. TL1's
      `IGameState` replaces both. In this order:
  - [ ] `WorldNetworkDemo` / `GeneratedWorldComposition.IsGameplayStarted()`
        implement `IDependent<IGameState>` and start the world from
        `OnStateTypeChanged` when `IsNewRun` is true, or check
        `IGameState.CurrentStateType == GameStateType.Playing`
        (same meaning as `IsGameplayStarted`).
  - [ ] Delete `IGameplaySessionState` from `Contracts/GameplayEvents.cs`.
  - [ ] Remove the bridge from `GeneratedWorldGameDemo.unity`, delete
        `GameStateEventBridge.prefab`, and clear the `gameStateBridgePrefab`
        field on `ServiceContainer.prefab`.
  - [ ] Composition owner: delete `GameStateEventBridge.cs`, `ResolveBridge()`,
        the bridge registrations, and the `Register(gameStateManager)` line.
        Until then, keep that line, or the world builds behind the menu.

## TL3 — Joe (Power-Ups)

- [ ] Contract, for example `IPowerUpService`:
      `SpawnPowerUp(WorldPosition2D position, PowerUpType type)` and
      `event Action<PowerUpCollectedInfo> OnPowerUpCollected`.
- [ ] Move `PowerUpType` into Contracts.
- [ ] Contracts version of `PowerUpCollectedEventArgs`, with `Vector3 Position`
      changed to `WorldPosition2D` (or dropped).
- [ ] `PowerUpManager` implements it; send it to Composition to register.
- [ ] `PlayerPowerUpTarget` uses the TL6 contract instead of `PlayerHealth`
      and the commented-out `PlayerController` call. Speed boost doesn't reach
      the player today.
- [ ] Remove `TL6Assembly` from `Joe.Game.asmdef`.

## TL4 — Tyler (Enemies & Combat)

- [ ] Contract, for example `IEnemySpawner`: `SetSpawnRate(float rate)` and
      `event Action<EnemyDefeatedInfo> OnEnemyDefeated`.
- [ ] Agree the payload with TL1 and TL5. The TL1 champion doc says
      `Action<int>` (score), but the code is `Action<EnemyType, Vector3>`.
      Suggested: a struct with `EnemyType Type`, `int ScoreValue`,
      `WorldPosition2D Position`.
- [ ] Move `EnemyType` into Contracts.
- [ ] `EnemySpawner` implements it; send it to Composition to register.
- [ ] `EnemyProjectile`, `EnemyAttackAnimations` → TL6 `IPlayerHealth.TakeDamage`.
- [ ] `EnemyBase` power-up drops → TL3 `IPowerUpService.SpawnPowerUp`.
- [ ] Remove `TL6Assembly` and `Joe.Game` from `TL4Assembly.asmdef`.

## TL5 — Will (Meta Progression)

- [ ] Contract, for example `ICurrency`: `AddCurrency(int amount)`,
      `int CurrentCurrency`, and `event Action<int> OnCurrencyChanged`.
- [ ] `MetaProgressionManager` implements it; send it to Composition to register.
- [ ] Remove the direct `HUDController` field. TL1 subscribes to
      `OnCurrencyChanged` instead.
- [ ] `EnemyCoinRewardListener` → TL4 `IEnemySpawner.OnEnemyDefeated`
      instead of a serialized `EnemySpawner`.
- [ ] `ApplyStartingBonuses`: subscribe to TL1 `IGameState.OnStateTypeChanged`
      and, when `IsNewRun` is true, apply bonuses through TL6 contract members. Update use case RS1:
      bonuses are applied right after Playing is entered, in the same frame.
- [ ] Remove `Chris.Game` and `TL4Assembly` from `TL5Assembly.asmdef`.

## TL6 — Matteo (Player Controller)

TL3, TL4, TL5, and TL2 all depend on this, so it should land first.

- [ ] Contract, for example `IPlayerHealth`: `TakeDamage(int)`, `Heal(int)`,
      `CurrentHealth`, `MaxHealth`, `event Action<int, int> OnHealthChanged`,
      `event Action OnPlayerDied`.
- [ ] Contract for movement: `SetSpeedMultiplier(float)`. It doesn't exist
      yet, and the RFP requires it. Also implement `IPlayerCapabilities`
      (already in Contracts, no provider yet).
- [ ] Members TL5 needs for starting bonuses (health, speed, magnet range).
- [ ] Members TL2 needs to replace its reflection (ground layer, max speed).
- [ ] The player is spawned at runtime, so make sure it is passed to
      `ServiceContainer.InjectDynamicObject`. Register its contracts once it
      exists.
- [ ] RFP names `PlayerController`; the class is `TempPlayerController`.
      Rename it or note the change in the TL6 champion doc.

## Composition — one owner (suggested: Caleb or Chris)

- [ ] Register each new provider in `ServiceContainer.Awake`:
  - [ ] `IPowerUpService` (TL3)
  - [ ] `IEnemySpawner` (TL4)
  - [ ] `ICurrency` (TL5)
  - [ ] `IPlayerHealth`, movement, `IPlayerCapabilities` (TL6)
  - [ ] `ILevelProgress`, `IPlayerPositionSource` (TL2), if not registered elsewhere
- [ ] Add `Joe.Game`, `TL4Assembly`, `TL5Assembly`, `TL6Assembly` to
      `EscapeThe90s.Composition.asmdef` as needed. Composition is the only
      assembly allowed to reference team assemblies.
- [ ] Runtime-spawned providers (player, enemies, pickups) are registered or
      injected after they are created.
- [ ] No component logs `... is missing <Type>` on Play.

## Final verification (day 2)

- [ ] No team asmdef references another team's assembly. Test asmdefs may.
- [ ] Clean clone builds in Unity with zero errors.
- [ ] CI passes on `main`.
- [ ] Full run in the build scene: menu → start → take damage (HUD health
      updates) → kill enemy (score and currency update) → collect power-up →
      pause/resume → die (game over) → restart (clean run, starting bonuses
      applied) → return to menu.
- [ ] Every champion doc's Interface Contract section matches the final code
      (RFP §5: documentation consistent with final code).
