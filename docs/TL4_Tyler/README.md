# TL4 enemies and combat

## Shared integration

The public enemy API lives in
[Contracts/TL4_Tyler](../../Assets/src/Contracts/TL4_Tyler/README.md).
`EnemySpawner` implements `IEnemySpawner`; Composition registers the scene's
spawner and adds `EnemyGameStateBinding` before dependency injection/activation.
The initial contract migration did not change scene wiring. The subsequent
[final-scene integration setup](FINAL_SCENE_INTEGRATION.md) adds a candidate
FinalGame scene and TL4 source prefabs.

Other teams should receive `IEnemySpawner` through `IDependent<IEnemySpawner>`
rather than referencing TL4Assembly. Subscribe to its defeat event to receive
`EnemyDefeatedInfo` with type, level, score, coin reward, and engine-independent
position. EnemyType is now in EscapeThe90s.Contracts; serialized enum values
remain Ground=0, Flying=1, Turret=2, Aim=3.

Score defaults to zero and is configured independently on the enemy prefab;
existing level-scaled coin values are unchanged. TL1/TL5 should agree score values
and migrate their consumers. Legacy callbacks remain for existing consumers;
never award from both the legacy and contract events.

Example consumer (assembly references Contracts and Infrastructure.Unity):

```csharp
using EscapeThe90s.Contracts;
using EscapeThe90s.Infrastructure.Unity;

public sealed class EnemyScoreListener : DependencyBehaviour, IDependent<IEnemySpawner>
{
    private IEnemySpawner enemies;
    public int Score { get; private set; }
    public void Inject(IEnemySpawner dependency) { enemies = dependency; }
    protected override void OnEnabledAfterInitialization()
    {
        enemies.OnEnemyDefeated += HandleDefeat;
    }
    protected override void OnDisabledAfterInitialization()
    {
        enemies.OnEnemyDefeated -= HandleDefeat;
    }
    private void HandleDefeat(EnemyDefeatedInfo info) { Score += info.ScoreValue; }
}
```

The state binding permits automatic spawning only during Playing. It leaves the
spawn rate intact on pause/resume and clears accumulated spawn time when stopped.
Manual factory calls remain available for demos/tests. TL1 freezes existing
movement/projectiles using scaled Unity time; restart recreates scene objects.
Standalone TL4 scenes do not need game-state services unless the binding is added.

## Remaining dependencies

EnemyBase receives TL3's IPowerUpSpawner through IDependent injection; EnemySpawner
injects runtime-created enemies through the scene DependencyScope. TL4 no longer
references Joe.Game. Standalone demos must register IPowerUpSpawner and inject
enemies if they need drops; there is no manager-search fallback.

TL6's player-health contract is still pending. Melee/projectile damage retains its
existing concrete PlayerHealth calls and TL6Assembly reference.

## Verification

- Existing TL4 EditMode tests remain, with EnemyType imports updated.
- TL4ContractTests covers one defeat notification, independent score/coin values,
  position and level payloads, and no notification for living despawns.
- TL4.Contracts.PlayMode covers real DependencyScope injection, late subscription,
  pause/resume without spawn debt, unsubscribe on disable, menu and game over.
- Compiler verification using Unity's cached response files passes for Contracts,
  Chris.Game, TL4Assembly, Composition, TL4Tyler_Tests, and TL4.Contracts.PlayMode.
  This is a compile check, not a fresh Unity build or test execution.
- Batch tests could not execute: the working project is open in another Unity
  instance; an isolated copy was blocked by sandbox socket permissions in Unity
  Package Manager. Run EditMode and PlayMode in Unity Test Runner before declaring
  the suite green and moving to final-scene assembly.

## Enemy bodies and terrain

Defeated enemies are visual-only corpses: EnemyBase disables every child
Collider2D (solid and trigger, including inactive children) and stops Rigidbody2D
simulation before defeat callbacks. Corpse sprites/animations remain visible;
players and enemies cannot stand on, block against, or stack on dead bodies.
Flying corpses also stop in place rather than falling through the terrain.

