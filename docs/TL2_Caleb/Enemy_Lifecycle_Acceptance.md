# Enemy lifecycle acceptance — 2026-11-12

Tested revision: `5fa8335a9f34a449f142012c199bd804699ab89b` on `main`, plus the pre-existing local streaming work and the test changes below. No staging, commit, push, stash mutation, or scene save was performed. This report supersedes the street-fixture proposal in the earlier streaming owner review.

## Changes in this batch

- `Assets/tst/TL4_Tyler/Tests/GeneratedWorldEnemyLifecycleTests.cs`: four setup lines explicitly select the existing public `coordinateWorldMode = false` before RequestStart. The fixture's existing reflection convention is retained; mixed ground/flying/turret, animation, streaming, and unload assertions are unchanged. This is the only TL4 file edited, with its owner's authorization.
- `Assets/src/TL2_Caleb/Scripts/WorldGen/StreamingTests/CoordinateEnemyLifecycleTests.cs` and matching `.meta`: load the checked-in default coordinate scene, clone its settings, set seed 431, request gameplay through IGameState, observe chunk contracts and enemy instances, and move the real camera/player focus.
- `Assets/src/TL2_Caleb/Scripts/WorldGen/StreamingTests/CoordinateEnemyLifecycleObserver.cs` and matching `.meta`: subscribe after scene composition and before the first LateUpdate. Match public EnemySpawner notifications to immutable ILevelProgress spawn instructions by enemy type and published world X. No reflection or private ownership inspection is used in the coordinate test.
- Existing `StreamingTests/EscapeThe90s.WorldGen.StreamingTests.asmdef`: add Demo and TL4Assembly references to this test assembly only. Its existing `.meta` is unchanged.
- This report. No production code, Contracts, terrain rules, expectations, or golden hashes were changed in this batch.

The new fixture saves/restores Unity's random state and destroys its settings clone in teardown. Scene loading is asynchronous: the first draft failed at setup because it queried composition immediately. A bounded scene-activation wait fixed the test harness; the diagnostic is retained in `Temp/streaming-acceptance/lifecycle-coordinate-before-scene-wait.json`. No production lifecycle defect was demonstrated.

## Reproduction and named outcomes

Both cases pass in isolation and in the unfiltered final PlayMode run:

- `GeneratedWorldEnemyLifecycleTests.GeneratedWorldSpawnsMixedEnemiesAndRetiresThemWithUnloadedChunks`: explicit street compatibility mode; existing mixed-enemy and complete initial-window retirement assertions pass.
- `EscapeThe90s.WorldGen.Tests.CoordinateEnemyLifecycleTests.DefaultCoordinateScene_ChunkRecycled_RetiresOwnedEnemiesAndPreservesLoadedNeighbors`: default coordinate mode; seed **431**. Chunk **(-2,0)** publishes one Ground enemy instruction and produces the corresponding live enemy. Chunk **(1,0)** publishes one Flying enemy instruction and produces that enemy. Move the actual player focus to **X=32**, Y=final surface at 32 + 3, with physics disabled for this controlled movement. Observe (-2,0)'s recycle event, confirm its terrain hash is no longer available and its original enemy is destroyed after the deferred-destruction frame. Confirm (1,0) has not recycled, remains loaded, and retains its original enemy. Every assertion includes seed, chunk and lifecycle stage.

## Compilation and full suites

Unity 6000.6.0f1, Windows Editor. `unity recompile --project-path . --timeout 60 --format json` completed with zero errors/warnings after adding the coverage. The final pre-suite request reported `up_to_date`, not compiling, with zero errors/warnings. A transient Pipeline disconnect/domain-reload response was retried; no source change was made to bypass it.

| Suite | Discovered | Passed | Failed | Skipped | Inconclusive |
| --- | ---: | ---: | ---: | ---: | ---: |
| EditMode, unfiltered | 124 | 124 | 0 | 0 | 0 |
| PlayMode, unfiltered | 304 | 295 | 9 | 0 | 0 |

All 124 previous EditMode cases and all 303 previous PlayMode cases remain present. One new PlayMode case was discovered and passed. All 18 coordinate-building regression cases pass. `GoldenCoordinateWindow_FixedSeed_MatchesPinnedHash(431UL,-4,4,-2,2,"3B13AA50F0DB0C56")` passes without re-pinning. The four existing coordinate streaming acceptance cases pass.

Authoritative named results (repository-relative; Temp outputs remain local):

- `Temp/streaming-acceptance/lifecycle-final-editmode.json`
- `Temp/streaming-acceptance/lifecycle-final-playmode.json`
- `Temp/streaming-acceptance/lifecycle-final-failures.json` — complete assertion messages and stacks
- `Temp/streaming-acceptance/lifecycle-baseline-comparison.json` — exact per-name comparison
- `Temp/streaming-acceptance/lifecycle-street-isolated.json`
- `Temp/streaming-acceptance/lifecycle-coordinate-isolated.json`
- `Temp/streaming-acceptance/lifecycle-new-enemy-isolated.json`
- `Temp/streaming-acceptance/lifecycle-compile.json`, `lifecycle-final-compile.json`
- `Temp/streaming-acceptance/lifecycle-final-source-manifest.json`

