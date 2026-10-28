# Escape the 90s — art style and asset generation guide

Version 2 · Updated 2026-10-27 · Owner: TL4 / Tyler

**Style in one sentence:** A ruined neon arcade city inhabited by hostile machines and purple mutants, rendered as crisp side-scrolling pixel art with near-black silhouettes, blue-violet shadows, cyan screen light, and hot-magenta energy.

Use this guide and the relevant reference images together when requesting new artwork. This is an art direction document, not a claim that every depicted character, attack, menu, or visual effect is implemented. Text inside the reference boards is illustrative; the production requirements below define deliverables.

For player work, read **Section 11: Player character assembly contract** before generating or assembling assets. Its player-specific requirements take precedence over the generic examples.

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

## 11. Player character assembly contract

### 11.1 Scope, references, and authority

This section specifies the first **horizontal, two-handed rifle** setup. MUST means a required invariant; SHOULD means a preferred choice that may change with an explicit brief. Do not silently substitute a one-handed pose, a flattened character-with-gun image, or an unrelated player controller.

- [Player identity reference](references/player-walk-identity.png): frame from the supplied `PlayerWalkDemo.mp4`. Preserve spiky dark-blue hair, red neck scarf and long cape, navy cyber armor, cyan/magenta highlights, blue backpack, and mechanical legs.
- [Assembled Unity prototype](references/player-two-handed-unity.png): demonstrates separate body, arms and rifle. This is a working generated-art prototype, not a guarantee of final animation polish.
- Implementation source of truth: the separately delivered **`TwoHandedHero_UnityAssets.zip`**, containing `Assets/TwoHandedHero`. It has intentionally **not** been installed in this repository. Request the ZIP or its source folder if it is unavailable; do not claim to have inspected or imported it from this guide alone.
- The ZIP's `.meta` files, prefab, clips and scripts define its exact current assembly. Preserve those files when extending that package. Numbers below describe that version; they are not universal measurements for new drawings.
- Future player deliverables stay outside the repository as a downloadable asset-folder ZIP unless the user explicitly requests installation. Documentation and its visual references may live here.

### 11.2 Required separation of artwork

| Piece | MUST contain | MUST NOT contain |
| --- | --- | --- |
| Body action maps | Head/hair, scarf/cape, torso with shoulder attachment area, backpack, hips, legs and feet | Arms, hands, weapon, muzzle flash, scenery |
| Support arm | Complete far arm: shoulder, upper arm, elbow, forearm, supporting palm | Torso, rifle or second arm |
| Trigger arm | Complete near arm: shoulder, upper arm, elbow, forearm, trigger-grip hand | Torso, rifle or second arm |
| Weapon | Complete rifle, stock, grip, foregrip, barrel | Character's hands, arms or body |
| Optional muzzle/impact effects | Effect only, with explicit attachment/timing metadata | Baked-in weapon or character |

The first package uses **one reusable sprite for each complete arm**, not individual upper-arm/forearm bones and not a new arm image for every frame. Arm transforms follow the weapon and adjust for braking/recoil. Hair and cape are baked into the body maps. Do not describe this as IK, cloth physics, a skeletal rig, or independently animated cape layers.

Keep complete body maps for separate actions. Never present a labeled concept board as an importable atlas. Export actual RGBA transparency, preserve internal armor colors, and inspect alpha on light and dark backgrounds. No opaque checkerboard, background glow plate, or neighboring-frame fragments are allowed.

### 11.3 Exact prefab hierarchy and draw order

Use these names and paths when extending the existing package; animation bindings depend on them.

```text
TwoHandedHero                         TwoHandedRig; root for external placement
└── Visuals                           Animator; mirror this entire transform
    ├── Body                          SpriteRenderer, sorting order 10
    ├── Shoulder                      Transform; frame-keyed shoulder height
    └── WeaponGrip                    Transform; shared attachment/recoil origin
        ├── SupportHand               SpriteRenderer, order 5; COMPLETE support arm
        ├── Weapon                    SpriteRenderer, order 20
        ├── TriggerHand               SpriteRenderer, order 30; COMPLETE trigger arm
        └── Muzzle                    Transform; shot/effect origin
```