The Level1_Turret prefab now has a dynamic Rigidbody2D with horizontal motion and
rotation frozen, plus a solid base collider. Gravity settles its approximate
spawn height onto terrain instead of leaving it suspended over gaps.

## Boss prototype

See [Boss encounter setup and controls](BOSS_ENCOUNTER.md). The standalone scene is
`Assets/Scenes/TL4_Tyler/BossSandbox.unity`; final-scene registration remains a
separate integration-owner task.

## TV-claw and grenade berserker prototypes

`EnemyType` appends TVClaw=4 and GrenadeBerserker=5; existing serialized values
remain unchanged. Both animated prefabs and typed spawn markers live alongside
Level1_Ground. EnemySpawningMvp is now based on the current GeneratedWorldGameDemo
composition, replacing its obsolete generator-less TL2 setup. Both scenes use
WorldNetworkDemo and GeneratedStreetContent with terrain, player, camera, menu,
HUD, click combat and the same five-enemy rotation. Their active enemyPrefabsByKind
array supplies Ground, Flying, Turret, null Aim, TVClaw, and GrenadeBerserker.
The street assignment cycles configured non-null slots. Both world-demo setup
menus preserve that configuration when regenerating scenes. The disabled legacy
GeneratedTerrainEnemyPopulation is not the spawning path in either scene.

TVClaw uses the shared timed melee/pursuit system: 300 base HP, 30 melee damage,
4.5 speed, enlarged green TV-head placeholder. Its deathclaw-like silhouette and
claw animation still need dedicated art; the prototype reuses the early enemy.

GrenadeBerserker has 65 base HP and holds position while throwing every 2 seconds.
Grenades fly toward the player's launch-time position over 1.2 seconds and explode
with radius 2, using level-scaled ranged damage. Any positive damage or a player
within 4 units permanently triggers its speed-6 rush. Contact within 1.1 units
kills the berserker. Every combat death, including a one-shot kill, emits exactly
one radius-2.8 blast at twice the larger of melee/ranged damage. Terrain blocks
blast damage; multiple player colliders cannot multiply a blast. Streaming unload
and scene cleanup are despawns and do not explode. Grenades survive owner death.
The orange TV-head body remains placeholder art. Grenades and explosions reuse
SiegeCoreAttackVfx and TurretBossVfxBurst from the boss. The ballistic fuse remains
independent of the boss arena attack-state machine. Death blasts also damage nearby
enemies once per blast and can chain other berserkers; thrown grenades retain
player-only damage. Raging berserkers ignore enemy collisions and shared crowd
slowdown, including enemies spawned after rage begins; terrain/player collision
is retained.

Validation: Contracts and TL4Assembly compile with Unity 6000.6 cached compiler
references. Playtest these two scenes for jump/contact behavior, blast readability,
and cadence before accepting tuning; no fresh Unity Test Runner run is claimed.

New-enemy regression tests: `Assets/tst/TL4_Tyler/Tests/NewEnemyTests.cs` in
`TL4Tyler_Tests`. In Window > General > Test Runner, select PlayMode and filter
`NewEnemyTests`. Covers prefab/marker wiring, melee-only identity and scaling,
zero/negative damage, permanent damage/proximity rage, grenade suppression during
rage, contact death, one-shot/reentrant death and reward deduplication, multi-hurtbox
blast deduplication, terrain/radius safety, grenade fuse/owner-death survival,
living despawn safety, and both factory types. Tests compile; batch execution was
blocked because the project was already open in Unity. Execution results pending.

Scene regression coverage: `EnemySceneIntegrationTests` checks the sandbox's active
HUDController (required to hide the menu on Start), active generated-demo enemy
prefabs, sparse enemy-kind cycling, and the actual Start button callback through
Menu -> Playing -> hidden menu. These tests and the modified WorldGen Integration,
Demo, and Demo.Editor assemblies pass compiler verification. Unity Test Runner
execution remains pending. The sandbox now instantiates the shared HUD prefab.

The sandbox scene regression also requires an active WorldNetworkDemo and assigned
settings, art, player prefab and StreetView, to catch a menu-only scene with no
world generator. Scene identity/meta GUID is retained when refreshing its contents.

