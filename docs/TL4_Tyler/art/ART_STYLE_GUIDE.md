# Escape the 90s — art style and asset generation guide

Version 1 · 2026-10-22 · Owner: TL4 / Tyler

**Style in one sentence:** A ruined neon arcade city inhabited by hostile machines and purple mutants, rendered as crisp side-scrolling pixel art with near-black silhouettes, blue-violet shadows, cyan screen light, and hot-magenta energy.

Use this guide and the relevant reference images together when requesting new artwork. This is an art direction document, not a claim that every depicted character, attack, menu, or visual effect is implemented. Text inside the reference boards is illustrative; the production requirements below define deliverables.

## 1. Reference library and priority

The supplied images are preserved in `references/`. Repeated copies of the level-3 sheet have been consolidated. These are visual references, not ready-to-slice production atlases.

| Reference | Use it for |
| --- | --- |
| [City and platforms](references/city-platforms.png) | Architecture, depth, sunset palette, platform readability |
| [Boss battle](references/boss-battle.png) | Cyan/magenta lighting, mechanical weight, combat composition |
| [Pause UI](references/pause-ui.png) | Pixel typography, dark panels, cyan outlines, magenta selection |
| [CRT Titan animation](references/crt-titan-animation.png) | Heavy poses, cables, CRT expression, attack arcs and collapse |
| [Level-3 mutant](references/level3-mutant.png) | Purple organic tissue, steel implants, claws, red targeting optics |
| [Arcade mosquito](references/arcade-mosquito.png) | Flying silhouette, translucent-looking pixel wings, stinger and antennae |
| [Arcade rat](references/arcade-rat.png) | Low ground silhouette, cabinet anatomy, cable tail and plug |
| [Arcade colossus](references/arcade-colossus.png) | Cabinet armor, buttons, joysticks, boss-scale mechanical construction |

For extensions of an existing enemy, its approved sprite is the identity reference: preserve anatomy, proportions, equipment, and palette placement. Use the boards for lighting, material language, and world identity. For a new enemy, pick one primary character reference plus one environment reference. Do not combine all boards into one character.

The earlier level-1 ground enemy has purple flesh, teal armor, and red goggles. Preserve those identifying features when adding frames; do not replace it with a CRT-headed creature simply because CRTs dominate the newer references.

## 2. Non-negotiable visual rules

- **Side-scroller readability:** gameplay sprites use a fixed side view or shallow three-quarter side view. Default facing direction is right. Do not rotate the camera between animation frames.
- **Arcade technology as anatomy:** CRT glass, cabinet panels, joysticks, buttons, vents, coin slots, segmented cables, plugs, and exposed joints should explain the creature's construction.
- **Dark bodies, selective neon:** most of the body stays dark. Concentrate bright accents on faces, optics, weapons, and a few silhouette edges.
- **Visible pixel construction:** use deliberate pixel clusters, stepped diagonals, sharp edges, and small color ramps. Avoid photographic surfaces, smooth vector contours, airbrushing, and a pixel filter applied to a painted image.
- **Consistent identity:** screen housing, limb count, antenna count, weapon, markings, and proportions remain stable through every frame.
- **Gameplay first:** the feet, muzzle, wings, claws, and attack silhouette must be legible at actual gameplay size. Simplify tiny labels and surface noise before increasing resolution.

The references mix detailed concept illustrations with sprite-sheet presentations. Match their design language, but simplify illustrative microtexture for small playable sprites. “16/32-bit inspired” describes the look; it does not require reproducing a historical console's hardware limits.

## 3. Working palette

These hex values are proposed production anchors chosen to represent the references, not exact sampled colors. Reuse them and short shade ramps; do not introduce a new unrelated palette for every enemy.

