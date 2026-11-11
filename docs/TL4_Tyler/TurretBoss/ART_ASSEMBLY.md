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
