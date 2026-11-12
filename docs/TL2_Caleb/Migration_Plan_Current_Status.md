# TL2 Migration Plan: Current Status

**Updated:** 2026-11-11

## Local checkpoint through C02b (2026-11-12)

- C01 addressing coverage and C13a existing-generator coverage are verified. C02b makes `IWorldProfile` extend `ICellSource`; `SideScrollerWorldProfile.IsFilled` reads final stamped tiles, and `CoordinateChunkView` uses `DualGrid.MaskAt` with that profile. Ground occupancy lookup is allocation-free and remains in pure Core.
- Fresh Unity script compilation completed with zero errors or warnings. The full EditMode suite passed 105 cases, with zero failures, skips, or inconclusive cases. All 11 C02b cases passed; C13a's pinned coordinate-window hash remains `3B13AA50F0DB0C56`.
- The replacement enemy lifecycle fixture now imports `EscapeThe90s.Contracts`. Its Play Mode behavior is not verified by the EditMode run; it still targets graph/StreetView assumptions rather than coordinate streaming.
- Remaining Unity verification: coordinate rendering/collider seams, street compatibility smoke check, streaming stress, and the appropriate enemy lifecycle Play Mode checks. Earlier verification notes below describe historical runs.
- The living implementation plan is outside this Git repository. This checkpoint does not complete additional plan tasks.

## Work completed

- **M0–M3:** Kept determinism checks, organized fixtures and diagnostics, and introduced the world-generation assembly boundaries.
- **M4–M5:** Split region, link, and building generation into smaller stages while preserving generation order and pinned hashes.
- **M6:** Added chunk kinds and terrain stamps, difficulty progression, pooling support, and immutable chunk lifecycle contracts.
- **M7:** Added Unity terrain chunks, rendering and colliders, player-centered chunk streaming, and chunk load/recycle events.
- **M8:** Split the playable world setup into bootstrap, player rig, graph-based route selection, map overlay, camera behavior, and chunk-driven enemy/powerup integration. Cross-team adapters live in the WorldGen Integration assembly.
- **M9:** Moved shared contracts to `Assets/src/Contracts`, reusable dependency and event infrastructure to `Assets/src/Infrastructure`, and game-specific wiring to `Assets/src/Composition`. A general typed event bus is the new frontend; deprecated publisher interfaces remain as compatibility adapters. Archive-only files and unused legacy tile assets were removed.

## Direction in the updated plan

- The user has since superseded the graph-run direction for gameplay: build an endless level from stable **world X/Y chunk coordinates**, with 16×16 tile chunks. Current side-scroller behavior streams along X; Y rows let a future survival world expand vertically. Terrain above the surface is sky/air, below is ground/void, and the generator must derive results from seed plus coordinates.
- The graph and intersection system remains available as a separate compatibility mode, but the default playable flow no longer asks the player to choose a route.
- An infinite explored world map may still be useful later, but it is not the gameplay streamer.
- Keep the **pixel-art, arcade visual language** and preserve the recognizable designs and identities of the current player and enemies. Rebuild their presentation at an appropriate pixel-art resolution; do not carry forward the current overly high-resolution rendering or replace the characters with unrelated designs.
- Use the updated plan's Absolum reference for atmosphere, lighting, layered backgrounds, encounters, and set-piece composition. It does not override the pixel-art requirement. The first two biomes and who owns final art remain open decisions.

The endless run, infinite map, forks, arenas, and visual expansion are new work. They are not part of the reported M0–M9 implementation.

### E01–E02 implementation started

