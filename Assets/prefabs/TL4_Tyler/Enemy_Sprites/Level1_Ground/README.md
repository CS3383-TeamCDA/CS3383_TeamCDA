# Level 1 ground enemy visuals

Drag `Level1_Ground_Animated.prefab` into a scene. Its Animator starts in Idle.

## Animation controls

- `Moving` bool: walk when true, idle when false.
- `Running` bool: run while true (takes priority over Moving).
- `Shoot`, `Melee`, `Hit`, `Death` triggers: play the matching action.
- Shoot, melee and hit return to idle, then resume the selected locomotion state.
- Death holds its last pose. Gameplay must stop sending triggers after death.

## Clips

Each character clip contains eight distinct poses. Idle plays at 8 sprite FPS, Walk at 12, Run at 16, and Shoot/Melee/Hit/Death at 12. Idle, Walk and Run loop; actions play once. The endpoint key closes looping clips without adding a repeated-frame pause. Rendering the game at 60 FPS is independent of sprite playback speed.

The `Sprites/` folder contains one eight-frame sheet per action. The earlier `Level1_Ground.png` remains for existing sprite references and the unchanged single-frame Projectile clip/prefab. Existing animation clip, controller and prefab GUIDs are retained.

The animated enemy prefab contains visual components only. Enemy movement, damage and trigger calls still need gameplay integration. The projectile prefab has no movement or damage behavior.

## Art source and validation

Art was created with the built-in image generation tool using the earlier alien atlas as the identity reference. Prompt brief: preserve purple alien, teal armor and red goggles; produce eight distinct poses each for idle, walk, run, shoot, melee, hit and death on separate transparent sheets, each with four columns and two rows; keep generous padding, consistent scale and right-facing orientation, with no labels or borders. These are AI-generated intermediate poses, not pixel-exact edits of the original artwork.

Assets were imported and checked in Unity 6000.6.0f1 in an isolated project. Validation checks frame presence and animation sprite references. In-game motion and transitions still need playtesting with the enemy controller.
