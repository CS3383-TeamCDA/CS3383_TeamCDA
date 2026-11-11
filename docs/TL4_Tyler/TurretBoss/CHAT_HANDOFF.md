# Turret Boss continuation handoff

## User's current goal

Continue the Escape the 90s turret/tank boss prototype, named Siege Core.
The user approved its basic combat and avoidance after personally playtesting.
Most recent feedback: the base idea is right; animations, attack variability and
timing still need work; add a proper boss-fight health bar.
Suggested next work was a boss HUD plus an editable tuning asset, then controlled
seeded variation and production animations. These suggestions have not been
implemented. User asked to transfer this work into a new chat, not to start coding
those next features during the transfer.

## Editing boundary

Only modify TL4-owned files. User explicitly forbids editing other teams' code.
Relevant areas: Assets/src/TL4_Tyler, Assets/prefabs/TL4_Tyler,
Assets/Scenes/TL4_Tyler, Assets/tst/TL4_Tyler, docs/TL4_Tyler and TL4's own shared
contracts in Assets/src/Contracts/TL4_Tyler. Leave Composition, FinalGame,
ProjectSettings and TL1/TL2/TL3/TL5/TL6 unchanged. Shared registration/integration
changes are handoff tasks for the relevant owner. Preserve existing changes;
check git status before editing. Do not push or send team messages unasked.

## Current playable scene

Assets/Scenes/TL4_Tyler/TurretBoss/TurretBossSandbox.unity

This is separate from Assets/Scenes/TL4_Tyler/BossSandbox.unity, the older generic
ground-boss prototype. Do not confuse them or replace one with the other.
The turret sandbox creates its camera, flat arena and cyan player at runtime.
Outside Play Mode, 'No cameras rendering' is normal. Use the editor's Play button,
not Test Runner's Player tab, to play it.

Controls: A/D move, Space jump, S/Down crouch, J debug hit (50 damage only when
core exposed), Escape pause, R restart. Crouching actually changes the dummy's
collider; no TL6 movement changes were made. It uses existing TL6 PlayerHealth.

## Design and implemented behavior

User chose concept #1 Siege Core: planted purple/teal turret with red optic and
heavy cannon. Potential phase two grows legs inspired by concept #3. Current
visuals are enlarged standard turret sprites, procedural leg segments, simple
hazard geometry, and debug GUI, not final generated boss artwork.

Phase one: low horizontal floor bolts (jump), alternating grenade or vertical
missile barrage (move off landing markers), charged horizontal beam (crouch),
then exposed-core recovery. Grenades have fixed marked landing spots and visible
lobs. Missile marks track briefly yellow, lock red before descent, then explode.
Warnings do no damage; missile damage is the ground explosion, not airborne contact.

At a nonlethal hit bringing HP to 40% or below: clear hazards, block damage during
a two-second transformation, lift body, unfold four placeholder legs. Phase two
is forced on for development; seeded probability is NOT implemented. A lethal
hit kills directly rather than forcing a health gate.

Phase two: marked lunge, short advancing low leg spin (jump), stomp waves (jump)
with independent launcher barrage, wait for payload completion, plant/lower,
charged beam, exposed core; repeat. Launcher alternates missiles/grenades across
mobile loops. No floor bolts return. Body does not physically trap/push player;
swept damage checks handle lunge/spin. Spin and beam never overlap. Beam waits
for launcher completion. Pause freezes scaled timers. Death/disable/restart
cancel both tracks.

Launcher is a separate clock/attack-field instance. Body-state changes clear only
the body field, preserving in-flight payload. Both tracks have independent warning
visuals and hit flags. TurretBossLauncher.OnStageChanged is an animation hook;
a small launcher module already flashes/recoils separately from legs. Final body,
cannon and launcher artwork/Animator layering still need production work.

## Files

- Assets/src/TL4_Tyler/TurretBoss/TurretBossController.cs — phases/body coordinator
- Assets/src/TL4_Tyler/TurretBoss/TurretBossAttackField.cs — hazards, telegraphs, hit checks
- Assets/src/TL4_Tyler/TurretBoss/TurretBossLauncher.cs — independent grenade/missile track
- Assets/src/TL4_Tyler/TurretBoss/TurretBossLegRig.cs — procedural temporary legs/spin
- Assets/src/TL4_Tyler/TurretBoss/TurretBossSandbox.cs — arena, dummy controls, debug HUD
- Assets/prefabs/TL4_Tyler/TurretBoss/TurretBoss.prefab — scaled turret prototype
- Assets/tst/TL4_Tyler/PlayMode/TurretBossTests.cs — regression tests
- docs/TL4_Tyler/TurretBoss/README.md — detailed behavior/setup