All four renderers MUST use the same sorting layer. Back-to-front order is support arm → body → weapon → trigger arm. The `SupportHand` and `TriggerHand` names describe their pivot roles; the sprites contain complete arms. Do not add duplicate arms to the body.

Keep `WeaponGrip` and `Shoulder` as siblings under `Visuals`. The rig computes their relationship; parenting the grip beneath Shoulder as well would apply the offset twice. Body clips bind `Body/m_Sprite` and `Shoulder/m_LocalPosition.y` relative to the Animator on `Visuals`. They MUST NOT keyframe the Weapon sprite, root world position, facing scale, or hand transforms.

### 11.4 Coordinate and grip contract

Author facing right: local +X points toward the muzzle and +Y points up. Anchor offsets are in **WeaponGrip-local Unity units**, not pixels and not scaled Weapon-renderer coordinates. All `WeaponGrip` children share bob, recoil and facing. The weapon image's pivot is the trigger-hand grip.

| Setting | Current package value |
| --- | --- |
| Carry and Run grip offset from Shoulder | `(0.67, -0.14)` |
| Stop grip offset from Shoulder | `(0.58, -0.36)` |
| TriggerHand local position | `(0, 0, 0)` |
| SupportHand position, both sample rifles | `(0.54, 0.02, 0)` |
| Muzzle position, both sample rifles | `(1.17, 0.30, 0)` |
| Trigger arm Z rotation | `0°`; Stop uses `-20°` |
| Support arm Z rotation | `17°`; Stop uses `5°` |
| Run bob | `0.025 × sin(runTime × 4π / runCycleSeconds)` on local Y |
| Run cycle duration | `1.0` second |
| Default recoil | `0.055` units backward, recovering over `0.16` seconds |

Evaluation order MUST be:

1. Animator chooses body sprite and shoulder-height key for the current frame.
2. In `LateUpdate`, compute `WeaponGrip.localPosition = Shoulder.localPosition + actionGripOffset + bob + recoil`.
3. Set TriggerHand local position to zero and apply its action rotation.
4. Set SupportHand and Muzzle local positions from the equipped weapon definition; apply the support-arm action rotation.
5. Render the whole assembly with the same facing transform.

For recoil, subtract `recoilDistance × remainingRecoilTime / recoilSeconds` from the grip's local X. Moving only the rifle image would detach the hands and MUST NOT be used.

`SetFacingRight` mirrors the **entire Visuals group** by changing the sign of its X scale, preserving the original scale magnitude and Y/Z scales. Do not flip just Body, double-flip the gun, or mirror the physics root. Retain the previous facing when movement input/velocity is zero.

### 11.5 Sprite metadata and dimensions

Keep existing `.meta` files; do not let Unity regenerate their GUIDs. Use Point filtering, no mipmaps, no lossy compression, and preserve texture dimensions. Do not independently center or auto-trim each body sprite and discard its anchor.

Current package specifics for reproducibility:

- Carry PNG: `1774 × 887`, eight sprites in reading order.
- Run PNG: `1254 × 1254`, sixteen sprites in reading order.
- Stop PNG: `1536 × 1024`, six sprites. Its row gutter is offset: in Unity bottom-left coordinates the upper-row rectangles start at Y `534` with height `490`; the lower row starts at Y `0` with height `534`. Each column is `512` pixels wide. **Do not replace this with an equal 3×2 automatic slice.**
- Shoot PNG: `1254 × 1254`, four sprites in reading order.
- Parts PNG: `1536 × 1024`; support arm upper-left, trigger arm upper-right, PulseRifle lower-left, ArcRifle lower-right. The saved sprites use tight rectangles inside these regions.
- Parts pivots are normalized within their saved tight rectangles: support arm `(0.86, 0.13)`, trigger arm `(0.91, 0.49)`, each rifle `(0.29, 0.15)`. Parts import at `400` PPU.
- The builder sizes the PulseRifle renderer to `1.65` world units wide, support arm to `1.5`, trigger arm to `0.85`. Both rifles reuse the calibrated weapon renderer scale; new rifle art must match that scale or be recalibrated.
- Body PPU is computed per sheet as maximum visible frame height divided by `2.4`. Body pivots use a measured foot baseline and per-frame shoulder-X anchors. The generated shoulder-height keys use visible-frame height divided by PPU, multiplied by `0.68` for Carry/Run or `0.71` for Stop/Shoot. These are prototype calibrations, not anatomical measurements guaranteed for replacement art.

