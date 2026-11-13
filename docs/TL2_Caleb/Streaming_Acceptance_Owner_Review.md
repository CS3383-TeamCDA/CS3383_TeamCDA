# Streaming acceptance: owner review (2026-11-12)

Baseline HEAD: `921719811ad0668158a20bc57e9f1675426a0fbd` plus the preserved building fixes. No Git mutations are authorized in this batch. Raw named baseline results and first diagnostic stacks are in `Temp/streaming-acceptance/baseline-{editmode,playmode}.json` and `baseline-failures.json`.

Baseline: EditMode 123/123; PlayMode 299 discovered, 291 passed, 8 failed, zero skipped/inconclusive. The following changes are **proposals only**, not applied to TL4 files. Assertions and pinned expectations remain intact.

## First actionable diagnostics

| Case | First failure | Assessment / owner |
| --- | --- | --- |
| TL4ContractStateTests.CorpseIgnoresPlayerButKeepsSolidTerrainContact | TL4ContractStateTests.cs:127: ignored-player collision expected true, false | TL4 corpse-policy conflict below |
| TL4ContractStateTests.DefeatedFlyingEnemyFallsAndSettlesOnTerrain | TL4ContractStateTests.cs:90: gravity > 0, actual 0 | EnemyBase disables simulation before FlyingEnemy.Move; TL4 |
| TurretBossTests.FortyPercentTransformsOnceAndMobileLoopReplacesFloorAttacks | TurretBossTests.cs:299: recovery > 2.5, actual 1.2 | RecoveryDuration halves its computed duration; TL4 balance/API decision |
| TurretBossTests.MobileOpeningsReactToDistanceCooldownAndRepeatHistory | TurretBossTests.cs:28: ShortChargeWarning, actual StompWarning | Weighted random opener no longer implements the exact deterministic policy asserted by the fixture; TL4 decision |
| TurretBossVisualRigTests.IdlePlantsAllFeetAndWalkingLiftsOnlyOneAtATime | TurretBossVisualRigTests.cs:39: root children 3 expected, 15 actual | First AdvanceDamageEffects initializes 12 damage sparks after root count is captured; TL4 |
| GeneratedWorldEnemyLifecycleTests.GeneratedWorldSpawnsMixedEnemiesAndRetiresThemWithUnloadedChunks | GeneratedWorldEnemyLifecycleTests.cs:104: loaded street chunks > 0, actual 0 | Fixture inspects StreetView while scene defaults to coordinate mode; TL4 fixture owner / TL2 compatibility coordination |
| TurretGameplayTests.HighAwarenessTurretTracksShootsAndStaysStationary | TurretGameplayTests.cs:47: position zero, actual Y -52.45 | Dynamic turret has gravity and fixture has no supporting terrain; TL4 stationary-body policy |
| TVClawArtTests.HunterRunsThroughLivingEnemiesCorpsesAndNewArrivals | TVClawArtTests.cs:44: positive horizontal velocity, actual 0 | Fixture places target 8 units away; current Low-awareness prefab has Close radius 6; TL4 fixture setup |

Paths in the table are under `Assets/tst/TL4_Tyler/{Tests,PlayMode}`. Final results may report slightly different physics positions; use the final named results as the authoritative final observation.

## Small, unapplied owner proposals

### Keep the existing lifecycle fixture as a street compatibility test

In `Assets/tst/TL4_Tyler/Tests/GeneratedWorldEnemyLifecycleTests.cs`, after finding `demo` and before RequestStart, explicitly select the mode already asserted throughout the fixture:

```diff
         Assert.IsNotNull(demo, "The scene must contain WorldNetworkDemo.");
+        FieldInfo coordinateMode = demo.GetType().GetField("coordinateWorldMode");
+        Assert.IsNotNull(coordinateMode);
+        coordinateMode.SetValue(demo, false);
```

This runtime scene field is destroyed with the test scene. No asset field or assertion changes. A new coordinate lifecycle test is still required; this does not turn the old fixture into one.

### Recovery duration: remove the accidental public half-duration, if the existing contract is authoritative

`Assets/src/TL4_Tyler/TurretBoss/TurretBossController.cs`:

```diff
-    public float RecoveryDuration => recoveryDuration * 0.5f;
+    public float RecoveryDuration => recoveryDuration;
```

This also lengthens the state-machine recovery, because Advance uses this property. TL4 must approve that balance change before application.

### Initialize damage effects before the visual-rig hierarchy is considered built

`Assets/src/TL4_Tyler/TurretBoss/TurretBossVisualRig.cs`:

```diff
-    private void Awake() { Build(); }
+    private void Awake()
+    {
+        Build();
+        if (body != null) EnsureDamageEffects();
+    }
```

The twelve spark nodes then exist before reload bookkeeping begins. Retain all existing hierarchy assertions and rerun the visual-rig suite.

### Put the TVClaw fixture's target inside its configured detection radius

`Assets/tst/TL4_Tyler/Tests/TVClawArtTests.cs`, only in HunterRunsThroughLivingEnemiesCorpsesAndNewArrivals:

