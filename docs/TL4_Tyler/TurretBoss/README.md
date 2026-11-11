# Turret Boss / Siege Core — isolated prototype

Open `Assets/Scenes/TL4_Tyler/TurretBoss/TurretBossSandbox.unity` and press Play.
This prototype is separate from the earlier ground-boss sandbox and FinalGame.

| Controls | Action |
| --- | --- |
| A / D | Move |
| Space | Jump while standing |
| S / Down | Crouch (actually shrinks collider and preserves feet position) |
| J | Debug hit for 50 damage; works only during exposed-core recovery |
| Escape | Pause/resume |
| R | Rebuild arena and reset fight/player |

The cyan rectangle is a local sandbox player using existing PlayerHealth. No TL6
movement or crouch scripts are edited. All warnings, projectiles, hit regions and
explosions are simple colored prototype geometry; the boss uses enlarged existing
turret art. It is not yet the rendered Siege Core artwork.

## First attack cycle

1. Floor port warning (1 second), then three low horizontal bolts: jump.
2. Alternate payload each cycle: first three grenades, next three vertical missiles,
   then repeat. Grenades mark the player's position, wait 0.8 seconds, lob for 1.2
   seconds and explode for 0.35 seconds. Move off the two-unit landing region.
   They do not track after the warning begins.
   Each missile launches vertically for 0.65 seconds, then displays a one-second
   landing warning. The yellow marker follows the player for the first 0.6 seconds,
   then turns red and locks for the last 0.4 seconds. It descends at that fixed X
   over 0.45 seconds and explodes in a 1.5-unit-wide region for 0.35 seconds.
   Missile descent is visual; damage comes from the ground explosion. All three
   missiles finish before beam charging starts.
3. Fixed-height cannon: 1.5-second line telegraph, then 0.7-second damaging beam.
   Its region spans y=0.95..1.55; crouched player's top is about 0.7 on flat ground.
4. 2.5-second recovery: armor opens (pink tint) and debug hits can damage the core.
5. Repeat; death clears active hazards and plays the existing turret death animation.

Bolts sweep their frame travel to detect hits. Each bolt, grenade explosion or
beam can damage the player once. Warnings do no damage. Clearing a track cancels its own damage; full encounter
cleanup cancels both body and launcher tracks. The cannon waits for payloads to
finish, while mobile stomp attacks may overlap launcher attacks. Pause
uses scaled time; restart destroys the old arena and hazards before creating new
ones. Victory/retry and HP are displayed in the sandbox HUD.

Boss prefab starts at 600 HP with +100 per additional level. Ranged damage starts
at 16 with +2 per level. Select the sandbox root before Play to change bossLevel.
The shared EnemyBase.TakeDamage entry point is now virtual so the turret can
reject closed-armor hits even when callers hold an EnemyBase reference. Ordinary
enemies retain the same implementation.

## Files and boundaries

- Scripts: `Assets/src/TL4_Tyler/TurretBoss/`
- Boss prefab: `Assets/prefabs/TL4_Tyler/TurretBoss/TurretBoss.prefab`
- Scene: `Assets/Scenes/TL4_Tyler/TurretBoss/TurretBossSandbox.unity`
- Tests: `Assets/tst/TL4_Tyler/PlayMode/TurretBossTests.cs`

This fight is not registered as IBossEncounter yet; the existing controller is
still typed to the earlier BossEnemy prototype. Final integration will need a TL4
encounter adapter to expose this fight without editing Caleb's implementation.
No scene/build/Composition changes outside TL4 were made.

## Mobile phase two (forced on for testing)

The first nonlethal exposed-core hit that brings health to 40% or below starts a
single two-second transformation. Hazards clear, damage is blocked during the
transition, the turret rises and four articulated placeholder legs unfold.
A lethal hit still kills directly instead of forcing an extra life/health gate.

Mobile loop: one-second marked lunge warning → fixed three-unit lunge over 0.6
seconds → 1.2-second leg-spin wind-up → low spin advancing two units over one
second → stomp warning and shock waves, with an independent three-shot launcher
barrage → wait for payload completion → planting → beam → exposed core. Jump over
the spinning legs; the taller lunge requires distance or a higher jump.