For exact per-frame rectangles and pivots, read the supplied PNG `.meta` files or `Editor/BuildTwoHandedHero.cs`. Do not infer them from the rendered preview. For newly authored maps, prefer genuinely uniform cells, consistent native pixel density, recorded attachment coordinates and a common baseline. If artwork geometry changes, update the import metadata and attachment calibration together, then review every frame.

### 11.6 Animation and event contract

A frame count means **unique drawings**, not render frames or duplicated keys. A repeated endpoint key for looping/holding does not count as a new drawing.

| State | Drawings | Sprite playback | Duration | End behavior |
| --- | ---: | ---: | ---: | --- |
| Carry | 8 | 8 FPS | 1 second | Loop |
| Run | 16 | 16 FPS | 1 second | Loop |
| Stop | 6 | 12 FPS | 0.5 seconds | Return to Carry |
| Shoot, stationary | 4 | 12 FPS | 1/3 second | Return to Carry |

New run variants may use **12–16 distinct frames**; new stopping variants may use **4–8**. The first package uses the exact counts above. Changing a frame count or playback rate MUST also update the runtime duration fields and run-bob period. Rendering at 60 FPS is independent of sprite playback FPS.

Runtime behavior MUST preserve these rules:

- Start in Carry.
- `SetMoving(true)` immediately starts Run; repeated calls with the same value do not restart the loop.
- Changing from moving to stopped starts Stop once. Plant/brake → absorb momentum → settle torso → let hair/cape catch up. Returning to Run interrupts Stop immediately.
- `Fire()` while stationary starts Shoot and recoil. It can interrupt Stop. Shoot then returns to Carry.
- `Fire()` while running keeps Run on the body and applies recoil to the shared weapon/hand assembly. It MUST NOT freeze or reset the legs.
- Starting to run interrupts standing Shoot. Do not force the user to wait for an action clip before movement responds.
- Stop and Shoot transitions in this prototype are driven by `TwoHandedRig` timers and `Animator.Play`, not Animator triggers or animation events. Do not add a second competing transition system.
- `Fire()` is a visual request, not permission to deal damage. The gameplay controller owns fire rate, ammo, collision, damage and projectile spawning. It calls the visual event only after accepting the shot.

There are no authored Walk, Jump, Crouch, Reload or angled-aim states in this package. Do not rename Run to Walk or claim these actions exist. A future jump implementation must define how it interrupts Stop/Shoot without delaying gameplay response.

### 11.7 Interchangeable weapon requirements

Use `TwoHandedWeapon` definitions for sprite, support-grip offset, muzzle offset, recoil distance and recoil duration. Equip using `rig.Equip(definition)` so both attachments update with the artwork. Do not directly replace only the SpriteRenderer sprite.

To add a rifle in this pose family:

1. Author its trigger grip and foregrip to match the reference handling pose. Keep stock clearance around the face and shoulder.
2. Import the sprite with its pivot at the trigger-hand contact, not the geometric center.
3. Match the existing renderer's physical scale by calibrating PPU or sprite dimensions. Do not assume equal image dimensions imply equal weapon size.
4. Duplicate a weapon definition and assign the new sprite.
5. Measure support-palm and muzzle offsets in WeaponGrip-local space. If measuring a point from weapon pixels, subtract the sprite pivot, divide by PPU, and apply the Weapon renderer's local transform to obtain the grip-local position.
6. Preview right and left facing, Carry, Run, Stop and Shoot. Check shoulder connection, finger/grip contact, supporting palm, stock/face clearance and muzzle position.
7. If the stock, grip spacing or weapon mass cannot fit naturally, create a new arm-pose family. Do not stretch limbs or hide major gaps with glow.

The first support hand is a cupped palm. A vertical foregrip can need different finger artwork for a convincing full grasp; transform alignment alone is not proof of anatomically correct contact. Validate the image as well as the coordinates.

