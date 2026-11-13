# Two-Handed Hero — standalone Unity prototype

Created 2026-10-27. Tested with Unity 6000.6.0f1. This package is independent of the Escape the 90s repository and references no TL1/TL2/TL4 game assemblies.

## Import and preview

1. Extract the ZIP somewhere outside your Unity project.
2. Copy `Assets/TwoHandedHero` and `Assets/TwoHandedHero.meta` into your project's `Assets` folder. Keep all `.meta` files so sprite, clip, and prefab references remain intact.
3. Let Unity finish importing and compiling.
4. Open `Assets/TwoHandedHero/Scenes/TwoHandedPreview.unity` and press Play. A landscape Game view is recommended.
5. Use the on-screen Run, Stop, Fire, face-direction and weapon buttons. Auto preview cycles Carry → Run → Stop → Shoot. The speed slider intentionally allows testing stride/travel mismatches.

The package contains ready-built assets. You do not need to run the editor builder. The included `Tools > Two Handed Hero > Rebuild Prototype Assets` command replaces this package's generated clips, weapon definitions, prefab, and scene; only use it before making your own customizations.

## Included artwork and animation

| Action | Sprite map | Unique frames | Playback | Loop |
| --- | --- | ---: | ---: | --- |
| Carry / idle | `Art/Carry.png` | 8 | 8 FPS | Yes |
| Run | `Art/Run.png` | 16 | 16 FPS | Yes |
| Coming to stop | `Art/Stop.png` | 6 | 12 FPS | No; returns to Carry |
| Standing shoot | `Art/Shoot.png` | 4 | 12 FPS | No; returns to Carry |

`Art/Parts.png` contains two reusable complete arm sprites and two interchangeable rifles. The back/support arm and front/trigger arm are separate SpriteRenderers. The body maps omit arms and weapons. Hair and cape motion are currently baked into the body frames, not separate cape physics.

The PNGs contain actual alpha. Sprite rectangles, feet pivots, hand/grip pivots, Point filtering and uncompressed imports are already saved in `.meta` files. **Use the saved slicing**, not automatic equal-grid slicing: the generated Stop atlas has a noncentral row gutter, and the parts use tight custom rectangles. PNG source dimensions also differ by action; each sheet's PPU is calibrated to approximately 2.4 world units of character height.

These are AI-generated prototype drawings based on the supplied PlayerWalkDemo video, not exact extracted animation frames. They preserve the blue hair, red scarf/cape and blue cyber armor, but shapes, pixel density and pose continuity still merit artist cleanup. The arms use one shared two-handed pose with attachment motion and recoil rather than a unique arm drawing for every body frame. This is a working modular starting point, not a final hand-polished production animation set.

## Layer and attachment structure

```text
TwoHandedHero                   TwoHandedRig (visual control API)
└── Visuals                     Animator; mirror this entire group
    ├── Body                    sorting order 10
    ├── Shoulder                frame-animated shoulder height
    └── WeaponGrip              primary hand/grip anchor, bob and recoil
        ├── SupportHand         order 5; pivot at supporting palm
        ├── Weapon              order 20; pivot at trigger grip
        ├── TriggerHand         order 30; pivot at grip hand
        └── Muzzle              projectile/effect attachment point
```

The Animator owns body sprite changes and shoulder-height keys. `TwoHandedRig.LateUpdate` positions the shared grip and both hands after Animator evaluation. The hands therefore follow weapon recoil together. Flip the whole Visuals transform via `SetFacingRight`; flipping only the body SpriteRenderer would leave the weapon behind.

Two `TwoHandedWeapon` ScriptableObjects are provided: PulseRifle and ArcRifle. Both use the same grip family and arm pose. Each stores its sprite, support-hand offset, muzzle offset and recoil settings. Existing weapon art renderer scale is calibrated for these two similarly sized sprites. For a differently sized weapon, calibrate sprite PPU or renderer scale as well as grip/muzzle positions; matching only the image bounds will not ensure a correct grip. These offsets are in unscaled WeaponGrip-local units.

## Integrate into a controller

Use the prefab as a visual child of your player object. Keep physics/input on the parent; do not add a second movement controller to this visual prefab.

```csharp
using TwoHandedHero;

// Call on locomotion changes (or every frame; unchanged values are ignored):
rig.SetFacingRight(horizontalVelocity >= 0); // retain last facing when velocity is zero
rig.SetMoving(isRunning);

// Weapon switching:
rig.Equip(selectedWeaponDefinition);

// On a successfully authorized shot from your gameplay system:
rig.Fire();
// Spawn your projectile/effect at rig.muzzle.position.
// Horizontal direction: rig.FacingRight ? Vector2.right : Vector2.left.
```

`SetMoving(false)` after a run begins Stop. Starting a run interrupts Stop or standing Shoot immediately. Stop returns to Carry after 0.5 seconds; standing Shoot returns after 4/12 seconds. Firing while running adds visual recoil without replacing the Run body animation. If you change clip durations, update the corresponding timing fields on the rig. `Fire()` is a visual request: it does not enforce ammo, cooldown, damage, or spawn a projectile.

This first version supports horizontal two-handed carrying/firing in both directions. It does not include angled aim, reload, walk, jump, crouch, weapon-drop, collision, IK, or independent shoulder rotation. For differently sized or angled weapon families, author additional arm poses instead of stretching this one.

The preview uses IMGUI buttons, so it does not require a particular Input System configuration. Auto movement is only for demonstration and is not a gameplay controller. Connect actual movement and attack events in your own code.

## Validation and limitations

The package is built and tested in a separate Unity project. Automated checks cover sprite frame counts, interruptible stopping, shooting during a run, weapon replacement, facing mirroring, hand and muzzle attachment positions, and return to Carry. Rendered previews cover the starting pose of all four clips. These checks do not certify every in-between drawing or production gameplay timing.

The ZIP's `Verification` folder contains test results and optional test sources. To run those tests yourself, install Unity Test Framework and copy the optional Tests folder into `Assets/TwoHandedHero/Tests`. They are outside the default import so the artwork package does not require Test Framework.

No project-wide settings, existing player scripts, scenes or prefabs are modified by importing this folder. The package uses its own namespace and assemblies. Keep the README and `.meta` files when sharing it.
