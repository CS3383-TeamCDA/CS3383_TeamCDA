# Turret Boss / Siege Core — isolated prototype

Open `Assets/Scenes/TL4_Tyler/TurretBoss/TurretBossSandbox.unity` and press Play.
This prototype is separate from the earlier ground-boss sandbox and FinalGame.

| Controls | Action |
| --- | --- |
| A / D | Move |
| Space | Jump while standing |
| S / Down | Crouch (actually shrinks collider and preserves feet position) |
| J | Debug hit for 50 damage; works only during exposed-core recovery |
| Escape | Pause/resume |
| R | Rebuild arena and reset fight/player |

The cyan rectangle is a local sandbox player using existing PlayerHealth. No TL6
movement or crouch scripts are edited. All warnings, projectiles, hit regions and
explosions are simple colored prototype geometry; the boss uses enlarged existing
turret art. It is not yet the rendered Siege Core artwork.

## First attack cycle

1. Floor port warning (1 second), then three low horizontal bolts: jump.
2. Alternate payload each cycle: first three grenades, next three vertical missiles,
   then repeat. Grenades mark the player's position, wait 0.8 seconds, lob for 1.2
   seconds and explode for 0.35 seconds. Move off the two-unit landing region.
   They do not track after the warning begins.
   Each missile launches vertically for 0.65 seconds, then displays a one-second
   landing warning. The yellow marker follows the player for the first 0.6 seconds,
   then turns red and locks for the last 0.4 seconds. It descends at that fixed X
   over 0.45 seconds and explodes in a 1.5-unit-wide region for 0.35 seconds.
   Missile descent is visual; damage comes from the ground explosion. All three
   missiles finish before beam charging starts.
3. Fixed-height cannon: 1.5-second line telegraph, then 0.7-second damaging beam.
   Its region spans y=0.95..1.55; crouched player's top is about 0.7 on flat ground.
4. 2.5-second recovery: armor opens (pink tint) and debug hits can damage the core.
5. Repeat; death clears active hazards and plays the existing turret death animation.

Bolts sweep their frame travel to detect hits. Each bolt, grenade explosion or
beam can damage the player once. Warnings do no damage. Clearing a track cancels its own damage; full encounter
cleanup cancels both body and launcher tracks. The cannon waits for payloads to
finish, while mobile stomp attacks may overlap launcher attacks. Pause
uses scaled time; restart destroys the old arena and hazards before creating new
ones. Victory/retry and HP are displayed in the sandbox HUD.

Boss prefab starts at 600 HP with +100 per additional level. Ranged damage starts
at 16 with +2 per level. Select the sandbox root before Play to change bossLevel.
The shared EnemyBase.TakeDamage entry point is now virtual so the turret can
reject closed-armor hits even when callers hold an EnemyBase reference. Ordinary
enemies retain the same implementation.

## Files and boundaries

- Scripts: `Assets/src/TL4_Tyler/TurretBoss/`
- Boss prefab: `Assets/prefabs/TL4_Tyler/TurretBoss/TurretBoss.prefab`
- Scene: `Assets/Scenes/TL4_Tyler/TurretBoss/TurretBossSandbox.unity`
- Tests: `Assets/tst/TL4_Tyler/PlayMode/TurretBossTests.cs`

This fight is not registered as IBossEncounter yet; the existing controller is
still typed to the earlier BossEnemy prototype. Final integration will need a TL4
encounter adapter to expose this fight without editing Caleb's implementation.
No scene/build/Composition changes outside TL4 were made.

## Mobile phase two (forced on for testing)

The first nonlethal exposed-core hit that brings health to 40% or below starts a
single two-second transformation. Hazards clear, damage is blocked during the
transition, the turret rises and four articulated placeholder legs unfold.
A lethal hit still kills directly instead of forcing an extra life/health gate.

Mobile loop: one-second marked lunge warning → fixed three-unit lunge over 0.6
seconds → 1.2-second leg-spin wind-up → low spin advancing two units over one
second → stomp warning and shock waves, with an independent three-shot launcher
barrage → wait for payload completion → planting → beam → exposed core. Jump over
the spinning legs; the taller lunge requires distance or a higher jump.

The launcher alternates missile and grenade barrages each mobile loop. Its own
clock, telegraphs, projectile visuals and hit tracking survive body-state changes.
A small rear module flashes/recoils independently of the leg animation. Production
launcher animations can subscribe to TurretBossLauncher.OnStageChanged without
replacing the body animation. The existing turret still supplies placeholder art.