```diff
-            player.transform.position = Vector2.right * 8;
+            player.transform.position = Vector2.right * 4;
```

All crowd/corpse assertions remain unchanged. This tests pursuit after detection without changing prefab awareness balancing.

### Enforce stationary turret physics, if that is the intended runtime contract

In `Assets/src/TL4_Tyler/TurretHazard.cs`, append to Awake:

```diff
         animationDriver = GetComponent<EnemyAnimationDriver>();
+        Rigidbody2D body = GetComponent<Rigidbody2D>();
+        if (body != null)
+        {
+            body.gravityScale = 0f;
+            body.constraints |= RigidbodyConstraints2D.FreezePosition;
+        }
```

Alternatively the owner can supply correctly aligned terrain in the fixture. Do not weaken the stationary-position assertion. These alternatives need TL4's grounding/physics decision.

## Policy conflicts that cannot be resolved safely by a speculative one-line patch

- **Corpse physics:** EnemyBase.ConfigureCorpseCollision explicitly disables all colliders and rigidbody simulation. TVClawArtTests asserts that killed GroundEnemy colliders are disabled, while TL4ContractStateTests expects a killed GroundEnemy to ignore the player and remain a solid falling body. FlyingEnemy.Move also contains physical-corpse logic that is unreachable once simulation is disabled. TL4 must select a physical-versus-visual corpse policy, or expose an explicit configurable policy and select it in fixture setup. There is no universal collider-enable patch that preserves both sets of assertions as currently configured.
- **Boss openings:** SelectOpening uses seeded weighted selection with reduced repeat weights. The existing fixture requires exact distance-based choices and an exact alternation. Restoring those guarantees requires TL4 to define priority rules before the weighted fallback. Do not change the expected enum or seed to manufacture a pass. No unverified random-policy rewrite is proposed as a known fix.

## Separate shutdown observation

The earlier `EnemyGameStateBinding.cs:29` NullReferenceException was not reproduced in the captured baseline full-suite results. A defensive owner-review proposal for `OnDisabledAfterInitialization` is:

```diff
-        gameState.OnStateTypeChanged -= HandleStateChanged;
-        spawner.SetGameplaySpawningAllowed(false);
+        if (gameState != null) gameState.OnStateTypeChanged -= HandleStateChanged;
+        if (spawner != null) spawner.SetGameplaySpawningAllowed(false);
```

This is not a proven root-cause fix; TL4/Infrastructure should also test disable-before-injection and teardown ordering.

## Order-sensitive enemy-awareness observations

An intermediate complete run also failed `NewEnemyTests.DistantPlayerGetsGrenadeAndRageStopsFurtherThrows` at NewEnemyTests.cs:235 (TryAcquireTarget false) and `NewEnemyTests.RagePassesThroughExistingAndNewEnemiesWithoutCrowdSlowdown` at :305 (horizontal velocity zero). The first reproduced in an independent NewEnemyTests-only run (30/31 passed).

The GrenadeBerserker prefab is Low awareness. EnemyBase.ChooseAwarenessRange randomly selects Close (6 cells) or Medium, while these scenarios place their target 8 cells away. Do not rely on another fixture's global random sequence. In **each of those two scenario setups**, immediately after creating `enemy`, the smallest owner proposal is:

```diff
         var enemy = Enemy<GrenadeBerserkerEnemy>("GrenadeBerserker");
+        enemy.SetAwareness(EnemyAwareness.High);
```

Both High ranges include the existing 8-cell target; assertions and prefab balance stay unchanged. This proposal is not applied. The new TL2 stress fixture independently saves/restores UnityEngine.Random.state and seeds its own prefab variation, so it does not leave its long spawned-content run's random state for later fixtures.

## Measurement interpretation

Stress uses the production CoordinateWorldStreamer/ChunkStore/ChunkView pipeline, seed 431, warm-up 64 chunks, and fixed simulated 1/60-second steps at 16/32/64 cells per second for 2,000 chunks each. The entire requested speed-expanded X/Y window must be loaded after **every** measured step. Terrain-only and actual building/enemy/powerup content-enabled runs are separate.

`GC.GetAllocatedBytesForCurrentThread()` returned zero even for a deliberate 64 KiB allocation in this Editor. Initial zero-byte reports are invalid and retained under `invalid-allocation-counter/`. Final scoped allocation measurements use calibrated main-thread Unity GC.Alloc **event counts** (its sample Value is nanoseconds, not byte metadata). Whole-frame byte counters are separately labelled and include the Test Runner, other systems and harness frame context. They must not be attributed entirely to streaming.

Streaming CPU timing excludes harness assertions, array sorting, report construction and file output. Accelerated stress batches several simulated steps per rendered frame; an additional 240 real rendered frames per speed sample one streaming step per frame. Rendered-frame context includes Editor/Test Runner overhead and is not a standalone-player benchmark. Native allocation bytes and an IL2CPP/player profile are not measured.

The all-project-tests-pass completion condition remains open until both unfiltered suites pass with no failed, skipped, inconclusive or missing baseline cases. No source outside TL2 has been changed for these proposals.
