# TL2 terrain generation changes

## Summary

The first TL2 terrain system generated simple grass and dirt tiles at varying ground heights. The current system generates a deterministic, playable street world: a region graph determines where streets and intersections connect, each street receives a biome and authored buildings, and terrain art, collision, enemies, and pickups load as the player moves.

The original code remains in `Assets/_Archive/TL2_Caleb_Legacy`. Current TL2 source is in `Assets/src/TL2_Caleb`.

## Planner breakdown

- [x] Replace height-varied grass/dirt-only output with seeded street and region generation.
- [x] Generate connected street graphs, intersections, route choices, and subway/arcade points.
- [x] Add biome rules, authored buildings, palette artwork, terrain cells, and terrain-derived colliders.
- [x] Render streets in Unity and stream terrain chunks around the player.
- [x] Make generated streets playable, shorten their lengths, and bind route choices to the exact endpoint.
- [x] Stream enemies and powerups with terrain; expose ground, flying, and turret enemy types.
- [x] Integrate the existing menu, player, enemy, and powerup systems in a separate generated-world scene.
- [x] Delay world creation until Start Game and bound the camera to each generated street.
- [x] Keep the previous source tree and scene usable, document AI-assisted work, and provide a terrain comparison presentation.

## Changes since the first terrain generator

### From terrain chunks to a generated street world

- Replaced the main generation flow with deterministic region, street, and intersection data.
- Added stable IDs and seeded random streams so a region or street can be rebuilt consistently without depending on load order.
- Generated connected street edges and graph intersections, including connections that cross region borders.
- Added nearest-arcade lookup to select a biome for generated streets.
- Shortened playable streets to 4–6 chunks in the integrated demo.
- Added per-biome rules for buildings, accents, density, enemy counts, enemy kinds, and required buildings.
- Added authored arcade, pizza parlor, and generic building art, with palette rows for biome and accent variants.
- Built street ground tiles from a continuous cell grid using dual-grid masks, with optional curbs and steps. Generated collider boxes come from the terrain data rather than the visual tile edges.
- Added deterministic enemy slots and street-level powerup placement. Both enemies and pickups are tied to loaded terrain chunks.

### New playable demo behavior

- The route chooser lists only streets connected to the exact street end the player reached. The world mini-map shows surrounding graph streets, arcades, and subway stations.
- The integrated demo retains the existing TL1 menu/HUD, TL6 player, TL4 combat/enemy system, and TL3 powerup manager. `GeneratedPlayerPowerUpTarget` connects generated-demo pickups to the player without changing TL3 or TL6 source.
- The generated world now waits for the menu's `PlayingState` before creating the street, player, enemies, or pickups. The pre-existing player is hidden while the menu is open, and `StreetView` is disabled until play begins.
- Ground, flying, and turret enemies all appear in the demo. Their spawn slots cycle through the three available TL4 enemy kinds. Flying enemies start above the street.
- Enemy and pickup objects unload with their terrain chunks. Enemies receive the player as their target.
- The demo uses a pixel-snapped follow camera bounded to the current street's world-space ends. It disables the unbounded Cinemachine brain while this generated-world scene runs.
- The Unity Console identifies the AI-assisted street rendering call with `[AI demo] EscapeThe90s.WorldGen.StreetView.ShowWorldStreet`.

## Key classes and functions

### Generation and graph

- `WorldNetwork` in `Scripts/WorldGen/Core/WorldNetwork.cs`: `GetStreet`, `GetIntersection`, `GetNearestArcade`, and `TryGetNextIntersection` provide the generated network and its route choices. It implements `ILevelQuery` and is the single seeded entry point for cached street layouts, biome rules, and difficulty tiers.
- `RegionMap` in `Scripts/WorldGen/Core/RegionMap.cs`: `GetStreets`, `RegionAt`, `GetRegion`, and `GetLink` generate and query seeded regions.
- `RegionStreets` and `RegionStreetBuilder` in `Scripts/WorldGen/Core/RegionStreets.cs`: represent and build connected street edges for a region.
- `StreetGenerator` in `Scripts/WorldGen/Core/StreetGenerator.cs`: `Generate` creates a street layout; `GenerateChunk` creates deterministic buildings, variants, accents, safe ends, and enemy slots.
- `Difficulty` in `Scripts/WorldGen/Core/Difficulty.cs`: `TierAt` provides distance-based difficulty tiers.
- `StableId`, `WorldHash`, and `DeterministicRng` in their same-named Core files: create stable region/street IDs, combine hashes, and supply seed-based random values.
- `BiomeRules` and `BuildingRule` in `Scripts/WorldGen/Core/BiomeRules.cs`: configure the generation data for buildings and enemies.
- `StreetRequest`, `StreetLayout`, `ChunkData`, and `EnemySlot` in `Scripts/WorldGen/Core/Types.cs`: carry generation requests and results.

### Terrain, art, and Unity rendering