Spin and cannon never run simultaneously. The cannon cannot start charging while
the launcher is active. Transformation, death, disable and sandbox restart cancel
both tracks. Floor bolts do not return after transformation. Each barrage resets
to three payloads. Cannon direction is selected when its state begins.

The boss body does not physically push/trap the player during mobile movement;
lunges use swept damage checks instead. Each lunge, spin and stomp attack hits at most
once. Movement stays within the sandbox bounds. Mobile motion uses a kinematic
body on the flat test arena, not terrain-aware navigation. Death retracts the
placeholder legs and restores a dynamic body so the wreckage can settle.

Next: balance phase two in the sandbox, replace placeholder geometry with Siege
Core art/leg animation, then add the seeded chance. Phase two is currently always
available rather than random; no FinalGame/Caleb registration was changed.

## Verification

Source and TL4 tests pass compile checks using Unity's cached compiler references.
Scene/prefab local IDs and references pass structural checks. New regression tests
cover attack ordering, core invulnerability/exposure, death cleanup, standing versus
crouched beam hits, alternating grenade/missile cycles, missile target lock and
dodge behavior, single-hit damage and hazard cancellation. Phase-two tests cover
threshold/transition immunity, bounded lunges, jumpable stomp damage, two full
mobile loops with completed barrages, planting before beam and no return to floor
attacks. Independent-track tests verify payload survival across body changes,
explicit cancellation, and jumpable single-hit spin damage. Runtime Test Runner
and visual playtesting remain to be run in Unity.

## Boss health overlay

The sandbox now binds an encounter-owned `TurretBossHUD` after `boss.Begin`.
A wide bottom overlay shows SIEGE CORE, HP, phase and armor/core status. Purple
health turns teal during exposed recovery; orange damage trails wait 0.45 seconds
then drain at 35% of the bar per second. Scaled time freezes the trail on pause.
The white 40% marker uses the controller's shared transformation threshold and
hides once phase two starts. Victory retains the empty bar; arena restart removes
and rebuilds the overlay. Existing upper-left text remains sandbox diagnostics.

For another TL4 encounter host, add the HUD to an encounter-owned object and call
`Bind(controller)` after `Begin`. No EventSystem or input interception is needed.
This is a runtime uGUI overlay; no changes to TL1 HUD or shared scenes are needed.
Compile checked against cached Unity assembly references; visual layout and runtime
behavior still require Play Mode verification in the turret sandbox.

## Movement and launcher tuning update

Beam facing is selected on charge entry and held through firing, including the
sprite's late-frame facing. Crossing behind the boss does not redirect the shot.
Mobile attack selection now charges only within 3.5 horizontal units (editable on
the controller), with a four-second charge cooldown. The warning is 0.45 seconds
and the three-unit dash takes 0.3 seconds. Otherwise the boss selects spin;
a nearby player can also trigger charge during spin wind-up. Active spin, beam,
transformation and core recovery are never interrupted by proximity charges.
Spin commits to the player's direction at wind-up and travels at 4.5 units/second
until reaching the arena edge (-8.4 / 3.3 body-center X) or four seconds. It does
not reverse when the player crosses behind it. These limits leave leg clearance
inside the sandbox walls. Movement bounds still belong to this flat sandbox.

The independent launcher module follows the boss including its raised height.
Grenades and missiles snapshot its launch position on firing, so payloads begin
at the current boss rather than the old hardcoded X and do not get dragged by
subsequent movement. Landing warnings retain their existing lock rules.
Updated regression coverage checks beam direction across a target crossing,
longer bounded spin travel, and fixed grenade launch origins after boss movement.
Source/test compilation passed; runtime tests and visual playtest remain pending.

## Short charge and variable phase-two barrages

Charge warning reduced to 0.25 seconds. Phase-two launcher bursts now choose 1–5
shots per barrage and sample a fresh 0.15–0.85-second gap after each explosion
before beginning the next payload. Landing warning/lock and flight durations stay
consistent. Phase one retains its three-shot introductory rhythm. Launcher stays
active during gaps, so the body still waits before planting/beam; cancel, pause,
and restart retain their existing behavior. Gap range is serialized on the
launcher. Its Initialize accepts an optional local RNG seed for repeatable tests;
world-seed integration remains future work. No global Unity random state is used.
Added seeded regression coverage for shot counts, varying gaps, completion and
cancellation. Source and test assemblies compile; runtime execution is pending.

