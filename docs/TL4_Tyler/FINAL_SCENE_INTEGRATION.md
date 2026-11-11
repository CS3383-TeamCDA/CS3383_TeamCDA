# Final scene integration

## Starting point

Open `Assets/Scenes/FinalGame.unity`. This is the integration candidate, copied
from GeneratedWorldGameDemo so the existing menu, HUD, player input, camera,
world generation and power-up configuration are retained. The source demo is
unchanged. FinalGame is listed but disabled in Build Settings until verified.

Tyler is the initial scene integration owner; CODEOWNERS now covers this exact
scene and its meta file. This does not change the repository's branch protection
settings or give other teams permission to edit each other's implementations.

## Scene composition

| System | Scene source / owner | Current status |
| --- | --- | --- |
| Main menu and HUD | TL1 prefab instances / Chris | Inherited from integrated demo |
| Game state and dependency scope | ServiceContainer prefab / composition owner | Resolves TL1 manager and legacy bridge at runtime; registers scene TL4 spawner |
| Terrain, routes and camera | Existing TL2 setup / Caleb | WorldNetworkDemo and StreetView retained; legacy disabled terrain setup remains |
| Player and input | PlayerTL6 prefab / Matteo | Existing integration assigns input and ground settings |
| Power-ups | Existing PowerUpSystem prefab / Joe | Existing generated pickups and drops retained |
| Enemy system | TL4EnemySystem prefab / Tyler | One EnemySpawner with EnemyGameStateBinding and Ground/Flying/Turret references |
| Combat input | TL4CombatInput prefab / Tyler | Existing click-to-delete attack retained as temporary combat input |
| Meta progression, health/score HUD and game over | Team contracts / respective owners | End-to-end wiring still needs completion and verification |

The generated-street adapter owns enemy placement and calls the manual spawner
factory. Therefore TL4EnemySystem has automatic spawnRate zero and no spawn
points; enabling the old GeneratedTerrainEnemyPopulation would introduce a second
population path. That disabled component was removed from the new enemy prefab.
EnemyGameStateBinding gates automatic spawning; TL2 gates initial world generation
through the existing session-state bridge. Do not remove that bridge yet.

The existing generator still configures the spawner's prefab field by reflection
and creates enemies through the concrete factory. This is inherited transitional
integration, not a completed migration of TL2 to TL4 contracts. IEnemySpawner
currently exposes rate/level/reward controls, not the Unity factory API.

## Team workflow

1. Each team changes its own scripts and source prefabs on its normal branch.
2. Review and merge through the existing owner/CI process. Preserve meta files.
3. Tell the integration owner about new required services, serialized references,
   prefab structure changes or contract changes. Include the prefab path, provider
   class, interface and dependencies. Internal implementation changes usually need
   no scene edit.
4. The integration owner pulls merged work, updates FinalGame/Composition as
   needed, runs the combined checks, and submits scene changes separately.
5. Team members may open/play the scene, but should avoid saving incidental
   hierarchy or Inspector changes. Make persistent feature changes in the source
   prefab; avoid scene overrides that mask source-prefab updates.

CODEOWNERS requests reviews; actual enforcement depends on existing repository
branch protection. No pushes, PRs or protection-setting changes were made here.

## Before promoting the build entry

- Run existing EditMode and PlayMode tests, including TL4 contract/state tests.
- Open FinalGame and check for missing scripts/references and dependency errors.
- Start from the menu: player input and terrain initialize; generated enemies and
  pickups appear; camera follows the player.
- Check click attacks, enemy melee/projectiles, drops and chunk cleanup.
- Pause/resume: movement, attacks and automatic spawning stay frozen while paused.
- Complete health/death → GameOver, game-over UI, HUD health/score/currency and
  TL5 reward wiring with the corresponding owners. These are not certified working.
- Check restart from pause/game over and return-to-menu/start, including clean
  score/currency/run state. In particular, returning to menu does not itself reload
  the scene; confirm or implement the desired new-run reset across all systems.
- Once these pass, enable FinalGame as the first build scene and disable the demo.

## Verification of this change

The scene and both extracted prefabs passed YAML parsing, unique object-ID checks,
local-reference resolution and checks of newly introduced external asset references.
No C# scripts were changed. These checks do not replace Unity importing and playing
the scene. Unity runtime and Test Runner verification remain pending.

## TL3 power-up migration

FinalGame inherits PowerUpSystem's definition array directly from Joe's source
prefab. Its TL4 copied-definition overrides were removed. Joe controls pickup
prefabs, visuals, effects and manager settings. Enemy drops and generated pickups
now call IPowerUpSpawner. Runtime enemies and GeneratedStreetContent receive the
service through the scene DependencyScope.

Generated pickups are now owned and cleaned up by TL3's manager, not registered as
terrain chunk children. The current void SpawnPowerUp contract does not expose
handles or chunk-release operations. Chunk/route-specific cleanup would require
an agreed extension from TL3; this integration no longer inspects spawned pickups,
changes their sorting order, or changes private despawn settings by reflection.

The obsolete TL4 GameDemoPowerUps copies have been removed. The three older demo/
test scenes that referenced them now inherit Joe's source PowerUpSystem definitions
as well. The player-effect adapter remains until TL3/TL6 provide their shared
target/player contracts.