### 11.8 Assembly sequence for an automated agent

1. Read this section and the package README. Confirm access to the actual source assets. Preserve user ownership boundaries and the requested output location.
2. Inspect current `.meta`, prefab, clips and runtime scripts before editing. Record baseline GUIDs and frame counts.
3. Generate or modify only the requested pieces. Preserve all other identity details. Keep a source-art copy; do not overwrite approved assets without authorization.
4. Import into an isolated Unity project. Build the named hierarchy, renderer ordering, grip pivots and shoulder animation bindings exactly as specified.
5. Use existing package assets when available. The rebuild menu replaces the package's generated clips, weapon definitions, prefab and preview scene, so do not run it over user-customized assets without a reason and a recoverable copy.
6. Render the assembled character, not just disconnected sprite sheets. Check every frame and transition, both directions, both rifles, and the rifle's visibility against the cape/body.
7. Run behavior checks: frame counts; stop interruption; running fire; weapon swap; mirrored hand/muzzle coordinates; return to Carry. The delivered version passed eight automated tests; those tests do not verify every pixel or pose.
8. Package `Assets/TwoHandedHero` with its folder `.meta`, all child `.meta` files, PNGs, `.anim` clips, controller, prefab, scene, weapon definitions, runtime scripts and README. Keep optional Test Framework sources outside the default imported Assets tree unless the dependency is explicitly desired.
9. Open the ZIP and verify integrity, relative paths, and all serialized GUID references. Do not include Library, Temp, machine-specific paths, unrelated teammate assets, or project-wide settings.
10. Report the delivered files, actual checks performed and remaining limitations. Do not equate successful compilation with finished animation quality.

### 11.9 Player acceptance gates

- [ ] Body has no baked arms or gun; no doubled shoulders, hands or weapons after assembly.
- [ ] Every body frame and both arm sprites preserve identity, proportions and consistent apparent pixel size.
- [ ] Trigger hand contacts the trigger grip; support hand contacts the intended foregrip in both directions.
- [ ] Rifle does not hide the face during braking, recoil or a run; stock does not pass through the head.
- [ ] No frame includes pixels from its neighbor; pivots do not jump between actions.
- [ ] Rifle, both hands and muzzle move together during recoil and facing changes.
- [ ] Run has the requested distinct poses and readable foot contacts; travel speed is tested against stride speed.
- [ ] Stop can be interrupted; firing while running preserves the leg cycle.
- [ ] Gameplay receives a correctly transformed muzzle point; firing visuals do not duplicate damage or ammo logic.
- [ ] Imported package opens without references to the original game repository or the creator's local paths.
- [ ] ZIP includes import instructions, metadata, and an honest account of which poses/tests were reviewed.

### 11.10 Copy-ready player generation brief

```text
Follow ART_STYLE_GUIDE.md section 11. Produce a modular TWO-HANDED player asset,
not a flattened hero-with-weapon illustration. Use player-walk-identity.png for
identity and player-two-handed-unity.png for the assembled handling relationship.

Piece to create: [BODY ACTION MAP / SUPPORT ARM / TRIGGER ARM / RIFLE / EFFECT].
Action: [Carry / Run / Stop / Shoot]. Exact unique drawings: [8 / 16 / 6 / 4].
Cell dimensions and layout: [explicit dimensions and rows/columns].
Maintain right-facing fixed side view, scale, pixel density and recorded anchors.
Body-only maps MUST omit both arms, both hands, weapon and muzzle flash. Preserve
blue hair, red scarf/cape, blue backpack, navy cyber armor and cyan/magenta details.
Arm-only output MUST omit body and weapon. Rifle-only output MUST omit all hands.
Use true transparent RGBA. No labels, grids, frames, scenery or painted checkerboard.
Keep every silhouette inside its assigned cell with padding. Do not duplicate
poses to meet the count. For Run, include foot contacts, passing poses and push-off
with delayed cape/hair response. For Stop, show plant, braking, settling and follow-through.

Deliver the requested independent PNG(s) plus explicit frame order, pivot/anchor
coordinates and timing. Do not change scripts, install assets in the repository,
or claim a Unity prefab exists unless those actions were requested and completed.
```