## Reactive mobile combat (current behavior)

This replaces the earlier fixed mobile-loop description above. After transforming
or finishing a core opening, the boss assesses for 0.3 seconds. It turns with a
0.4-second leg tell if the player crossed behind its committed facing. At more
than five units it pursues with spin; within 3.5 units it prefers charge if its
cooldown is ready; otherwise it prefers stomp. At less than 1.5 units with charge
on cooldown it first takes a non-damaging step away. Viable close/mid-range openers
avoid immediate repeats. Phase one retains the introductory sequence.

Each mobile attack ends in 0.35 seconds of visible settling. Charge or spin may
extend into stomp if the player is close, or spin pursuit if distant (65% chance).
Chains cap at three attacks; stomp always finishes the chain. A spin ending near
the player may disengage with a non-damaging step. All follow-up attacks retain
their own wind-ups. The launcher can overlap spin (50% chance) or stomp and still
uses variable barrages. Payloads must finish before any core opening or beam.

The boss plants after a chain and usually exposes its core directly; at distance
it sometimes adds a beam instead. Recovery is 2.5 seconds plus 0.65 per committed
mobile attack, plus another 0.65 if the chain ends in stomp. It never cancels an
earned opening to react to proximity. Planted beam facing stays locked.

Placeholder motion now includes charge bracing/forward lean, spin vibration,
stomp lift/slam, post-attack settling and cannon recoil. These are procedural
prototype tells, not final animation assets. Controller Inspector fields tune
assessment, turning, follow-through, extension chance and recovery bonus.
`Begin` accepts an optional seed for repeatable local decisions and barrages;
production world-seed wiring is still a separate integration task.

Decision and mobile-loop regression coverage was updated for distance/cooldown,
repeat avoidance, follow-through, bounded chains, launcher/beam exclusion and
longer guaranteed recovery. Source and test assemblies compile; Unity runtime
execution and visual balancing remain pending.

## Combat sandbox now uses the modular Siege Core rig

`TurretBossSandbox.unity` now references `SiegeCoreVisual.prefab` and creates a
TL4 `TurretBossCombatVisuals` adapter. Open the COMBAT sandbox to fight the new art;
`TurretBossArtPreview` remains the independent art-review scene. The old sprite,
Animator, procedural stick legs and launcher block are hidden only when the new
visual binding is present. A sandbox without a visual prefab retains legacy art.

The adapter follows controller X/facing on a separate grounded visual root so
physics-root lift/lean does not double-transform the art. It triggers deployment
on transformation, parks the solid base, slides shutters on core exposure, drives
cannon charge/fire directly from combat elapsed time and responds independently
to real launcher shot events. Beam geometry begins at the rig muzzle; payloads
snapshot the launcher muzzle. Existing warning regions, beam height, damage,
attack selection and timings remain controller-owned. Mobile cannon posture
lowers to the crouchable beam height without returning to the abandoned base.

The rig is half scale in combat. Root movement above the slow gait's supported
speed uses grounded stance placement rather than overextending legs. Dedicated
charge/spin/stomp animation is still next work. Death cancels weapon motion and
holds a wreck pose; a proper death animation is still pending. Restart destroys
both controller and visual roots under the arena and unsubscribes launcher hooks.

Source and regression-test compilation and scene-reference checks pass. New
integration coverage exercises hidden legacy art, launcher triggers, cannon
recoil, core exposure and transformation. Unity runtime execution and combat
visual review are still pending; do not equate compilation with passing runtime
tests. No FinalGame/Composition or other teams' files were changed.

## Combat locomotion and attack poses

