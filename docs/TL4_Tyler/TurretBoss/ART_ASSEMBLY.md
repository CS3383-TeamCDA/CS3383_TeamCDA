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
