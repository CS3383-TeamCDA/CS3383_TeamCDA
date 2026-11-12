# TL4 player art and combat sandbox

Open `Assets/Scenes/TL4_Tyler/PlayerArtSandbox.unity` and press Play. The camera, platforms, targets and player are built at runtime, like the TL4 boss sandboxes.

## Controls

| Input | Action |
| --- | --- |
| A / D | Move |
| Space | Jump; press again in the air for one extra jump and a forward flip |
| Shift | Horizontal ground / air dash; move to choose direction, otherwise last travel facing |
| Mouse movement | Aim in eight directions |
| I / J / K / L | Aim up / left / down / right; combine for diagonals |
| F / left mouse | Fire test rounds |
| Q | Switch PulseRifle / ArcRifle artwork |
| S / Down / left Ctrl | Hold crouch; press while running to slide |
| H | Take 25 test damage |
| T | Heal 25 (J is now an aim key) |
| R | Reset player, shots, damage protection and target health |

Keyboard aim takes precedence while aim keys are held. Moving the mouse resumes mouse aim. Aiming controls visual facing independently of travel, so moving backward while shooting is possible. The shared grip rotates about the shoulder, keeping the rifle and both hands together in every stance. Shooting during the flip uses the final world-space muzzle position.

## Movement and feedback

`PlayerArtTest.prefab` is a TL4 copy of Matteo's player. It configures Ground, hides the template sprite, enables continuous collision/interpolation and removes the standalone spawn notifier. The original TL6 prefab and scripts are unchanged.

`PlayerSandboxMotor` disables the TL6 MonoBehaviour's tick and owns one instance of TL6's public state manager. It reuses Running, Jumping and Dashing states. Its input wrapper handles jumps itself, adding a 0.12-second coyote window, 0.14-second jump buffer and one extra airborne jump. It also transitions from Running to Jumping when walking off a ledge. The extra jump refreshes only on landing, never on an air dash. Dash has its own input gate: one per airtime, also consumed if a ground dash carries off a ledge. It uses TL6's configured 16 units/sec speed and 0.2-second gravity-free duration, with a 0.12-second recovery before another grounded dash. A dash cancels the flip visually but never replenishes the extra jump. Landing or R restores the air dash. The flip lasts 0.38 seconds, has a blue burst and rotates only the visuals. Normal variable jump height and air dash behavior are retained.

`PlayerCrouchSlide` shortens the capsule without moving its foot position and checks overhead clearance before standing. The tunnel near spawn tests blocked standing. Slide starts above 4 units/sec, begins at least 10 units/sec and decelerates by 16 units/sec² for up to 0.65 seconds. Crouch movement is 2.5 units/sec. Crouch idle holds a single sprite; CrouchWalk cycles only during movement. Jump and dash requests are suppressed while low.

TL4-owned Jump, Fall, Crouch and Slide clips use the generated atlases in `Assets/prefabs/TL4_Tyler/PlayerAnimations`. Whole-cell rectangular meshes preserve cape and boot edges. The shared visual rig adds optional shoulder-centered aiming, stable-body recoil and locomotion APIs. Slide uses its own shoulder offsets. Dash uses its own generated horizontal dive atlas and shoulder attachment keys. Dedicated hurt/death frames have not been drawn; hurt uses flash/blink and death uses a visual collapse.

`PlayerMotionEffects` adds a surface-following shadow, foot dust, takeoff/landing bursts, an extra-jump burst and dash afterimages that include body, arms and weapon. The dash also emits a stationary launch ring and short speed streaks, with a horizontal stretch in the visual pivot. Landing now plays a brief bent-knee brace followed by a recovery pose before idle/run; the 0.12–0.18 second visual response does not lock movement, jumping, aiming or shooting. `PlayerSandboxFeedback` adds landing compression, damage flashing, 0.7 seconds of damage protection and death collapse. Reset clears those effects.

## Combat test

Eight-way test shots travel at 32 units/sec, fire every 0.18 seconds and deal 10 damage. Both rifles use those same test values. Shots are pooled, sweep their travel segment to avoid tunneling and check shoulder-to-muzzle clearance to prevent shooting through a nearby wall. They ignore the player and stop on terrain or targets.

Three orange targets show health and hit counts in the HUD; depleted targets recover after 1.5 seconds. The red floor pad deals 20 damage, respecting damage protection. H uses the same protected damage route. Only damage routed through `PlayerSandboxCombat.TakeDamage` receives sandbox invulnerability; this is not an integration with the team's enemy, weapon, ammo or game-state systems.

## Validation

The scripts can be checked with the project's cached Unity compiler response files without starting another Unity editor. Compilation does not verify animation appearance or play feel: use the sandbox in Play Mode to check all aim directions, crouch/slide, double jump, reset and target hits.
