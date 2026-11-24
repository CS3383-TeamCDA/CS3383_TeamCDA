# UpgradeSystemTestsDocumentation

**Project:** Escape the 90s (Unity / C# PC build)
**Feature:** TL5 – Meta Progression & Persistent Upgrade System
**Class:** `UpgradeSystemTests` (NUnit test fixture)
**Scope of this deliverable:** automated tests for `MetaProgressionManager`, `UpgradeDefinition` and the effect classes.

---

## 1. Description

Tests the upgrade logic: currency, purchasing, snapshots and starting bonuses. Each test runs against a fresh `MetaProgressionManager` with four test definitions (max level 3, base cost 10, growth 2, base value 1, 5 per level).

### Isolation

- `SetUp` backs up and deletes `metaprogression.json`, clears the manager's static `cache` by reflection, creates the manager, and injects the definitions by reflection (the field is a private `[SerializeField]`).
- `TearDown` destroys the manager, restores the original save file, and clears the cache again.
- `UpgradeDefinition` has only private serialized fields, so test definitions are built with `JsonUtility.FromJson`.
- `FakePlayer` implements `IPlayerPermanentUpgradeReceiver` and records the bonuses it receives.

## 2. Setup

1. Put `UpgradeSystemTests.cs` in a `Tests/` folder (e.g. `Assets/Tests/EditMode/`).
2. The folder's Assembly Definition must reference the game assembly and `UnityEngine.TestRunner` / `UnityEditor.TestRunner`, with the NUnit precompiled reference.
3. Open Window > General > Test Runner and run the tests.

## 3. Tests

| Area | Test | Checks |
| --- | --- | --- |
| Definition | EffectValue | `baseValue + valuePerLevel * level`. |
| Definition | Cost | Cost rises with each level. |
| Definition | Effect | Each `UpgradeType` maps to its effect class. |
| Currency | AddCurrency positive | Currency rises; event raised with new value. |
| Currency | AddCurrency 0 / negative | Ignored; no event. |
| CanPurchase | Too poor / affordable / max level | False / true / false. |
| Purchase | Success | Cost deducted, level +1, event raised. |
| Purchase | Failure | Nothing changes, no event. |
| Purchase | Max level | Level never exceeds max. |
| Purchase | Persistence | `SaveSystem.Load()` returns the purchased state. |
| Purchase | Effect value | Value rises after a purchase. |
| Snapshot | Round trip | Currency and levels restored. |
| Snapshot | Copy | Editing a snapshot does not change the manager. |
| Restore | Clamp | Levels clamped to max. |
| Restore | Negative currency | Clamped to 0. |
| Restore | Null | Ignored. |
| Restore | Short array | Missing entries default to 0. |
| Bonuses | ApplyStartingBonuses | Player receives health, attack and speed values. |

## 4. Notes and Limits

- **Magnet range is not tested:** `MagnetRangeEffect.Apply` is empty.
- **`FakePlayer` must match the interface.** If `IPlayerPermanentUpgradeReceiver` has more members than the three shown, add them or the file will not compile.
- **`Awake` runs on `AddComponent`.** The manager calls `SaveSystem.Load()` in `Awake`; the tests then reset state with `RestoreSnapshot`. If `DependencyBehaviour.Awake` needs injected services, the setup will need adjusting.
- **Cost formula:** `GetCost` returns `baseCost + costGrowth^level`. This is additive, which may not be intended (multiplying would give exponential growth). The test only checks that cost increases, so it passes either way.
- **Not covered:** `UpgradeSegmentBar` and `CurrencyCounter` (UI; need PlayMode tests), and the game-state-triggered `ApplyWhenPlayerExists` coroutine.
- **Not run in Unity:** this code has not been compiled or tested in this environment.

---

## 5. AI Interaction Log

Tool: Claude (Anthropic), chat interface. Inputs given to the AI: the project's C# files and documentation. The system diagram image and Champion Document were not available in this conversation. Instructions: keep code succinct, avoid extra features, and write a documentation file for each class.

| # | Request | AI response | Outcome |
| --- | --- | --- | --- |
| 1 | Create a test file for the upgrade system, given existing save system tests. | Wrote `UpgradeSystemTests` covering definition maths, currency, purchase rules, persistence, snapshots and starting bonuses, isolated from the real save file. | First version of the test class. |

### Verification

The AI did not compile or run any code in Unity. The tests should be run in the Unity Test Runner by the team.