Removed the high-speed standing-pose fallback. Gait period now adapts to actual
root velocity (including the combat rig's half scale). Slow movement retains the
one-foot crawl; above 1.5 local units/second diagonal pairs alternate with two
support feet. Swing durations are stored per step and shorten on acceleration.
Resting feet remain in world space; reversal and stopping continue through their
current step. Numerical move/reverse/stop samples at 30/60/120 Hz stay within
joint reach.

The combat adapter now synchronizes explicit procedural poses to controller state
and elapsed time: charge warning braces low, charge tucks/trails the legs and
leans forward, spin warning crouches/coils, active spin sweeps four articulated
legs in a low rotating pattern, and stomp rises then compresses sharply before
settling. Attack exit resets planted anchors for the next stance/gait. Independent
launcher and cannon clocks continue without being replaced by leg motion. Existing
attack durations, damage, arena limits and core-opening rules are unchanged.

Use TurretBossSandbox to review these combat animations. ArtPreview still provides
the slow walk/weapon/transformation review. Death is still a held wreck pose.
Added regression checks for combat-speed diagonal stepping and charge/spin/stomp
poses. Source/test compilation passes; runtime tests and visual balancing remain
pending. This supersedes the documented high-speed stance fallback.

## Hit, damaged-state and death presentation

Accepted HP loss now emits TL4 DamageTaken feedback. Closed armor, zero damage
and rejected hits do not trigger it. The rig uses a short warm-white flash,
small chassis jolt and pooled sparks, without interrupting the attack timeline.
Armor darkens below 70% HP; below 40% it intermittently sparks and red energy
flickers. This uses a TL4 unlit sprite shader/material referenced by the visual
prefab, with per-renderer property blocks; no source sprites are modified.

Death cancels weapon animation, flares then darkens the uncaged reactor, opens
its shutters, droops cannon/launcher and buckles the mobile legs outward with
feet drawing inward into a low wreck pose. The planted form slumps on its base.
The sequence settles in roughly 1.6 seconds and leaves dark wreckage behind.
The adapter stops following the dead physics body and removes its damage/launcher
subscriptions. All timers use scaled time, so pause freezes the effects. Restart
removes the visual/effect pool with the arena. No gameplay hit-stun or damage
changes were added. The accepted low leg-sweep charge is unchanged.

ArtPreview: H simulates a hit (repeated hits increase persistent wear); K defeats
the right-hand assembly. Restart Play Mode to reset. Combat sandbox J remains
an exposed-core-only hit.

Source and regression-test compilation pass; material/shader asset references
and numerical collapse reach were checked. Regression additions cover rejected
versus accepted hits, lethal transition, paused death, weapon suppression and
persistent wreckage. Unity runtime tests and shader/visual review remain pending:
UI inspection succeeded, but editor controls disconnected before a restart.

## Animated attack effects

Combat sandbox now references SiegeCoreAttackVfx.asset and its transparent
32-frame sprite atlas: 8 beam frames, 4 rotating grenade frames, 4 rocket/exhaust
frames, 8 explosion/smoke frames and 8 ground shockwave frames. Beam plasma tiles
across the shot length to avoid stretching its pixels; the red charging line stays
a separate warning. Low bolts also use the plasma frames. Rockets flip for
descent, grenades animate during the existing lob, and stomp waves play mirrored
left/right sequences. Frame changes use the attack field's scaled elapsed time.

Explosions now spawn a 0.65-second visual-only aftermath clip; damage still ends
with the existing 0.35-second blast state. Normal state changes retain aftermath;
explicit field cancellation, death and arena cleanup remove it. Launcher fields
inherit the same VFX set. Existing warning colors/target locks and damage geometry
remain unchanged. Missing VFX assets fall back to the original hazard graphics.

Sprites are in Art/SiegeCoreAttackVfx.png with slice coordinates in the matching
JSON. These are first-pass generated effect frames and may need art/timing polish.
Source/tests compile, all 32 slice bounds and scene references validate. Added
regression coverage for frame advancement, pause, shared launcher configuration,
non-damaging aftermath and explicit cleanup. Runtime tests and visual gameplay
review remain pending. Open TurretBossSandbox to review the animated attacks.

## Charge selection after pursuit

The quick charge was previously considered only when choosing a new combo opener.
After a low leg-sweep pursuit ended close to the player, follow-up selection could
choose only stomp/sweep and never re-evaluated charge. It now rechecks the existing
3.5-unit proximity range after pursuit follow-through and selects charge if ready,
within the three-attack combo cap. The 0.25-second tell, 0.3-second dash, four-second
cooldown, and guaranteed eventual recovery remain unchanged. Source/test compile
checks pass; runtime playtest remains pending. Regression coverage checks that
this follow-up reaches the actual dash and honors cooldown/combo limits.

## Actual body/foot VFX anchors

Phase-one bolts no longer use the fixed X=5.5 spawn. A visible small firing port
is assembled on the front of the planted base, and its FloorMuzzle supplies both
shot origin and floor-warning X. Bolts are drawn immediately at that muzzle on
spawn, then retain their sampled height and firing direction with swept damage.
Fallback scenes without modular art derive the origin from boss position/facing.

Charge placement now samples the outermost ankle/toe on the travel side from the
actual modular rig. The burst's trailing edge meets that leading foot and its
crest extends forward. LateUpdate runs after leg presentation to refresh this
anchor, eliminating the old fixed-center offset and one-frame foot mismatch.
The end burst uses the same sampled position. No AI/attack durations changed.

Source and tests compile. Added assertions for immediate low-port spawn/movement
and leading-foot anchors. Visual placement still requires gameplay review.


## Player collision pass (November 11)

The sandbox installs `TurretBossBodyCollision`: a grounded 1.8 × 2.1 solid
kinematic body with zero friction. Phase two no longer ignores the player.
Boss movement clamps at player contact so transform-driven charges cannot cross
or crush through the player. Cosmetic lift/lean does not move the body off the
floor; defeat disables its collision. Sandbox jump/headroom checks recognize
the boss as solid support.

All attack-field hit checks now query the player's actual non-trigger collider
shapes, including child/compound colliders, with immediate transform/size sync.
Existing bolt, stomp and charge sweeps and single-hit gates remain in use.
Telegraphs and airborne payloads remain harmless; grenade/missile blasts deal
damage on landing. Disabling the field clears hazards. The right sandbox wall
moves past the initial boss, and beam/payload coverage includes that arena area.
Fight timings and balance are unchanged.

Added `TurretBossCollisionTests` covering warning safety, child hurtboxes,
crouching, trigger exclusion, blasts, fast bolts, cancellation and solid-body
blocking. Source and tests compile with Unity Roslyn; `git diff --check` passes.
Runtime verification remains pending: the isolated editor failed AssetDatabase/
UDS initialization, and desktop automation could read Unity but could not click
its window. Run this test class in the Unity PlayMode Test Runner and replay the
sandbox before treating physical behavior as verified.


## Phase-two damage regression fix

The turret prefab was RangedOnly, so EnemyBase.MeleeDamage returned zero for
both charges and stomp. The prefab and Begin now use Both. Renamed Spinning /
SpinWarning to LongCharge / LongChargeWarning, and Lunging / LungeWarning to
ShortCharge / ShortChargeWarning, retaining enum numeric order and timings.
Phase-two low bolts now spawn at the current boss X at ground height instead of
the stationary platform muzzle left behind during transformation. Phase one
continues to use its art muzzle. Added regressions for nonzero charge contact
and mobile floor-bolt damage. Source/test compilation passes; runtime execution
is still pending because the Unity runner could not initialize in this session.


## First fight-rhythm tuning pass

Mobile openers use seeded distance-based weights instead of deterministic
alternation. Previous openers receive one quarter weight; distant targets get
pursuit, while close targets permit short charge/stomp. Short charge retains
its 0.25-second warning and cooldown. Chains cap at two body attacks; the
long-to-short extension now rolls its chance rather than always extending.
Very close targets suppress generic extensions. Follow-through is 0.45 seconds.
Long charge warning/duration are 0.7/2.8 seconds. Base core recovery is 1.6
seconds plus 0.4 per committed attack and another 0.4 after stomp. Phase-one
floor warning is 0.7 seconds, volley exit 2 seconds, beam warning 1.1 seconds.
Launcher 1–5 shot variation is unchanged. Source compilation and diff checks
pass; feel and existing behavior tests still require Unity PlayMode validation.


## Rapid phase-two barrage and charge contact

Phase-two barrages keep 1–5 payloads but schedule starts every 0.3–0.4 seconds.
Each payload owns an independent warning/flight/blast track, so launching no
longer waits for the previous explosion. Parent stays active until every track
finishes; cancellation clears all tracks. Phase-one serial volleys are unchanged.
Long charge movement is multiplied by 1.3 and exits into follow-through on its
first player hit. The field exposes its existing single-hit latch to the body
controller. Source/test compilation passes; gameplay verification is pending.


## Shared boss HUD standard

`BossFightHUD` supplies the shared bottom-right neon-magenta HUD with a framed
portrait, boss name, diagonal health segments, delayed damage trail and status.
Bind with `Bind(EnemyBase boss, string displayName, Sprite portrait,
Func<string> statusText = null)`. The encounter owns and destroys the HUD.
`TurretBossHUD` is the Siege Core adapter; the sandbox supplies its dome sprite
as the headshot. Future boss encounters can use the shared component and supply
their own portrait/status. No shared player UI or FinalGame scene was modified.
Unity source compilation and diff checks pass; visual review in Play Mode is pending.