EnemyBase.TakeDamage was made virtual to allow the boss's closed-armor gate to
work through EnemyBase references. Ordinary enemy behavior is unchanged.

## Integration status

Existing IBossEncounter and BossEncounterController in TL4 belong to the earlier
BossEnemy ground prototype. They are NOT wired to this turret fight. No turret
encounter registration exists in Composition/FinalGame yet. A future TL4 adapter
must expose the turret via the agreed encounter contract; coordinate foreign-file
changes rather than editing others' files. Arena bounds and several timings are
currently hardcoded for the sandbox.

## Verification and known pitfalls

User has visually playtested the mechanics and approved the general approach.
Compiler checks passed, including the TL4 PlayMode assembly with its actual
references. Automated runtime tests have NOT been confirmed green. Do not equate
compilation with passing tests. Prior CLI Unity runs failed because the working
project was open and isolated Unity Package Manager hit sandbox socket restrictions.

TL4.Contracts.PlayMode.asmdef now explicitly references TL6Assembly because tests
use PlayerHealth; an earlier compiler check accidentally supplied extra references
and missed this. It uses optional TestAssemblies, so do not also add duplicate
UnityEngine.TestRunner references. BossEnemy's field was renamed bossAttacks to
avoid Unity's inherited duplicate-field error with GroundEnemy.attacks.

Other teams had unused UnityEditor imports blocking standalone Player builds.
Our edits to their files were reverted at the user's direction. Report foreign
failures; do not fix them. Use Test Runner EditMode/PlayMode for editor runs.

## Next proposed deliverables

1. Proper boss HUD: SIEGE CORE name, wide health fill, delayed damage trail,
   40% transformation marker, optional phase label; keep debug text separate.
2. One TL4 ScriptableObject tuning asset for timings, damage, movement, thresholds
   and overlap rules rather than scattered hardcoded values.
3. Seeded selection among approved patterns with repeat limits and cooldowns.
   Preserve fair dodge windows and beam/spin exclusion.
4. Final turret/leg transformation art and separate animation tracks after tuning.

Read current source before implementing; this handoff records intent/status and
may become stale after later edits. The repository files are authoritative.

## Continuation update — boss HUD

Implemented `TurretBossHUD.cs` and sandbox binding: bottom health overlay, delayed
orange damage trail, phase/armor status, shared 40% marker, and victory state.
Controller now exposes `MobilePhaseThreshold` so HUD and combat share the value.
Compilation passed with cached Unity references (existing DeleteExeAttack obsolete
API warnings only). Unity visual/runtime verification is still pending. Editable
tuning asset, seeded variation and production art remain unimplemented.

## Continuation update — requested combat changes

Beam direction now stays locked from charge through firing. Charge uses 3.5-unit
proximity selection, 0.45-second warning, 0.3-second dash and four-second cooldown.
Spin commits in one direction at 4.5 units/second until the arena edge or four
seconds; proximity can switch spin warning to charge but never interrupt firing
or active spin. Launcher follows current body position; projectiles snapshot the
launch origin instead of using the grenade's old hardcoded offscreen position.
Regression tests updated; source/test compilation verified, runtime still pending.

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

## Continuation update — reactive mobile combat

Implemented situational mobile openings, turn/reposition tells, capped branching
combos, follow-through/disengage, and heavier-chain recovery bonuses. Spin may now
also launch a barrage. Beam is a situational ranged finisher, not mandatory each
cycle. All launcher payloads finish before beam or core exposure. Full current
behavior is documented in README's "Reactive mobile combat" section, superseding
the old fixed-loop descriptions. Added procedural lean/brace/lift/slam/recoil
motion. Begin accepts an optional RNG seed; no world-seed integration yet.
Updated regression tests; source/test compilation passes. Unity runtime and visual
verification remain pending. Only TL4-owned files were modified.

## Continuation update — modular art preparation