- `StreetTerrain` in `Scripts/WorldGen/Core/StreetTerrain.cs`: provides the street's filled-cell and height data.
- `DualGrid` in `Scripts/WorldGen/Core/DualGrid.cs`: converts terrain neighborhoods into tile-sheet masks.
- `TerrainColliders.ForChunk` in `Scripts/WorldGen/Core/TerrainColliders.cs`: creates collider geometry from terrain cells for a chunk.
- `ChunkWindow` in `Scripts/WorldGen/Core/ChunkWindow.cs`: finds the player-centered chunk window and decides when chunks load or unload.
- `WorldGenSettings` in `Scripts/WorldGen/Unity/WorldGenSettings.cs`: stores the seed, biome assets, and minimum/maximum network street lengths; `CreateWorldNetwork` builds the graph service.
- `BiomeDefinition` in `Scripts/WorldGen/Unity/BiomeDefinition.cs`: stores a biome's generation rules as a ScriptableObject.
- `WorldArt` in `Scripts/WorldGen/Unity/WorldArt.cs`: maps biome palettes, dual-grid ground tiles, and building types/variants to Unity assets.
- `StreetView` in `Scripts/WorldGen/Unity/StreetView.cs`: `ShowWorldStreet(WorldStreet street, bool reverse)` renders a graph street; `RegisterChunkObject` attaches spawned content to chunk lifetime; `IsChunkLoaded` reports load state; `TryGetWorldXBounds` supplies camera limits.
- `StreetPreview` in `Scripts/WorldGen/Unity/StreetPreview.cs`: editor-facing street preview component.

### Demo and team integration

- `WorldNetworkDemo` in `Scripts/WorldGen/Demo/WorldNetworkDemo.cs`: waits for gameplay to start, creates the world/player, transitions between streets, draws the graph and route picker, and synchronizes enemies and powerups with streamed chunks. Important functions include `TryInitializeAfterGameStarts`, `IsGameplayStarted`, `CreateDemoBiomeRules`, `EnterStreet`, `EnsureAllDemoEnemyKinds`, `SyncStreetEnemies`, and `SyncStreetPowerUps`.
- `CameraFollow` in `Scripts/WorldGen/Demo/CameraFollow.cs`: follows the player, snaps the camera to pixel increments, and clamps it to `StreetView` bounds.
- `DemoWalker` in `Scripts/WorldGen/Demo/DemoWalker.cs`: lightweight movement controller for standalone world-generation scenes; the integrated demo uses the existing team player.
- `GeneratedPlayerPowerUpTarget` in `Scripts/WorldGen/Integration/GeneratedPlayerPowerUpTarget.cs`: implements the powerup target adapter. `SetSpeedMultiplier`, `Heal`, and `SetPickupRadius` forward powerup effects to the player.
- `WorldNetworkDemoSetup` in `Scripts/WorldGen/Demo/Editor/WorldNetworkDemoSetup.cs` and `IntegratedWorldDemoSetup` in `Scripts/WorldGen/Demo/Editor/IntegratedWorldDemoSetup.cs`: create standalone and integrated demo setups. `CreateIntegratedDemo` copies the existing team demo scene before adding the generated-world driver.
- `ShowcaseSetup` in `Scripts/WorldGen/Demo/Editor/ShowcaseSetup.cs`: creates the original visual showcase objects for Scene view inspection.
- `RegionMapWindow`, `RegionMapRaster`, `WorldArtSetup`, and `IndexTextureImporter` provide editor map visualization, raster output, art asset setup, and indexed texture import.

The code is separated into `EscapeThe90s.WorldGen.Core`, `EscapeThe90s.WorldGen.Unity`, `EscapeThe90s.WorldGen.Editor`, `EscapeThe90s.WorldGen.Integration`, and `EscapeThe90s.WorldGen.Tests` assemblies using the matching `.asmdef` files. The powerup adapter stays in the TL2 integration assembly; the existing TL1, TL3, TL4, and TL6 source files were not changed for this integration.

## Migration plan foundation work

The first implementation tranche from the partial completion and migration plan improves test organization and assembly boundaries without changing world-generation output:

- Moved `DeterminismChecks` from `Core` into `Tests`, and exposed one check per generation concern while retaining the original combined test and golden-hash checks.
- Added named EditMode fixtures for deterministic RNG and IDs, street generation, terrain and chunk windows, region maps, and world networks.
- Moved the region-map raster renderer out of `Core` and into `Editor`, keeping editor visualization code out of the engine-independent generation assembly.
- Added dedicated runtime and editor assemblies for the demo, and placed demo scripts and setup utilities in `EscapeThe90s.WorldGen.Demo` namespaces. The integrated scene's serialized component identifier was updated to match.

This tranche does not change pinned generation hashes or add new cross-team contracts. The Unity EditMode suite was run in the Editor and reported passing.

## Code quality refactor (M4–M5)

- Consolidated seeded layout queries, biome-rule lookup, and distance-based difficulty into `WorldNetwork`, which implements `ILevelQuery`. Removed the separate `LevelService`; the settings asset now creates the same world entry point for both graph streets and standalone previews.
- Split `RegionStreetBuilder.Build` into grid setup, terminal collection, arcade selection, network connection, densification, and output steps.
- Split region link generation into surface-link selection and forced-subway handling, and split building placement into required, optional-special, and filler phases.
- Kept random draws and their ordering in the same generation phases so this refactor does not intentionally alter generated output.

