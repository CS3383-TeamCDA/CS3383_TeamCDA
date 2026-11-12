# Siege Core art and presentation

Gameplay/timings live in [README.md](README.md). This reference covers the current
art pipeline, replacing superseded pose/VFX iteration notes.

## Assets

Under `Assets/prefabs/TL4_Tyler/TurretBoss/`:

| Asset | Purpose |
| --- | --- |
| `SiegeCoreVisual.prefab` | Modular rig; constructs its sprite hierarchy at runtime |
| `Art/SiegeCoreParts.png` and `.json` | Transparent parts atlas and measured slice/joint metadata |
| `Art/SiegeCoreAttackVfx.asset`, `.png`, `.json` | Attack effects configuration and 32-frame atlas |
| `Art/SiegeCoreBeam.mat` | Continuous animated beam material |
| `Art/SiegeCoreDamage.mat` | Progressive wear and hit-feedback material |

Parts use 12 named 362 × 362 slices, custom pivots, 100 pixels per unit, point
filtering and no mipmaps/compression. The bare energy reactor has no cage;
separate solid shutters slide aside to expose it.

| Atlas row | Column 1 | Column 2 | Column 3 | Column 4 |
| --- | --- | --- | --- | --- |
| Top | Dome | Chassis | Cannon | Launcher |
| Middle | Launcher mount | Bare core | Left shutter | Right shutter |
| Bottom | Planted base | Upper leg | Lower leg | Foot |

## Rig and combat adapter

`TurretBossVisualRig` owns modular assembly, articulated hip/knee/ankle links,
planted-foot gait, shutter motion, independent weapon clocks and damage/death
presentation. Far legs render behind the body; feet stay level. Planted/mobile
body heights are 1.32 / 2.4. The low-profile opaque platform sits behind the core
and remains parked when the boss deploys. Retraction returns to the platform
before folding. Walking uses world-space foot anchors; charge poses brace the
legs rather than attempting a spin animation.

`TurretBossCombatVisuals` follows the controller on a separate grounded visual
root. It hides legacy sprite/Animator/stick legs when the modular art is present,
binds weapon origins and forwards accepted damage/launcher events. It does not
own attack selection or hit detection. Cosmetic body lift must not move physical
collision off the ground.

Cannon recoil, launcher kick and shutters can run alongside leg movement. Core
exposure has no surrounding cage. Progressive scratches/chips/scorching accumulate
with HP loss via per-renderer shader properties; accepted hits flash/jolt/spark.
Sparks and energy instability increase with damage. Death cancels weapons,
flares/darkens the core, opens shutters and settles the body/legs into persistent
wreckage. All animation uses scaled time and is removed with the arena.

## Attack presentation

The effects atlas supplies eight plasma frames, four grenade frames, four
rocket/exhaust frames, eight explosion frames and eight ground-wave frames.
The continuous beam samples the original plasma artwork with animated flow and
no gaps. Low bolts use the plasma frames; rockets flip for descent; grenades
rotate during lobs. Landing shadows are generated oval sprites, not damaging
objects. Explosion and skid aftermath do not extend damage lifetimes.

Charge art and its active contact box share a grounded footprint; exact geometry
is documented in the gameplay guide. Do not restore animated-foot anchoring or
anchor phase-two low shots to the abandoned platform. The HUD headshot assembles
the dome and cannon from existing art rather than introducing another atlas.

## Art preview

Open `Assets/Scenes/TL4_Tyler/TurretBoss/TurretBossArtPreview.unity`, then Play.
The scene creates its camera at runtime and is separate from combat.

| Control | Preview |
| --- | --- |
| A / D | Walk the right assembly |
| T | Deploy / return to dock and retract |
| Space | Slide core shutters |
| F | Mirror |
| B / L | Cannon / independent launcher |
| H | Simulated hit and progressive wear |
| K | Defeat right assembly |

Restart Play Mode to reset. Preview weapon clocks are presentation demonstrations;
combat timing is driven by the controller. Missing VFX assets fall back to simple
hazard graphics. Runtime hierarchy reconstruction supports script reloads.

## Remaining validation

The user reviewed the art in the sandbox. Automated runtime and shader/render
checks still need a successful Unity session. Review both facings, portrait fit,
shutter clearance, deployment/docking, overlapping payload shadows, charge
contact alignment, progressive wear, death cleanup, pause and restart. Source
compilation alone does not establish visual correctness.