Approved base and modular artwork generated. User requested a bare exposed reactor
without cage; corrected transparent atlas includes it. Leg pieces corrected to
separate thigh/shin links. Added named slices/custom pivots, reusable runtime visual
rig prefab, and isolated TurretBossArtPreview scene (Space shutters, F mirror).
See ART_ASSEMBLY.md for file paths and validation limits. Combat visuals remain
unchanged. Production animation and gameplay binding are still future work.

## Continuation update — idle and walking

User approved the revised bent-knee standing pose. Added procedural idle weight
shift, upper assembly lag and a one-foot-at-a-time world-anchored crawl to the
visual rig. Art preview A/D or arrows now translates the mobile form; Space
shutters and F mirroring remain. Feet finish their current step on release.
See ART_ASSEMBLY.md. Source/tests compile and numerical reach check passes;
Unity runtime tests and visual playtest remain pending. Combat art binding and
transform/attack/death animations remain future work.

## Reversible transformation preview

T now toggles the right-hand preview boss between planted and mobile forms over
two seconds. The chassis braces/lifts, legs unfold in a staggered sequence, and
the existing base skirt retracts/scales/fades into the body. This is procedural
presentation with the current art, not dedicated hatch sprites. Pressing T again
reverses continuously from the current deployment. Movement is blocked in the
preview during transition or while planted. Completion resets ground anchors
for seamless idle/crawl handoff. Space shutters and F mirror remain available.

Regression checks cover scaled-time freeze, mid-transition reversal, endpoint
visibility and planted feet after completion. Source/test compilation and numeric
reach checks pass; Unity runtime execution and visual review remain pending.
Combat visuals and phase timing are unchanged; transformation combat binding
and attack/death presentation remain future work.

## Physical docking-base revision (current transformation)

Replaced the skirt shrink/fade with a solid persistent docking base. Legs unfold
first and plant outside its rim, then push the body upward. The base and legs
remain opaque. The front rim occludes tucked machinery instead of using alpha.
The base stays at its original world position when the boss walks away. Press T
again to walk back to the base before folding down; reversing a transition still
continues from its current pose. Right preview now starts planted so the first T
shows deployment. Existing direct SetForm(true) can still initialize a mobile
assembly without an abandoned base; animated deployment parks the physical base.

Source/test compilation and sampled joint reach checks pass. Regression coverage
now expects the base to persist, remain opaque and stay behind during walking,
with docking before retraction. Unity runtime/visual verification remains pending.
This supersedes earlier notes describing disappearing/retracting skirt art.

## Weapon and core presentation preview

B previews the cannon's 1.5-second charge and 0.7-second firing/recoil cycle on
both assemblies. A small energy glow uses the existing bare-core sprite at the
muzzle; this is presentation, not a damaging beam or final VFX. L independently
previews launcher bracing, kick and settling. Both weapons can animate together
while idle, walking or transforming without taking over the leg track. Repeated
triggers on a busy weapon are ignored. Space now slides the two core shutters
over 0.3 seconds and can reverse mid-slide. SetExposed remains an immediate
setup API; AnimateCore is the interpolated presentation API. No combat damage,
projectiles or shared integration was changed.

Updated regression coverage checks concurrent clocks, restart suppression, pause,
recoil restoration and reversible shutters. Source and test compilation pass;
Unity runtime tests and visual review are still pending.

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

## Spin silhouette correction

Removed the full horizontal hip/foot orbit that crossed fixed-facing leg sprites
through the chassis. Spin now keeps all four hips in their original sockets and
each knee/foot on its own side. Near and far pairs sweep low with a small opposing
phase and a short extension blend; existing rear/front rendering order is retained.
This is a readable side-view sweep with the current sprites, not a full 3D leg
rotation. Combat timing, movement and damage are unchanged.

Compilation passes. Numeric samples verify reach and side retention throughout
four seconds, with matching regression assertions added. Runtime/visual review
in TurretBossSandbox remains pending.

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

## Progressive damage revision

Persistent wear now begins with the first HP loss and accumulates continuously.
The shader adds stable pixel-space scratches, exposed-metal chips and scorched
patches as wear increases, without changing the silhouette or moving the pattern
over time. Red energy instability ramps with damage rather than using one fixed
critical-health blink. Below 75% HP occasional sparks begin, below 50% bursts grow
and become more frequent, and below 25% larger frequent bursts indicate critical
condition. Hit feedback and the existing death sequence remain intact. Repeated
H presses in ArtPreview step through health levels for visual review.