- Added a pure Core `RunPath` that appends connected generated streets and assigns each segment a monotonically increasing global chunk range.
- Added lookups from a global chunk index to its street segment, street-local chunk index, and traversal direction. Reverse traversal maps chunk indices from the far end of the street.
- Added `RunPath.TryGetPosition` and `RunProgress` for converting street-local X into forward-only distance and tracking the furthest run position. Difficulty is derived from that monotonic distance.
- Added `RunPathSelector` for seeded deterministic next-street choices. It excludes subway links and immediate backtracking, prefers exits farther from the run origin when possible, and avoids recently used street IDs when alternatives exist.
- Added E01/E02 EditMode coverage for reverse chunk mapping, continuous segment ranges, rejecting disconnected paths, zero-distance start progress, monotonic progress, and deterministic no-backtrack route selection.
- The route-choice run prototype is superseded by coordinate-driven gameplay. Core now defines a signed `WorldChunkCoord` with a 16-cell size and a deterministic `CoordinateWorldGenerator`; Unity now has a target-centered `CoordinateWorldStreamer` that builds and retires chunks in X/Y. The Integration assembly adapts existing enemy and powerup managers to coordinate chunk load/unload lifetime.
- The playable demo defaults to coordinate mode and starts at world position `(0, 3)`. It keeps the team's player controller and camera setup; graph mode remains available as a compatibility option. The old StreetView teardown fix destroys pooled chunks in place during owner destruction instead of reparenting into a dying object.
- Coordinate terrain now uses a pure `IWorldProfile`/`SideScrollerWorldProfile`, signed 16×16 addressing, deterministic surface variation, `Sky`/`Solid`/`Surface`/`Void` classification, and a world-floor boundary. Surface chunk data contains `TileId`s and deterministic enemy/pickup spawn locations.
- Biomes now use a seed-stable nearest-site field along world X, with adjacent regions choosing distinct palette biome IDs. The terrain defaults have somewhat more frequent, still gentle height variation. Surface chunks place generic and pizza-parlor city art at deterministic world positions, and that decor recycles with its owning chunk.
- `WorldNetworkDemo` now has an Inspector debug checkbox, **Show Chunk Debug**, available before Play Mode and live while playing. It draws loaded chunk boundaries and `(X, Y)` labels, with colors for Surface, Sky, Solid, and Void. The Scene view shows it directly; enable **Gizmos** in the Game view to see it during Play Mode.
- Streaming now uses a speed-aware asymmetric X window, vertical radius, build budget, hysteresis, pooled surface views, and coordinate-aware `ILevelProgress` events. Uniform sky/solid/void chunks do not allocate views. Distance and per-chunk difficulty are wired to the existing progression service.
- **C03 terrain-stamp slice:** `CoordinateTerrainStampField` overlays deterministic city blocks on the seeded coordinate surface. Each three-chunk block has a flat center lot sized for the building footprint and integer-interpolated ramps that reconnect to the surrounding surface. Settings expose block length and flat width; generation remains seed- and coordinate-deterministic.
- **C06 building placement:** Building anchors only appear at city-lot centers. Their world position uses the terrain collider's top surface (`surface + 0.5`), and the adapter offsets by half the sprite height so each building base rests on the ground. Spawned content remains owned by its source chunk and retires when that chunk unloads.
- Added focused C03/C06 tests for flat lot footprints, one-cell maximum surface steps, and deterministic building anchors on flat terrain. The tests compile but have not been run in Unity.
- `GeneratedCoordinateContent` subscribes to the coordinate lifecycle contracts and destroys objects with their owning chunk. A typed `IWorldContentSpawnAdapter` isolates the existing TL3/TL4 compatibility reflection in `LegacyWorldContentSpawnAdapter`; replacing that bridge requires stable consumer contracts from those feature owners.
- Preserve the pixel-art presentation and current player/enemy identities while expanding coordinate generation. Terrain stamps, broader prop variation, art polish, and integration tests remain open.

**Verification:** Unity 6000.6.0f1 refreshed and compiled the changed project assemblies. The first compile found and led to a fix for an unsigned-modulo type mismatch in `CoordinateWorldGenerator`; the following compiles regenerated Core, Unity, Integration, Demo, and WorldGen.Tests assemblies with no C# errors. A brief Play Mode start visibly showed different biome palette colors and generated city buildings. The coordinate tests compile but have not been run; movement, chunk-debug display, and streaming stress checks remain.

### Chunk visualization, biomes, and city dressing