| Role | Hex | Application |
| --- | --- | --- |
| Ink | `#050617` | Outline, deep occlusion, dark UI backing |
| Midnight navy | `#10133B` | Cabinet body, foreground structures |
| Deep violet | `#342065` | Shadow transitions, mutant shadow |
| Electric blue | `#2455E8` | Metal planes, cool reflected light |
| Cyan | `#19E4FF` | CRT glass, thin rim light, UI information |
| Pale ice | `#C5F7FF` | Sparse sharp highlights and effect cores |
| Purple | `#8D35C9` | Organic tissue, midtone glow, secondary panels |
| Hot magenta | `#FF19B8` | Energy, hostile screen faces, selection accents |
| Alarm red | `#FF375F` | Targeting optics, hit sparks, warnings |
| Amber | `#FFAE38` | Worn cabinet paint, buttons, coin highlights |

Suggested balance: roughly two-thirds dark body/shadow, one-quarter colored midtones, and the remaining area bright accents. This is a readability target rather than a strict pixel count. Reserve near-white for brief sparks and tiny specular highlights.

Keep color roles stable within an asset family. A red optic is not automatically a universal damage-type indicator. Distinguish variants through silhouette and equipment as well as color; do not rely on color alone.

## 4. Line, light, and material treatment

Use a near-black outer contour, typically one or two pixels at the chosen native sprite resolution, with selective cyan or magenta edges where light strikes. Maintain the same apparent outline weight across assets at their intended in-game scale.

Light readable planes from above/front with cool blue-cyan; use magenta on opposite rims and energy sources. Highlights should form discrete steps, not soft gradients. Keep self-emission close to the screen or weapon. Large bloom halos belong in separate effects or scene lighting.

| Material | Visual treatment |
| --- | --- |
| Painted cabinet metal | Broad dark planes, angular edge highlights, a few chips revealing a cool underlayer |
| CRT glass | Dark inset rectangle, blue/cyan scanline clusters, bright expressive face, thick bezel |
| Rubber cables | Dark segmented curves, occasional narrow colored highlight; clear separation from limbs |
| Purple organic tissue | Rounded clusters and compact purple ramps, fewer hard highlights than armor |
| Wings and energy | Cyan/magenta shapes with clear gaps and bright cores; preserve the main silhouette |
| Wear and damage | Localized scratches, cracked screen, missing panel, smoke or sparks; avoid noise everywhere |

## 5. Character families and scale

| Family | Shape language | Motion and distinguishing details |
| --- | --- | --- |
| Ground mutant | Forward lean, compact torso, bent legs, purple flesh, teal/steel equipment | Clear planted steps; walk and run must have different poses |
| Arcade rat | Low cabinet torso, clawed feet, long cable tail | Scurry, compressed anticipation, fast claw attack; keep tail readable |
| Flying mosquito | Small cabinet body, antennae, large wing envelope, pointed stinger | Hover bob, wing beats, forward flight lean; fixed body pivot |
| Static turret | Stable base, obvious barrel or emitter, compact armored head | Base stays planted; aim and recoil move the firing assembly |
| CRT Titan / colossus | Oversized screen, broad shoulders, heavy forearms, thick cables | Slow anticipation, decisive impact, heavy recovery; face remains a focal point |
| Higher-tier mutant | Recognizable base anatomy with larger armor/claws or extra equipment | Increase threat through shape and posture, not just more glow |

Define intended world size before drawing. A boss should feel larger through silhouette and measured scale, not through a different pixel density. Difficulty stat scaling does not require a new visual tier at every numeric level. Treat tier variants as explicit art requests.

## 6. Environments, UI, and effects

**Environment:** vertical industrial city, stacked platforms, vents, pipes, cables, billboards, ruined arcade hardware, and a purple-blue skyline. Use magenta/coral sunset bands as a background accent. Foreground platforms need an unambiguous top edge and a darker underside. Keep the playable route cleaner than decorative walls. Split near structures, middle buildings, far skyline, and sky into separate layers for parallax; lower detail and contrast with distance.

**UI:** near-black navy panels, clipped corners, thin cyan borders, square pixel lettering, and magenta active selection. Use icons plus text where practical. Export borders, icons, and panels separately; render changing health, coins, damage, level, and ammo as live text. Decorative scanlines must not obscure numbers. Do not bake a screenshot's HUD into a background asset.