The comparison baseline remains the preserved `final-editmode.json` (124/124) and `final-playmode.json` (295/303, eight failures). The precise difference is: the street case changes Failed → Passed; the new coordinate case passes; the other seven baseline failures remain; two previously passing TL4 NewEnemy cases now fail. There are no missing cases. This is **not** a seven-failure result.

## Remaining failures — owner TL4

All locations below are under `Assets/tst/TL4_Tyler/`. No fixes or assertion edits have been applied to these files.

| Case | First actionable diagnostic | Relation to baseline |
| --- | --- | --- |
| TL4ContractStateTests.CorpseIgnoresPlayerButKeepsSolidTerrainContact | PlayMode/TL4ContractStateTests.cs:127: player collision ignored expected true, actual false | Same failure |
| TL4ContractStateTests.DefeatedFlyingEnemyFallsAndSettlesOnTerrain | PlayMode/TL4ContractStateTests.cs:90: gravity >0 expected, actual 0 | Same failure |
| TurretBossTests.FortyPercentTransformsOnceAndMobileLoopReplacesFloorAttacks | PlayMode/TurretBossTests.cs:299: recovery >2.5 expected, actual 1.20000005 | Same failure |
| TurretBossTests.MobileOpeningsReactToDistanceCooldownAndRepeatHistory | PlayMode/TurretBossTests.cs:28: ShortChargeWarning expected, StompWarning actual | Same failure |
| TurretBossVisualRigTests.IdlePlantsAllFeetAndWalkingLiftsOnlyOneAtATime | PlayMode/TurretBossVisualRigTests.cs:39: root children 3 expected, actual 15 | Same failure |
| NewEnemyTests.DistantPlayerGetsGrenadeAndRageStopsFurtherThrows | Tests/NewEnemyTests.cs:235: TryAcquireTarget true expected, actual false | Same failure |
| TurretGameplayTests.HighAwarenessTurretTracksShootsAndStaysStationary | Tests/TurretGameplayTests.cs:47: position (0,0,0) expected, actual (0,-52.45,0) | Same failure, physics Y varies |
| NewEnemyTests.GrenadierStopsThrowingBeyondMaximumButRetainsAggroAndResumesInRange | Tests/NewEnemyTests.cs:253: TryAcquireTarget true expected, actual false | Previously passed |
| NewEnemyTests.RagePassesThroughExistingAndNewEnemiesWithoutCrowdSlowdown | Tests/NewEnemyTests.cs:305: horizontal velocity >0 expected, actual 0 | Previously passed |

Independent `NewEnemyTests` run: 31 discovered, 29 passed, two failed (DistantPlayer and RagePassesThrough). GrenadierStopsThrowing passed that isolated run, demonstrating a variable outcome. EnemyBase.ChooseAwarenessRange uses UnityEngine.Random to select Close (6) or Medium for Low awareness; all three scenarios place their target eight cells away. Rage movement delegates to the shared target-acquisition policy. This explains a setup dependency and supports an owner-reviewed awareness setup fix; the isolated result does not prove every possible failure in those scenarios has that sole cause.

Small proposed owner change, **unapplied**, immediately after creating the GrenadeBerserker in each of those three scenarios:

```diff
         var enemy = Enemy<GrenadeBerserkerEnemy>("GrenadeBerserker");
+        enemy.SetAwareness(EnemyAwareness.High);
```

Both High-awareness ranges include the existing eight-cell target. Keep every assertion and prefab balance unchanged; the owner must approve and verify this fixture setup decision. Other smallest proposals and corpse/boss policy conflicts remain in `Streaming_Acceptance_Owner_Review.md`.

The historical `EnemyGameStateBinding.cs:29` shutdown exception is separate. It does not occur in the captured fresh suite failure diagnostics; this batch does not establish that all shutdown orderings are safe. TL4/Infrastructure retain responsibility for that defensive lifecycle review.

## Preservation and next gate

HEAD is unchanged. The index remains empty. The local-only stash is still `072c3d5b774ab5d547f581c97de4d4f73e2e24ed`. The scene SHA256 is unchanged: `F2E00A2C631FCDB204137C935A7EFDBF21BC08D2961EE759D2B161E80D925AE7`. A comparison with the prior source manifest found only the intended test-assembly reference change among pre-existing streaming files. Existing local production edits were preserved.

Enemy ownership acceptance in both modes is verified for the scenarios above. All-project acceptance remains **open**: nine discovered PlayMode cases fail. Next action is owner resolution of those blockers and a complete suite rerun. The next TL2 RFP implementation batch is **R03: active coordinate Ground/Gap/Ramp behavior**, with deterministic ownership, jump-bounded gaps, seam/playability coverage, and the current golden hash retained unless an intentional terrain change is explained and separately reviewed. Do not use this report to mark R03, all of R11, or all-project acceptance complete. Defer optional polish; the user handles all Git writes.