C# and test compilation pass. Added material-property regression checks that
wear increases across successive health levels and resets at full health. Shader
appearance and runtime execution still require Unity Play Mode review.

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

## Continuous beam correction

Replaced tiled complete beam sprites (which looked like a chain of bolts) with
one full-length strip using SiegeCoreBeam.mat / SiegeCoreBeam.shader. The shader
keeps an uninterrupted opaque pale core with cyan/magenta filaments and streaks
flowing in the firing direction. Explicit scaled attack elapsed time drives flow
so pause freezes it. Charge still uses the thin red warning line; floor bolts
retain their individual sprite animation. Beam damage/width/duration are unchanged.
The shader material is referenced by the VFX asset for build inclusion.

Source/test compilation passes. Regression checks cover one-strip rendering,
flow direction/time, pause and restoration of warning material. Unity shader
rendering and gameplay appearance still need visual review. This supersedes
the earlier tiled-beam description.

## Original-art continuous beam revision

User requested the original cyan/magenta animated pixel-art beam, simply unbroken.
Removed the replacement procedural plasma appearance. The continuous strip now
samples the existing eight beam animation frames and scrolls only their middle
40% along its length, omitting individual projectile caps and transparent end
padding. This preserves original colors, irregular silhouette and frame animation
without repeating separated bolts. Direction and scaled-time pause are retained.
Source/test compilation passes; visual appearance still needs Play Mode review.
This supersedes the procedural-color beam description above.

## Charge selection after pursuit

The quick charge was previously considered only when choosing a new combo opener.
After a low leg-sweep pursuit ended close to the player, follow-up selection could
choose only stomp/sweep and never re-evaluated charge. It now rechecks the existing
3.5-unit proximity range after pursuit follow-through and selects charge if ready,
within the three-attack combo cap. The 0.25-second tell, 0.3-second dash, four-second
cooldown, and guaranteed eventual recovery remain unchanged. Source/test compile
checks pass; runtime playtest remains pending. Regression coverage checks that
this follow-up reaches the actual dash and honors cooldown/combo limits.

## Charge ground-burst wiring correction

User clarified that the missing charge animation meant the bottom-row cyan/purple
ground burst from the VFX atlas. It had only been used by stomp waves; changing
body-charge selection did not add that effect. The same frames now animate as a
ground wake during BOTH the quick dash and sustained low leg-sweep pursuit.
The wake follows actual resolved movement, flips with direction, draws at order
44 above the near legs, and leaves a short visual-only skid burst on completion.
It continues animating after an attack has already hit. Pause, explicit clear,
death and restart retain existing cleanup. Damage/timing are unchanged.

C# and test compilation pass. Regression checks cover visibility, layer order,
frame progression after a hit, following movement, mirroring and cleanup. Visual
review remains pending in TurretBossSandbox.

## Charge burst placement

Moved the charge effect from a rear offset under the body to the leading side:
center = current body X + travel direction × 0.9, with width 1.8 and height 0.65.
The low burst now occupies the front half of the attack, with its bright crest
ahead of the feet. Sprite mirroring and the completion burst use the same anchor.
Source/test compilation passes; visual placement still needs review.

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

## Body-centered charge object and contact box

Replaced moving-foot VFX anchoring with one body-centered charge object. A
serialized mirrored offset (1, -0.6) positions it from the chassis centerline.
A downward Ground-layer raycast sets its bottom on the floor; a trigger alone
does not provide physical ground placement. The sprite and BoxCollider2D share
the same transform and 1.8 × 0.65 world footprint. The box is enabled only in
Lunging/Spinning (quick/sustained charge), never during warning or follow-through.
Trigger enter/stay notifications and a sweep of the same box share one hit gate,
so fast travel cannot tunnel or deal damage twice. This replaces the old separate
lunge/spin contact rectangles with the visible charge object's footprint.
The skid aftermath has no collider. Phase-one low-port firing is unchanged.

Source/test compilation passes. Added checks for warning/recovery disabling,
Ground placement and single-hit contact. Runtime/visual verification is pending.
This supersedes earlier leading-foot placement and body-contact descriptions.


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