**Effects:** short stepped sparks, sharp crescents, blocky smoke, cyan/magenta energy with a pale core. Use amber/red for muzzle flashes where appropriate. Keep projectile, impact, and character art separate. Make the dangerous portion readable against both dark platforms and bright signage. Large effects must not conceal the attack's starting pose.

## 7. Animation direction and existing behavior

A game rendering at 60 FPS does **not** need 60 drawings per second. Smoothness depends on distinct poses, spacing, timing, stable pivots, and readable transitions. Do not satisfy a frame request by duplicating the same drawing.

The following are starting targets for **new** assets, not a request to overwrite existing clips:

| Action | Suggested unique frames | Suggested sprite FPS | Direction |
| --- | ---: | ---: | --- |
| Idle / hover | 4–8 | 6–8 | Subtle breathing, screen flicker, cables; seamless loop |
| Walk | 8 | 10–12 | Contact, down, passing, up on both sides; loop |
| Run / fly | 8 | 12–16 | Stronger lean and extension; run visibly differs from walk |
| Shoot | 4–8 | 12 | Aim/charge, discharge, recoil, recovery; play once |
| Melee | 6–8 | 12 | Anticipation, readable strike, recovery; play once |
| Hit | 2–4 | 10–12 | Short recoil; no accidental locomotion |
| Death | 6–8 | 10–12 | Progressive failure/collapse; hold final frame |
| Boss attack | 8–12 | 10–12 | Longer telegraph and heavier recovery; tune timing per attack |

Existing ground clips already have eight poses each: Idle at 8 FPS, Walk at 12, Run at 16, and Shoot/Melee/Hit/Death at 12. Preserve their contracts when replacing artwork.

Ground enemies shoot **horizontally in their facing direction**. Draw their muzzle and projectile alignment accordingly. Turrets use exactly **0°, 45°, 90°, 135°, and 180°**, where 0° is right and 90° is up. Supply five accurate aim poses with a stable base and explicit muzzle positions. Those five aim directions are separate poses, not five successive frames of an automatic sweep. Flying artwork may suggest free aiming, but supported attacks must be agreed with gameplay code.

Keep walking/running animation in place; movement comes from gameplay. Feet may lift naturally, but do not slide the whole character across cells. Keep the body anchor stable during hover and flying animations. Large strikes can extend beyond the idle silhouette but must remain inside their frame. Include attack timing notes identifying anticipation, active strike/discharge, and recovery frames; art alone does not implement hitboxes or damage.

## 8. Production delivery contract

Before generation, fill in this brief. Do not assume the dimensions of a concept board are the gameplay resolution.

```text
Asset ID and family:
Primary identity reference:
Secondary environment/style reference:
Intended on-screen/world size:
Native cell width × height:
Matching pixel density / PPU:
Facing direction: right (unless specified)
Actions and exact unique frame counts:
Layout per action: columns × rows
Playback FPS and loop/once:
Pivot: feet/base anchor or flying body anchor
Muzzle / attachment anchor and per-pose offsets:
Separate effects required:
Destination folder:
```

- Deliver RGBA PNGs with **actual transparent alpha** for sprites. Never paint a checkerboard into the image. No black background, titles, frame numbers, labels, grid lines, or borders in production sheets.
- Prefer one sheet per action or numbered individual PNGs. Use uniform cells, a consistent pixel grid, stable scale and pivots, and enough padding for every limb and effect. Record frame order as left-to-right, then top-to-bottom.
- Do not trim each frame independently without preserving its original anchor metadata. Do not stretch sprites to fit cells or mix resolutions within an animation.
- Keep a separate labeled contact sheet for human review if useful. That sheet is documentation, not the texture imported into animation clips.
- Suggested names: `Enemy_Family_Tier_Action.png` or `Enemy_Family_Tier_Action_00.png`; keep established project names when updating existing assets.
- Supply a small manifest or README with dimensions, layout, frames, FPS, pivot, PPU, palette, reference filenames, and effect/attack timing notes.
- For new Unity imports, match the intended pixel density, use Sprite (2D and UI), Multiple for atlases, Point filtering, no mipmaps, and no lossy texture compression. Confirm alpha and platform overrides. Preserve existing GUIDs when replacing assets.

