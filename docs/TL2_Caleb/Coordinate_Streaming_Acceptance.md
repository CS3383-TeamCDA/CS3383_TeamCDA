# Coordinate streaming acceptance and performance — 2026-11-12

**All-project-tests-pass condition: OPEN.** Final PlayMode has eight owner-review failures. No tests were disabled, ignored, filtered out of the complete runs, or given weaker assertions. No pinned expectation changed.

## Revision and preservation

The batch began at `921719811ad0668158a20bc57e9f1675426a0fbd` with the existing building fixes and scene diff. During the batch the user's building-fix commit advanced HEAD to `5fa8335a9f34a449f142012c199bd804699ab89b`; performance work remains local. This worker performed no Git staging, commit, stash, pull or push operations. Stash `072c3d5b774ab5d547f581c97de4d4f73e2e24ed` remains intact. The scene bytes still match the start-of-batch SHA256 `F2E00A2C631FCDB204137C935A7EFDBF21BC08D2961EE759D2B161E80D925AE7`.

## Complete suite results

| Run | Discovered | Passed | Failed | Skipped | Inconclusive |
| --- | ---: | ---: | ---: | ---: | ---: |
| Baseline EditMode | 123 | 123 | 0 | 0 | 0 |
| Baseline PlayMode | 299 | 291 | 8 | 0 | 0 |
| Final EditMode | 124 | 124 | 0 | 0 | 0 |
| Final PlayMode | 303 | 295 | 8 | 0 | 0 |

Fresh script compilation completed with zero errors/warnings before the final runs. Every baseline case name is present in its final suite. All 18 building regressions pass. `GoldenCoordinateWindow_FixedSeed_MatchesPinnedHash(431UL,-4,4,-2,2,"3B13AA50F0DB0C56")` passes unchanged.

Named result files are in `Temp/streaming-acceptance/`: `baseline-editmode.json`, `baseline-playmode.json`, `final-editmode.json`, `final-playmode.json`, `final-failures.json`, `final-compile-request.json`. Temp stays local and ignored. The complete command used `test run --mode editor` / `--mode playmode`, without a filter. Targeted iteration results are separately labelled.

Final remaining failures (see JSON for exact messages and stack lines):

1. `TL4ContractStateTests.CorpseIgnoresPlayerButKeepsSolidTerrainContact`: expected ignored-player collision true, false at TL4ContractStateTests.cs:127.
2. `TL4ContractStateTests.DefeatedFlyingEnemyFallsAndSettlesOnTerrain`: expected gravity > 0, actual 0 at :90.
3. `TurretBossTests.FortyPercentTransformsOnceAndMobileLoopReplacesFloorAttacks`: recovery > 2.5, actual 1.2 at TurretBossTests.cs:299.
4. `TurretBossTests.MobileOpeningsReactToDistanceCooldownAndRepeatHistory`: ShortChargeWarning expected, StompWarning actual at :28.
5. `TurretBossVisualRigTests.IdlePlantsAllFeetAndWalkingLiftsOnlyOneAtATime`: expected 3 root children, actual 15 at TurretBossVisualRigTests.cs:39.
6. `GeneratedWorldEnemyLifecycleTests.GeneratedWorldSpawnsMixedEnemiesAndRetiresThemWithUnloadedChunks`: street loaded count > 0 expected, 0 actual at GeneratedWorldEnemyLifecycleTests.cs:104; fixture assumes street mode while gameplay defaults to coordinates.
7. `NewEnemyTests.DistantPlayerGetsGrenadeAndRageStopsFurtherThrows`: TryAcquireTarget true expected, false actual at NewEnemyTests.cs:235. Also reproduced in an independent fixture-only run, 30/31 passed.
8. `TurretGameplayTests.HighAwarenessTurretTracksShootsAndStaysStationary`: position zero expected, actual Y -51.81 at TurretGameplayTests.cs:47.

These are TL4 implementation/fixture-owner blockers. Unapplied minimal proposals and policy conflicts are documented in `Streaming_Acceptance_Owner_Review.md`. The baseline TVClaw pursuit failure and an intermediate NewEnemy rage failure are order-sensitive awareness scenarios; they passed the final run, not silently removed. The new stress fixture saves/restores Unity's global random state. No TL4 files were changed. The historical EnemyGameStateBinding.cs:29 shutdown exception did not reproduce in the captured fresh suite diagnostics and remains separately documented.

## ST1 / ST3 / ST4 protocol

- Production pipeline: `CoordinateWorldStreamer.StreamOnce` → `CoordinateWorldChunkStore` → pooled `CoordinateChunkView`, with the real WorldArt asset.
- Seed 431; baseline speed 16 cells/s, then 32 and 64; fixed simulated 1/60-second steps. The 2,000-chunk paths cross negative and positive X, starting after a 64-chunk warm-up at chunk X -936.
- Cold startup settles before warm-up/measurement. Required lookahead is the **entire** speed-expanded X/Y window; every measured step checks every requested coordinate. This is stronger than checking only the camera edge. Speed-adapter lookahead is explicitly asserted.
- Separate terrain-only and real spawned-content sweeps (building sprites, ground/flying/turret prefabs, and the PowerUpSystem prefab). Content callbacks have their own timing/allocation scopes. These do not emulate a full player/combat session.
- Pool metrics are deltas after warm-up. Loaded-entry bound includes maximum configured lookahead and X/Y hysteresis. Loaded entries include uniform data-only chunks; views are counted separately.
- ST3 compares the actual loaded surface-view content hash at each of 2,000 matching coordinates across speeds. Hash capture/readiness checks/assertions/reporting are outside the measured store section.