Additional NewEnemyTests cover rush collision bypass/new arrivals/crowd slowdown,
friendly death-blast damage with multiple colliders, shared animated explosion
frames, and chained berserker deaths with one defeat event per enemy.

Explosion presentation: terrain within 0.85 units below the blast selects the
bottom-anchored Siege Core ground animation. Airborne blasts use a separate
radial pixel fire/smoke animation at the actual detonation point. Player-contact
suicide blasts always use the radial animation, even near terrain. No distant
floor snapping; damage stays centered at the detonation. Tests cover nearby vs
distant terrain and forced contact bursts. Compiler checks pass; runtime pending.

TV Claw combat now owns its sequence instead of generic attack animations:
close bite (1.1 range, .25s windup, half melee damage), heavy swipe (2.2 range,
.65s windup, full melee damage and stronger knockback). Both have .8s recovery.
A grounded player target lingering at 2.8–7 units for .7s triggers a .55s pounce
windup, direction locked immediately, then speed 10 / vertical impulse 4 for up
to .55s. Hit or wall contact stops the pounce; misses leave the same recovery.
Pounce cooldown is 3.5s from launch. Ordinary damage does not cancel windups.
An amber tint is the temporary pounce tell; quadruped running, crouch, claw and
bite animations remain art work. Tests cover attack timing/damage, escaped
strikes, windup persistence, locked launch direction, miss recovery and cooldown.

## TV Claw animation integration

The approved animation atlas is imported at `Enemy_Sprites/TVClaw/TVClawAtlas.png`
with 56 named sprite slices (8 frames per idle/run/swipe/bite/pounce/hit/death row).
The source is 1340x1174 rather than the requested exact grid; row boundaries are
measured and sprite pivots use common foot baselines. Pixel filtering, no texture
compression and 80 pixels/unit retain the pixel style. The original generated PNG
is retained separately. Generated frame consistency may still need manual polish.

`TVClawArt.asset` supplies the seven sequences to `TVClawArtDriver` on the existing
TVClaw prefab. Both demos therefore use the new art without new scene overrides.
The placeholder Animator is disabled; facing remains controlled by GroundEnemy.
Run/idle follow velocity; combat frames follow TVClawEnemy's actual windup and
recovery elapsed time. Pounce has separate crouch, launch and landing frame ranges.
Nonlethal hit reactions play only during pursuit and do not interrupt committed
attacks. Death holds its final frame. The old green tint and amber placeholder
windup flash are removed for this animated prefab.

TVClawArtTests checks sequence references, disabled placeholder animation, bite
windup/recovery selection and persistent corpse frames. C# compiler verification
passes; Unity Test Runner and in-game visual verification remain pending.

TV Claw atlas slices are horizontally trimmed to the main connected character
silhouette, excluding detached claw fragments spilling from adjacent frames.
Pivots compensate the cropped left edge to preserve each frame's world alignment.
TV Claw ignores collisions with EnemyBase bodies (living or defeated), including
new arrivals, and disables crowd-following speed clamping. Player/terrain contacts
remain enabled. TVClawArtTests covers traversal through living enemies and corpses.

Bite reach now uses a scale-aware local mouth offset (.8, .65) and .65-world-unit
reach to the nearest enabled player collider rather than root-to-root distance.
The same check selects the bite and validates damage after windup, mirrored for
left-facing attacks. Vertical separation, escaping during windup, and terrain LOS
still prevent hits. Tests cover a normal standing player at 2 units in either
direction without body overlap, escape, vertical separation and wall blocking.

Grenadier throws are capped at 12 world units (maxThrowRange on the prefab).
Retained aggro does not bypass the limit. It holds position outside throw range
and resumes normal-cadence throwing when the target returns; damage/proximity
rage and already-launched grenades are unchanged. Regression covers retained
aggro, no out-of-range throws, boundary inclusion and resuming in range.

Boss grenade/missile explosion aftermath now uses the ground anchor and measured
transparent-bottom inset, matching grounded grenadier blasts. Tests cover corpse
collision removal before callbacks, no corpse drift, and visible ground anchoring
for both boss payloads. Compiler checks pass; Unity test execution pending.