- Added an Inspector debug toggle to the coordinate streamer. It draws loaded chunk outlines and coordinate/type labels through Unity Gizmos.
- Added deterministic X-axis nearest-site biome regions and palette-row selection from the existing five-biome palette. Biome IDs now travel in generated chunk data and lifecycle events.
- Added deterministic world-anchored Generic/PizzaParlor building spawns using the existing WorldArt sprites. Buildings are owned by their source chunk and retire with it.
- Increased default surface variation slightly and shortened the anchor span to make the ground less uniformly flat while keeping smooth, gradual ramps.
- Added C05 tests for deterministic biome assignments, biome boundaries, and consistent biomes across vertical chunk layers.

**Verification:** Unity compiled the visual/content changes with no C# errors. I entered Play Mode and confirmed that the generated view shows different palette colors across biome boundaries and creates the Generic/PizzaParlor building objects. I did not exercise the chunk-debug toggle or run the new tests. To use the toggle, select the `WorldNetworkDemo` component, check **Show Chunk Debug**, and enable the Game view's **Gizmos** button for outlines there.

## Playable-world integration fixes

- The generated-world scene now includes the service container and HUD so game-state changes dismiss the menu and allow gameplay to begin.
- TL2's `Controls` input actions were restored under the active TL2 folder, retaining the existing asset GUID and the `InGame` map expected by the player prefab. The generated-world scene uses that asset.
- A stale `_inputHandler` scene override pointed to a missing object ID. It was removed from `EnemySpawningMvp`; the player prefab already has its own valid handler reference.
- World generation is a graph of finite streets connected at intersections. It is not one endless street. Chunks stream as the active street is traversed; another street is selected at its endpoint.

## Enemy test migration

### Retired test

`MvpSpawnsAnimatedEnemiesOffCameraAndRetiresUnloadedTerrain` loads `EnemySpawningMvp` and asks TL4's `GeneratedTerrainEnemyPopulation` to scan a legacy collision Tilemap. That scene's serialized Tilemap is empty, and the old generator that filled it is no longer part of the active setup. The test is marked ignored with that reason; its original code is retained for reference. It tested timed off-camera spawning, mixed enemy types, animation assets, pursuit, and cleanup on terrain unload.

### Replacement test

`GeneratedWorldSpawnsMixedEnemiesAndRetiresThemWithUnloadedChunks` loads `GeneratedWorldGameDemo`. It checks that current chunk content creates ground, flying, and turret enemies through the existing TL4 `EnemySpawner`, verifies an animated ground enemy, then checks that enemies are destroyed when their owning terrain chunks unload.

The replacement does not assert off-camera timed spawns: the active generated-world integration places enemies from each chunk's generated spawn data as that chunk loads, rather than using TL4's legacy timed spawn-point scan.

The first replacement versions failed to observe chunk unloading because the production network creates short 4–6 chunk streets; moving to their midpoint does not necessarily exceed the streamer's unload hysteresis. The current test version sets the referenced settings to a 14-chunk street at runtime, moves a controlled stream target to the local midpoint, and restores the original settings during teardown. This changes only the test's runtime configuration, not production street length.

**Current verification:** The prior 4–6 chunk versions failed at the unload assertion. The 14-chunk adjustment has not yet been rerun in Unity, so cleanup is still unverified by the test runner.

## Remaining work and decisions

- Run the new coordinate EditMode tests and PlayMode-check the forward-run demo in Unity. Rerun the replacement enemy lifecycle test with its test-only 14-chunk street; if it fails, inspect loaded chunk indices, stream target position, and recycle callbacks before changing world generation.
- Confirm chunk seams/colliders as the player crosses positive and negative X/Y coordinates; stress streaming at high speed and verify recycling and pool reuse.
- Extend the initial city-lot stamp with stairs, gaps, platforms and overpasses, add broader prop variation, then replace the legacy reflection bridge when TL3/TL4 provide stable Contracts interfaces.
- Decide whether the old graph route mode should remain as a developer-only compatibility option after the coordinate world is accepted. The graph run, forks, and exploration-map tasks are not the active gameplay direction.
- Confirm the main mode and hub decision; plaza-walk-out versus top-down intersections; player capability contracts; TL3/TL4 contract adoption versus adapters; TL5 save ownership; two initial biomes; and final-art ownership.
- Keep acceptance focused on the complete playable game and its full run loop; “Demo” assembly and scene names are historical.