Current imports do not share one numeric PPU: inspected examples use Ground Idle 312, Turret 288.275, and Flying 203.775. These values reflect existing asset scales, not a universal art standard. Match the specific prefab and intended world size; do not reset all assets to an arbitrary common PPU.

## 9. Copy-ready generation prompts

### Base style prompt

```text
Create artwork for Escape the 90s, a 2D side-scrolling action game in a ruined
neon arcade city. Match the attached approved identity reference and style guide.
Crisp deliberate pixel clusters, stepped diagonals, near-black contour, dark navy
and violet bodies, electric-blue metal planes, selective cyan screen lighting and
hot-magenta energy highlights. Worn arcade cabinets, CRT glass, buttons, joysticks,
vents and segmented cables form believable creature anatomy. Threatening but
expressive arcade-monster personality. Strong readable silhouette at gameplay size.
Preserve exact proportions, limb count, equipment and palette placement throughout.
```

### New enemy animation

```text
[BASE STYLE PROMPT]
Asset: [ID], [ground/flying/static], [tier], [melee/ranged/both].
Identity reference: [filename]. Style reference: [filename].
Generate ONLY [action], exactly [N] distinct sequential poses, [columns] × [rows]
equal cells, [width] × [height] pixels per cell. Fixed right-facing side view,
consistent scale, pixel density and [feet/base/body] anchor. Animate in place.
Motion: [specific anticipation, action, recovery or looping motion].
Keep [unique identity features]. All limbs, wings and weapons inside cells with
padding. True transparent background. Character art only; separate [effects].
No text, labels, frame numbers, dividers, borders, checkerboard or scenery.
No camera rotation, changing anatomy, duplicate poses, blurred edges, soft painted
shading, photorealism, 3D render finish or inconsistent outline thickness.
```

### Turret aim set

```text
[BASE STYLE PROMPT]
Using [approved turret reference], produce five separate right-to-left upper-arc
aim poses in this exact order: 0, 45, 90, 135, 180 degrees. 0 points right; 90 up.
Same planted base, head proportions, fixed side camera and pivot in every cell.
Barrel points along the specified angle. Preserve muzzle identity and clearance.
Five equal cells with true transparency; no labels or lines. These are discrete
aim poses, not an animation that rotates the whole turret. [Cell size and padding].
```

### Environment layer

```text
[BASE STYLE PROMPT]
Generate [foreground platforms / midground city / distant skyline / sky] only,
for a side-scrolling scene at [dimensions and pixel density]. Match city-platforms.png.
Industrial arcade ruins, vertical buildings, pipes and selective cyan/magenta signs.
[Specify tileable edges and parallax role.] Keep playable platform tops unmistakable;
reduce contrast and detail with distance. No characters, UI, baked camera effects,
or foreground obstacles unless requested. [Transparent alpha or opaque sky].
```

For UI, substitute the panel/icon specification and use `pause-ui.png`; request separate elements and leave dynamic text empty. For concept boards, labels and backgrounds may be requested explicitly, but never reuse that delivery format for a production sprite atlas.

## 10. Review before accepting an asset

- [ ] Compare beside its approved identity reference and existing enemies at gameplay size.
- [ ] Silhouette reads on both a dark platform and a bright neon background.
- [ ] Pixel size, outline weight, materials and palette fit the chosen family.
- [ ] Anatomy, markings and equipment stay consistent across frames.
- [ ] Frame count and order match the brief; all requested poses are distinct.
- [ ] No clipping, baked checkerboard, text, unwanted border, or opaque fringe.
- [ ] Pivot and feet/base/body anchor remain stable; loop seam has no jump or extra hold.
- [ ] Run differs from walk; attacks have readable anticipation and recovery.
- [ ] Muzzle direction matches ground or turret behavior; separate effects align correctly.
- [ ] Review in Unity beside existing prefabs, including hit/death and transitions.
- [ ] Save the reference choices, prompt, manifest and approved output together.

Start with one approved idle pose before generating a full character set. Generate and review one action at a time, then preview the full set in the TL4 movement test scene. A convincing concept sheet is only the beginning: reject frames that drift in identity or fail the production checks.