The launcher alternates missile and grenade barrages each mobile loop. Its own
clock, telegraphs, projectile visuals and hit tracking survive body-state changes.
A small rear module flashes/recoils independently of the leg animation. Production
launcher animations can subscribe to TurretBossLauncher.OnStageChanged without
replacing the body animation. The existing turret still supplies placeholder art.

Spin and cannon never run simultaneously. The cannon cannot start charging while
the launcher is active. Transformation, death, disable and sandbox restart cancel
both tracks. Floor bolts do not return after transformation. Each barrage resets
to three payloads. Cannon direction is selected when its state begins.

The boss body does not physically push/trap the player during mobile movement;
lunges use swept damage checks instead. Each lunge, spin and stomp attack hits at most
once. Movement stays within the sandbox bounds. Mobile motion uses a kinematic
body on the flat test arena, not terrain-aware navigation. Death retracts the
placeholder legs and restores a dynamic body so the wreckage can settle.

Next: balance phase two in the sandbox, replace placeholder geometry with Siege
Core art/leg animation, then add the seeded chance. Phase two is currently always
available rather than random; no FinalGame/Caleb registration was changed.

## Verification

Source and TL4 tests pass compile checks using Unity's cached compiler references.
Scene/prefab local IDs and references pass structural checks. New regression tests
cover attack ordering, core invulnerability/exposure, death cleanup, standing versus
crouched beam hits, alternating grenade/missile cycles, missile target lock and
dodge behavior, single-hit damage and hazard cancellation. Phase-two tests cover
threshold/transition immunity, bounded lunges, jumpable stomp damage, two full
mobile loops with completed barrages, planting before beam and no return to floor
attacks. Independent-track tests verify payload survival across body changes,
explicit cancellation, and jumpable single-hit spin damage. Runtime Test Runner
and visual playtesting remain to be run in Unity.

## Boss health overlay

The sandbox now binds an encounter-owned `TurretBossHUD` after `boss.Begin`.
A wide bottom overlay shows SIEGE CORE, HP, phase and armor/core status. Purple
health turns teal during exposed recovery; orange damage trails wait 0.45 seconds
then drain at 35% of the bar per second. Scaled time freezes the trail on pause.
The white 40% marker uses the controller's shared transformation threshold and
hides once phase two starts. Victory retains the empty bar; arena restart removes
and rebuilds the overlay. Existing upper-left text remains sandbox diagnostics.

For another TL4 encounter host, add the HUD to an encounter-owned object and call
`Bind(controller)` after `Begin`. No EventSystem or input interception is needed.
This is a runtime uGUI overlay; no changes to TL1 HUD or shared scenes are needed.
Compile checked against cached Unity assembly references; visual layout and runtime
behavior still require Play Mode verification in the turret sandbox.

## Movement and launcher tuning update

Beam facing is selected on charge entry and held through firing, including the
sprite's late-frame facing. Crossing behind the boss does not redirect the shot.
Mobile attack selection now charges only within 3.5 horizontal units (editable on
the controller), with a four-second charge cooldown. The warning is 0.45 seconds
and the three-unit dash takes 0.3 seconds. Otherwise the boss selects spin;
a nearby player can also trigger charge during spin wind-up. Active spin, beam,
transformation and core recovery are never interrupted by proximity charges.
Spin commits to the player's direction at wind-up and travels at 4.5 units/second
until reaching the arena edge (-8.4 / 3.3 body-center X) or four seconds. It does
not reverse when the player crosses behind it. These limits leave leg clearance
inside the sandbox walls. Movement bounds still belong to this flat sandbox.

The independent launcher module follows the boss including its raised height.
Grenades and missiles snapshot its launch position on firing, so payloads begin
at the current boss rather than the old hardcoded X and do not get dragged by
subsequent movement. Landing warnings retain their existing lock rules.
Updated regression coverage checks beam direction across a target crossing,
longer bounded spin travel, and fixed grenade launch origins after boss movement.
Source/test compilation passed; runtime tests and visual playtest remain pending.

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