The legacy hash checks still pass after M4–M6; the new metadata has a separate generation hash. The plan's plaza redesign and save/composition decisions remain open. Unity GameObject pool integration and terrain-stamp rendering are M7 work.

## Core foundations (M6)

- Added `StreetKind`, `ChunkKind`, `TerrainStamp`, and deterministic stamp selection. Street kinds map to the already-configured short street-length range so the playable demo does not unexpectedly return to long streets. Stamps are bounded to chunk-local cells; gap width is a conservative placeholder until TL6 jump capability is available.
- Added `IChunkGenerator` and `ChunkSpec`. `StreetGenerator` now produces chunk metadata and spawn locations alongside the legacy `ChunkData`; `ContentHash` remains the pinned legacy hash while `GenerationHash` includes the new metadata.
- Added a reusable engine-free `ChunkPool<T>` keyed by kind with prewarm, reuse, and lifecycle counters. Unity GameObject activation and renderer reset still belong to M7.
- Added smooth, rebasable `DifficultyProgression`, monotonic `LevelProgress`, and immutable `ChunkSpawnedInfo`/`ChunkId` data with once-per-active-load spawn and recycle events.
- Added M6 EditMode fixtures for chunk metadata, pool reuse, progression, and immutable event payloads.

M6 Core code compiles with the .NET compiler, and the engine-free determinism plus focused M6 checks pass. The NUnit fixtures have not been run in the Unity Editor yet. Unity's `StreetTerrain` renderer does not consume the new stamp geometry yet; that wiring is part of M7.

## Main files and assets

### Current world-generation code

All paths are relative to the Unity project root.

- `Assets/src/TL2_Caleb/Scripts/WorldGen/Core/` — deterministic network, terrain, rules, layouts, IDs, hashes, chunk windows, and data types.
- `Assets/src/TL2_Caleb/Scripts/WorldGen/Unity/` — ScriptableObjects and Unity terrain renderer.
- `Assets/src/TL2_Caleb/Scripts/WorldGen/Demo/` — playable graph demo, movement, and camera behavior.
- `Assets/src/TL2_Caleb/Scripts/WorldGen/Integration/` — TL2-owned player/powerup adapter assembly.
- `Assets/src/TL2_Caleb/Scripts/WorldGen/Editor/` and `.../Demo/Editor/` — map, art, import, showcase, and scene setup tools.
- `Assets/src/TL2_Caleb/Scripts/WorldGen/Shaders/PaletteSprite.shader` — palette-based sprite rendering.
- `Assets/src/TL2_Caleb/Scripts/WorldGen/Tests/DeterminismTests.cs` and the neighboring named test fixtures — determinism and system-level generation test sources.

### Generation and art assets

- `Assets/src/TL2_Caleb/WorldGenSettings.asset` — seed, biome list, and street-length settings.
- `Assets/src/TL2_Caleb/Biome.asset` and `Assets/src/TL2_Caleb/Default.asset` — biome/rule data.
- `Assets/src/TL2_Caleb/Art/WorldGen/WorldArt.asset` and `PaletteSprite 1.mat` — art lookup and palette material.
- `Assets/src/TL2_Caleb/Art/WorldGen/ground_dualgrid_index.png`, `generic_building_index.png`, `pizza_parlor_index.png`, and `palette.png` — indexed ground/building/palette artwork.

### Demo and project files

- `Assets/Scenes/GeneratedWorldGameDemo.unity` — integrated generated-world scene; the original `Assets/Scenes/TL5_Will/GameDemo.unity` remains available.
- `ProjectSettings/EditorBuildSettings.asset` — places the generated demo first in the enabled build scenes.
- `Assets/src/TL2_Caleb/AI_Interaction_Log.md` — AI usage record and Unity demonstration function reference.
- `README.md` — scene launch and control instructions.
- `docs/presentations/TL2_Caleb/TL2_Progress_Update.pptx` — short archived-versus-current terrain comparison.

## Archived starting point and preservation

The earlier code remains at `Assets/_Archive/TL2_Caleb_Legacy`. The earlier `LevelGenerator.GenerateLevel`, `ChunkSpawner.SpawnChunkAtCoordinates`, `LevelGen.LevelGenerator.RefreshChunks`, and `LevelGen.ChunkGenerator.GenerateChunk` flow created tile chunks from the simpler height-varied grass/dirt terrain. The archive keeps its scripts and `.meta` files so old scenes and prefabs can continue using the same Unity asset references. Existing TL2 scenes and prefabs were not moved.

## Demo controls

Open `Assets/Scenes/GeneratedWorldGameDemo.unity`, press Play, and then press **Start Game** in the menu. Use **A/D** to move, **Space** to jump, **Up/Down** to select an intersection exit, and **Enter** to take it. Click a route in the graph chooser to select it directly.

## Verification

The current project passed a Unity 6 batch import and C# compilation with no C# warnings or errors. The demo scene has also been confirmed working in Play Mode.