Both terrain-only and content sweeps produced:

| Speed | Measured steps | Peak loaded / configured bound | Warmed hits / requests | Views created after warm-up | Missing required windows | Missing/different hashes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1x | 120,000 | 45 / 105 | 2,000 / 2,000 (100%) | 0 | 0 | 0 / 0 |
| 2x | 60,000 | 45 / 105 | 1,999 / 1,999 (100%) | 0 | 0 | 0 / 0 |
| 4x | 30,000 | 50 / 105 | 1,999 / 1,999 (100%) | 0 | 0 | 0 / 0 |

All four new PlayMode cases pass:

- `ST1ST3ST4_TerrainOnly_AllSpeedsMeetStreamingAcceptance`
- `ST1ST3ST4_SpawnedContent_AllSpeedsMeetStreamingAcceptance`
- `Stream_ReplacementWindow_ReusesRetiringViewBeforeRent`
- `StreamOnce_SettledWindow_RecordsZeroAllocationEvents`

## Measurements and fixes

Environment: Unity 6000.6.0f1, Windows 11 build 26200, WindowsEditor Mono, Ryzen 7 7800X3D, RTX 5060, Game view 727×409, orthographic camera size 11.25, vSync 1, targetFrameRate -1, Profiler.enabled false (explicit recorder markers are enabled for measurement). This is not a standalone/IL2CPP benchmark.

The Editor's GC.GetAllocatedBytesForCurrentThread returned zero even for a 64 KiB allocation. Initial zero-byte reports are invalid and preserved under `invalid-allocation-counter/`. Calibrated `GC.Alloc` recorders count allocation **events** inside the explicit main-thread scopes; a deliberate allocation must produce an event and recorder overflow is an assertion failure. GC.Alloc sample Value is time, not byte metadata. Whole-frame GC bytes are a separate counter and include the runner, other systems and frame context; they cannot be assigned wholly to streaming. Native byte allocations are not measured.

| Mode / speed | Terrain events | Content callback events | Terrain CPU p95 / max ms | Content CPU max ms | 240 rendered frames p95 / max ms |
| --- | ---: | ---: | ---: | ---: | ---: |
| Terrain 1x | 24,875 | 0 | 0.0018 / 0.4391 | — | 3.4453 / 7.9699 |
| Terrain 2x | 24,862 | 0 | 0.0020 / 0.3950 | — | 2.2245 / 3.2949 |
| Terrain 4x | 24,863 | 0 | 0.2159 / 6.1294 | — | 2.0642 / 3.2137 |
| Content 1x | 24,875 | 74,201 | 0.0020 / 0.4411 | 5.0985 | 3.1666 / 5.4864 |
| Content 2x | 24,862 | 90,852 | 0.0020 / 0.4479 | 3.7045 | 4.0857 / 10.7194 |
| Content 4x | 24,863 | 95,373 | 0.2287 / 0.4475 | 3.8742 | 3.7018 / 10.1690 |

Terrain allocation events decreased about 42% from calibrated pre-optimization totals 42,875 / 42,853 / 42,854. Peak terrain events per step fell from 22 to 13. Quiet settled windows record zero events over 1,000 calls. Generation still allocates immutable snapshots and payloads; spawned content still creates/destroys team objects. This is not a zero-allocation claim for chunk loads or full content.

Whole-frame byte peaks during the accelerated sweeps were terrain 165,078 / 116,340 / 109,001 and content 2,431,305 / 2,217,835 / 908,788. These frames batch up to 600 simulated steps and include runner/system work. They are context, not per-stream-step bytes. The separately reported 240 rendered frames use one step per frame with an explicit following camera. Scope timing excludes harness/log construction, assertions, array sorting and file writes; renderer-frame context necessarily includes Editor/runner overhead. Timing is observational, not a CI timing threshold.

TL2 fixes:

- A one-view replacement regression reproduced a second unnecessary view creation. Returning retired views before renting replacements fixes it; the unchanged assertion now passes.
- Loaded capacity/bound calculation now accounts for vertical hysteresis.
- Fresh generator-owned arrays transfer into immutable snapshots without redundant copies. Public construction still defensively copies; the new EditMode caller-mutation regression and existing tile-copy test pass.
- Spawn metadata uses one exact-size array instead of temporary List/backing/ToArray allocations, preserving ordering and hashes.
- Removed per-load chunk-name string allocations; coordinate labels remain in the existing Gizmo path.
- Reuse per-chunk content ownership lists. Enemy/powerup reset contracts and content object pooling remain owner/integration work; reflection-based spawning costs are still visible in the content scope.
- Added opt-in readiness/hash/measurement diagnostics to the active streamer. Normal gameplay leaves telemetry off.

Raw final speed reports: `terrain-{1,2,4}x.json` and `content-{1,2,4}x.json` in `Temp/streaming-acceptance/`. Calibrated before-fix reports are under `before-optimization/`; the rent-order failure is `pool-regression-before.json`. All work and evidence remain local for the user to review, stage, commit and push.
