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

The animated enemy prefab includes GroundEnemy with scalable stats and attack mode Both. Enemy movement, damage and trigger calls still need gameplay integration. The projectile prefab moves along its launch direction, carries the owner and damage snapshot, and expires after its configured lifetime; collision damage is not yet connected.

## Art source and validation

Art was created with the built-in image generation tool using the earlier alien atlas as the identity reference. Prompt brief: preserve purple alien, teal armor and red goggles; produce eight distinct poses each for idle, walk, run, shoot, melee, hit and death on separate transparent sheets, each with four columns and two rows; keep generous padding, consistent scale and right-facing orientation, with no labels or borders. These are AI-generated intermediate poses, not pixel-exact edits of the original artwork.

Assets were imported and checked in Unity 6000.6.0f1 in an isolated project. Validation checks frame presence and animation sprite references. In-game motion and transitions still need playtesting with the enemy controller.

## Enemy stats

Select the prefab's GroundEnemy component to edit Level, Attack Mode, Base Stats, and Added Per Level. The formula is `base + (level - 1) * perLevel`.

| Stat | Level 1 | Added per level |
| --- | ---: | ---: |
| Health | 100 | 20 |
| Coin reward | 5 | 2 |
| Melee damage | 10 | 2 |
| Ranged damage | 8 | 2 |

These are editable starting values. `SetLevel(level)` preserves current health percentage and does not revive dead enemies. `EnemySpawner.SetSpawnLevel(level)` sets difficulty for future spawns before OnEnemySpawned fires.

`MovementType` is Ground, Flying or Static, determined by the component class (TurretHazard and AimEnemy are static). The existing Type values Ground/Flying/Turret/Aim remain intact for spawn slots and events. `SetAttackMode` selects MeleeOnly, RangedOnly or Both; CanMelee/CanRanged expose capability and disabled attacks return zero damage. Attack mode defaults to Both.

Read CurrentHealth, MaxHealth, CoinReward, MeleeDamage and RangedDamage for gameplay/UI. A Defeated listener can read CoinReward when awarding coins; this change does not credit a wallet, perform attacks or drive the Animator automatically.

## Shooting direction

TurretHazard fires at exactly five world-space angles: 0 (right), 45, 90 (up), 135, and 180 (left). It picks the closest angle to the target relative to the muzzle; halfway angles round upward. Targets below the muzzle and targets exactly at the muzzle are rejected. Rotating the turret does not rotate this world-space arc.

GroundEnemy fires horizontally in its displayed facing direction, even if the supplied target is above, below or behind it. Call SetFacingRight(bool) to turn it; root SpriteRenderer flipX and mirrored transforms are respected. Shooting does not automatically turn it.

EnemyRangedAttack owns the projectile prefab, optional muzzle, target, shot interval, speed and lifetime. The ground prefab has this component and its projectile assigned. Assign Target in the Inspector or call SetTarget(transform) for automatic repeated fire; leave it empty to call TryShoot(targetPosition, out projectile) manually. Attach the same component to a turret and assign its projectile and muzzle to use the five-angle policy. No player auto-discovery or range/line-of-sight filtering is included.

Melee-only and defeated enemies cannot shoot. Projectiles capture RangedDamage at launch, so later level changes do not change shots already in flight. Collision damage, firing animation timing and target acquisition remain gameplay integration points.

## TL2 room generation and off-screen spawning

TL2 LevelGenerator now gathers EnemySpawnPoint components from its generated entrance, middle and end chunks. Its Difficulty Level controls newly spawned enemy stats; Enemy Spawn Rate is enemies per second, and Max Enemies Alive caps the population. Changing difficulty affects future spawns. A missing gameplay camera prevents spawning.

To author locations, drag GroundEnemySpawnPoint.prefab into a TL2 room prefab at an intentional ground-enemy feet position. The marker references the animated ground prefab. For other enemies change Enemy Type and assign a matching Enemy Prefab. Position flying markers in open air and turret markers on their intended mounts. No markers have been inserted into the existing room layouts automatically.

The cyan gizmo is the world-space clearance box: adjust Clearance Size and Offset to contain the entire enemy. The complete box plus Screen Margin must be off-screen (left, right, above or below). Active enemies overlapping it prevent reuse. Optional Blocking Layers rejects geometry inside that box; choose only solid obstacle layers, leaving the supporting floor outside its interior. With no blocking layers selected, placement validity comes from the room designer. This is not automatic ground/navmesh detection.

LevelGenerator registers markers when it generates the rooms; disabled markers remain unavailable until enabled. If no marker is eligible, spawning waits rather than creating an enemy on-screen or at an arbitrary location. Spawned enemies are parented under their room so regenerating/destroying the level cleans them up.

The explicit CreateEnemy API remains an unrestricted factory for existing demos/tests. Gameplay should use the registered marker schedule or TrySpawnAt to enforce location, visibility and population checks.
