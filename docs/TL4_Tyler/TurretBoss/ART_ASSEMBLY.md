# Siege Core modular art assembly

Open `Assets/Scenes/TL4_Tyler/TurretBoss/TurretBossArtPreview.unity` and press Play.
The planted assembly is on the left, mobile on the right. A/D or arrow keys walk
the mobile form at 0.65 units/second within the review area. Space toggles core
shutters; F mirrors both forms. This preview creates its camera at runtime.

## Delivered assets

- `Assets/prefabs/TL4_Tyler/TurretBoss/Art/SiegeCoreParts.png`: corrected transparent
  source atlas. The reactor is bare energy with end caps, without a cage.
- Texture import metadata: 12 named 362 × 362 slices, custom mounting/joint pivots,
  100 pixels per unit, point filtering, no compression or mipmaps.
- `Art/SiegeCoreParts.json`: slice rectangles and measured source joint anchors.
- `Assets/prefabs/TL4_Tyler/TurretBoss/SiegeCoreVisual.prefab`: reusable visual rig
  definition; builds its sprite hierarchy at runtime.
- `TurretBossVisualRig.cs`: body, cannon recoil, payload launcher, shutter and
  four separate hip/knee/ankle hierarchies. Far legs render behind the body.
- `TurretBossArtPreview.cs`: isolated static review controls.

| Atlas row | Column 1 | Column 2 | Column 3 | Column 4 |
| --- | --- | --- | --- | --- |
| Top | Dome | Chassis | Cannon | Launcher |
| Middle | Launcher mount | Bare core | Left shutter | Right shutter |
| Bottom | Planted base | Upper leg | Lower leg | Foot |

Atlas pivots and endpoint offsets are initial measured assembly anchors. Local
part scales compensate for generated artwork proportions; they are recorded in
the rig builder. Source art is preserved unchanged, with slicing handled by Unity
metadata. Upper/lower legs were corrected to be independent links before import.
The core remains behind the two independently positioned solid shutters.

## Boundaries and verification

The combat prefab, procedural combat legs, hazard origin logic and attack timing
are unchanged. This is isolated art presentation, not production animation or gameplay
integration. Cannon and payload muzzle transforms are provided for future binding.
No sprite maps for walk, transformation, attacks or death have been generated yet.

Source compilation and metadata/GUID/slice structural checks passed. Alpha was
verified in the PNG. Unity import, visual alignment, shutter coverage and both
facing directions still require review in Play Mode; do not treat compile checks
as visual acceptance. Next work is to correct any fit issues in this preview,
then animate its existing pivots and connect combat-state presentation.

## Standing pose adjustment

Mobile body height is now 2.4 units. Near hips sit at ±1.1 / y=2.2, with
ankles at ±2.15 / y=0.46. Far hips sit at ±0.75 / y=2.3, with ankles at
±1.55 / y=0.58 to stagger their silhouettes in depth. A two-link knee solve
preserves source segment lengths and bends the knees outward while bringing
the feet under the body. Sprite rotation is separate from the joint hierarchy,
and feet remain level. Compilation and numerical reach/length checks passed;
visual approval remains a Play Mode check in TurretBossArtPreview.

## Idle and planted-foot crawl

Both forms now use low-amplitude chassis sway and upper-shell lag. Mobile hips
follow the weight shift while world-space ankle anchors hold all resting feet
still. Walking uses a four-foot crawl order (far left, near right, far right,
near left), with one lifted foot and three support feet. The 1.5-second cycle
uses eased foot travel, 0.28-unit clearance and forward placement based on actual
root velocity. Release movement to finish the current step and settle without
snapping all feet to the default pose. Mirroring or teleporting resets anchors.
The floor baseline makes slipping easier to inspect. Use F to inspect both
facing directions; the motion also supports backing up.

This is procedural joint animation using the approved separate parts. It does
not generate or require duplicated sprite frames. Combat still uses the old
prototype visuals. Preview speed, gait period and step height are serialized
for later tuning. Current gait targets flat ground at the preview's slow speed;
terrain placement and high-speed charge/spin presentation are separate work.

Source and regression-test compilation passed. A numerical 12-second move/stop
check kept every foot target reachable. Added Unity regression coverage for idle
foot locking, one-foot stepping, segment length preservation and settling; that
runtime test has not yet been executed. Visual timing still needs Play Mode review.

## Script reload recovery

The rig now reconnects its turret, weapon anchors, shutters and four leg states
from the generated hierarchy on enable and when animation detects reset runtime
references. This addresses a null turret reference when adding animation fields
while a preview instance already exists. Existing hierarchy objects are reused.
Regression coverage simulates cleared turret/leg runtime fields and checks that
recovery does not duplicate the assembly. Compilation passed; runtime execution
remains pending. Restart Play Mode after this update and clear earlier errors.

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

## Core clearance correction

The physical base now retains its 1.18 horizontal scale with a lower 0.54 vertical
profile. Its opaque front rim stays below the exposed reactor in the planted
pose, while still hiding folded machinery near the floor. Body height, shutters,
leg stance and deployment timing are unchanged. Source compilation passes;
visual clearance should be reviewed with Space in the restarted art preview.

## Planted body seating

Lowered the planted chassis from 1.5 to 1.32 units and placed the platform behind
the chassis/core/shutters. Mobile height and standing stance remain unchanged.
Idle, direct setup and transformation share the same height constants, including
the tucked hip offset. This closes the floating gap without obscuring the core.
Compilation passed; final overlap is a visual preview check.

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

## Small charge-fit adjustment

Trimmed 28 bottom padding pixels from the ground-burst frame slices so the drawn
base lies closer to the grounded box. Nudged the mirrored forward offset from
1.0 to 1.15 and reduced the shared graphic/contact-box height from 0.65 to 0.55;
width remains 1.8. No attack timing or selection changes. Compilation passes.

## Final charge size/height nudge

Increased the shared effect/contact footprint by 1.25× (2.25 × 0.6875), with a
0.04-unit ground inset to settle the drawn effect slightly lower. Horizontal
offset stays 1.15; aftermath shares the updated size and position. Compilation
passes; visual approval remains a sandbox check.


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
