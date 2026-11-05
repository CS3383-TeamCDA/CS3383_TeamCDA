# Escape the 90s: world generation, Phases 0-2

Contracts, deterministic core, and a seeded street generator. No art needed yet.

## Install
1. Copy `Assets/Scripts/WorldGen` into your Unity project (C# 9 / Unity 2021.3+).
2. `Assets > Create > Escape the 90s > World Gen Settings` (set the seed). Optionally create Biome assets
   (`Create > Escape the 90s > Biome`) and drag them into the settings' list; empty = built-in defaults.
3. Empty GameObject, add **StreetPreview**, assign the settings. Look at the Scene view (Gizmos on).
   Change node numbers / seed / flags and watch the street change; reload and it comes back identical.
4. Window > General > Test Runner > EditMode > run `AllDeterminismChecksPass`.

Outside Unity: `cd Tools/CoreRunner && dotnet run` runs the same checks and prints a sample street.

## Layout
- `Core/` has no Unity references (own asmdef), so it is unit-testable anywhere.
  - `WorldHash`, `DeterministicRng`: integer-only hashing and PRNG. Never use `UnityEngine.Random` for world gen.
  - `StableId`, `Int2`: seed-independent IDs. `ForStreet(a,b)` ignores endpoint order; `ForPair` for tower pairs.
  - `Constants`: chunk width, safe end chunks, min street length, tier distance. **Placeholders; settle before building content.**
  - `Types`: `BuildingType`, `BuildingFlags`, `ChunkData`, `EnemySlot`, `StreetLayout`, `StreetRequest`.
  - `BiomeRules` / `BuildingRule`: per-biome tuning (weights, per-street %, caps, enemy anchors, flip allowed).
  - `StreetGenerator`: `Generate(seed, request, rules)` and `GenerateChunk(...)`.
  - `LevelService` implements `ILevelQuery`, the API teammates call (cached `GetStreet`, `GetBiomeRules`, `GetDifficultyTier`).
  - `DeterminismChecks`: shared test logic.
- `Unity/`: `BiomeDefinition`, `WorldGenSettings` (ScriptableObjects) and `StreetPreview` (gizmo viewer).
- `Tests/`: EditMode wrapper around `DeterminismChecks`.

## Guarantees the checks enforce
- Same seed + request => identical street; different seed => different.
- Order independence: generating streets forward, reversed, or after unrelated streets gives identical results.
- A chunk rebuilt alone from (seed, street id, index, building) equals the street's copy.
- End chunks never hold buildings or enemies; at most one building per chunk; caps and anchors respected;
  special buildings never appear without their flag; required buildings placed (or counted in `DroppedRequired`).
- Pizza parlor rate is about 20% when flagged.
- Golden hash pinned (`DeterminismChecks.GoldenStreetHash`): fails if generation output changes, and in Unity it
  proves the result matches on your scripting backend. If you change rules on purpose, run the CoreRunner and update it.

## How a street is built
1. Required buildings (e.g. the region's arcades) go in random free chunks.
2. Each flagged special rolls once per street (`PerStreetPercent`, up to `MaxPerStreet`).
3. Remaining free chunks may get a filler building (`BuildingDensityPercent`, weighted by `FillWeight`).
4. Per chunk, separate RNG streams (look vs enemies) give variant/accent/flip and enemy slots, so tuning enemy rules never reshuffles visuals.

## Team API
```csharp
var level = settings.CreateService();                       // ILevelQuery
StreetLayout street = level.GetStreet(request);              // chunks, buildings, EnemySlots
int tier = level.GetDifficultyTier(distanceFromOrigin);      // integer; feed into StreetRequest.DifficultyTier
```
Enemy slot `SlotIndex` + street/chunk id is the stable key for persistence later.

## Not done yet (by design)
Gates (`GateKind` reserved, always None), dual-grid renderer + palette shader (Phase 3), chunk streaming (Phase 4),
biome-from-nearest-arcade and region graph (Milestone 2). Minimum street length is clamped to `MinStreetChunks`;
the region layer must request enough chunks for its required buildings (otherwise `DroppedRequired > 0`).

---

# Phase 3: palette shader, dual-grid ground, street view

## Set up (about 5 minutes)
1. Make sure the project uses the 2D template or has the **Tilemap** module (Package Manager > 2D Tilemap is built in on 2021.3+).
2. Copy `Assets/Scripts/WorldGen` and `Assets/Art/WorldGen` into your project. On import, `IndexTextureImporter` forces the
   right settings on `*_index.png` and `palette.png` (Point, no compression, no mipmaps, sRGB off for index art, on for the palette,
   16 PPU, bottom-centre pivot). Don't override them: compression or filtering corrupts the palette lookup.
3. Run **Create World Art From Sprites**. It is in the TOP menu bar (next to Window / Help, a menu called "Escape the 90s"),
   and also under Assets > Create > Escape the 90s. (The plain "World Art" entry next to it only makes an empty asset.) It creates `PaletteSprite.mat` (shader + palette) and a `WorldArt` asset
   wired to the ground sheet and the two buildings.
4. Create **World Gen Settings** (from Phase 2) if you haven't. Empty scene: add an empty GameObject with **StreetView**,
   assign Settings and Art, then right-click the component > **Rebuild** (or press Play).
5. Camera: orthographic, size **11.25** (= 360 / 2 / 16), or add the Pixel Perfect Camera component with Assets PPU 16 and
   reference resolution 640 x 360. Scroll along +X to see the whole street (chunks are 512 px = 32 units wide).

## What is verified and what is not
Verified here (outside Unity): all the logic checks, including the new dual-grid and terrain ones, plus an image rendered from the
real mask output and the real sprites (`Tools/Sprites/render_street.py`, preview image included). Not verified: the shader, importer
and Unity components were only type-checked against stubs, never run in the editor. Expect to fix small things on first import
and tell me what Unity reports.

## How it works
- **Terrain (`StreetTerrain`)** is a street-wide data grid: y = 0 is the surface, everything below is solid, optional curbs/steps
  (up to 2 cells high) appear only in chunks without buildings. Because it is street-wide, a tile on a chunk border just reads
  neighbouring cells, so seams are correct by construction.
- **Dual grid (`DualGrid`)**: render tile (rx, ry) sits on the corner of four data cells; its mask bits are TL=1, TR=2, BL=4, BR=8,
  and the tile sheet is laid out so tile position = (mask % 4, mask / 4 from the top). A flat street needs only tiles 12 and 15.
  A check proves every data cell agrees with the corner bit of its four tiles.
- **Tiles are sliced at runtime** from `ground_dualgrid_index.png` (`WorldArt.GetGroundTile`), so no manual sprite slicing.
- **Palette**: each renderer gets `_PaletteRow = biome * 3 + accent` through a MaterialPropertyBlock. Ground uses accent 0;
  buildings use the chunk's `Accent`.
- **Buildings** stand on y = 0, centred in the chunk, mirrored when `Flipped`. Types without art (arcade, bank, tower...)
  fall back to the Generic sprite and log one warning.

## Known limits
- The shader is unlit (works in Built-in and URP / URP 2D, ignores 2D lights). If you want lit pixel art we add a normal-map-free lit variant.
- One Tilemap per chunk, no colliders yet (the ground needs a collider before the player can walk on it; that comes with Phase 4).
- The whole street is built at once. Load/unload around the player is Phase 4.
- Enemies are not drawn yet; `StreetPreview` still shows their slots as gizmos.

## If the menu item is missing: manual setup (same result)
1. Assets > Create > Material. Select it, set Shader to `EscapeThe90s/PaletteSprite`, and drag `palette.png` into **Palette**.
2. Assets > Create > Escape the 90s > World Art. In the Inspector: **Sprite Material** = that material, **Ground Sheet** =
   `ground_dualgrid_index.png`, **Buildings** size 2: element 0 type `Generic` with `generic_building_index` in Variants,
   element 1 type `PizzaParlor` with `pizza_parlor_index`.
3. If the top menu is missing entirely, the Editor assembly did not compile: check the Console for red errors and send them to me.

---

# Phase 4: streaming, colliders, camera, showcase

## Fastest way to see it (about a minute)
1. Menu **Escape the 90s > Create Showcase Scene Objects**. It reuses a `StreetView` already in the scene (or makes one),
   adds a `Demo Player`, and sets up the main camera at 640x360 pixel scale with a follow script.
2. Set the Game view to a **fixed resolution of 640 x 360** (Game tab > aspect dropdown > + > Fixed Resolution).
3. Press Play. **A/D or arrows** move, **Space** jumps. Works with the old and the new Input System.
   Watch the Hierarchy: `Chunk N` objects appear and disappear around the player; curbs are solid.

Everything in `Demo/` (DemoWalker, CameraFollow, ShowcaseSetup) is throwaway. Delete the folder when the team's real player exists;
the generator does not depend on it. To wire the real player: assign its Transform to `StreetView.follow`, and give your
camera the same clamp logic as `CameraFollow` (camera y = `StreetView.CameraY(orthographicSize)`).

## What changed
- `StreetView` streams: with `follow` set it keeps chunks within `loadRadiusChunks` (default 2) and unloads one chunk later than
  it loads, so crossing a border never flickers. With `follow` empty it builds the whole street (good for Scene-view inspection).
- Colliders come from the data grid (`TerrainColliders`): one deep slab plus one box per curb. A check proves the boxes cover
  exactly the filled terrain cells. Street ends are temporary walls.
- Camera: orthographic size 11.25 (= 360 / 2 / 16); ground surface sits 48 px above the bottom, matching the 3 visible ground rows.
- New checks: chunk-window rules (clamping, no load/unload overlap) and collider/terrain agreement. Output hash is unchanged.

## If something looks wrong
- Magenta or invisible ground/buildings: the material has no shader or palette. Re-run Create World Art From Sprites, check the Console.
- Player falls through the world: look for `BoxCollider2D` components on the `Chunk N` objects; if absent, the Physics 2D module
  may be disabled (Package Manager > Built-in > Physics 2D).
- Player jitters at curbs: tell me the curb height and speed; a tweak to collider shape is easy.
- Verified here: logic and tests, and that all scripts compile against stubs for both Input System paths. Not verified:
  anything running inside Unity.

---

# Milestone 2, step 1: the region layer (Phase 6)

## See it
Menu **Escape the 90s > Region Map** opens a window that draws the regions for a seed (drag a World Gen Settings asset in to use its
seed, or type one). Pastel colour = region, darker nodes = borders, white dot = site, red dot = origin (node 0,0), green = surface
street links between neighbouring regions, cyan = subway stations. `region_map_preview.png` is the same renderer's output
(`RegionMapRaster` is shared, so the image is exactly what the window shows).

## What it is
- **Street grid.** Every intersection is a node with integer coordinates; the origin is node (0,0). One coarse cell is
  `RegionSizeNodes` x `RegionSizeNodes` (12 x 12) nodes.
- **Sites.** Each coarse cell has one site, hashed from (seed, cell) into the middle half of the cell. That jitter bound guarantees a
  node's nearest site is always within the 3x3 cells around it, so any region can be computed alone, in any order. (A check compares
  against a 5x5 brute force.)
- **Regions.** A node belongs to the region of its nearest site (integer maths, ties broken by cell order). The region id is the
  coarse cell that owns the site. Regions come out about 144 nodes each (60-260 allowed by the checks) with 4-8 neighbours.
- **Borders.** A border edge is a street-grid edge joining two nodes of different regions. A's edges into B are exactly B's edges into A,
  reversed (checked).
- **Links.** Each adjacent pair is decided from the pair's own hash: 0/1/2/3 surface links (weights 25/40/25/10), at least
  `MinConnectionSpacing` (3) nodes apart. If none is rolled, a subway link is forced with stations at the ends of a border edge.
  So every adjacent pair is connected, and the whole infinite world is one connected graph (the window check does a BFS).
- Pinned hash `GoldenRegionHash` guards the layout across platforms, like the street hash.

## Tuning
All in `Constants.cs`: `RegionSizeNodes`, `MinConnectionSpacing`, `LinkCountWeights`. Changing them reshuffles every seed.
Observed over 3 seeds: about 24% of pairs are subway-only, 3 surface links are rarer than the weights suggest (about 5%) because short
borders cannot fit three links 3 nodes apart.

## Not done yet
Street grids inside regions (Phase 7), turning a node-to-node edge into a street (length, id, request), intersections and walking from one
street to the next (Phase 8), and biome from nearest arcade (Phase 9).

---

# Milestone 2, step 2: street networks inside regions (Phase 7)

The Region Map window now has a **Show streets** toggle (on by default). `region_streets_preview.png` is a close-up from the same renderer.

## How a region's streets are made (`RegionStreetBuilder`)
1. **Terminals** are the nodes that must be on a street: every surface-link node leaving the region, every subway station inside it, and
   2-5 **arcade nodes** (interior nodes at least 3 apart; an arcade will stand on a street at one of them).
2. **Connect them** Prim-style: repeatedly find the cheapest path (Dijkstra) from the growing network to the nearest unconnected
   terminal and add it. Edge cost is 10 + a hashed 0-6 (so paths wiggle instead of making straight L shapes) + 12 if the edge touches the
   region border (so streets favour the middle). Connectivity is therefore true by construction, not repaired afterwards.
3. **Loops and dense blocks**: two sweeps over the unused grid edges, closest to an arcade first. Add chance is 80% within 2 nodes of an arcade,
   45% within 4, 22% farther, halved on the border. Dead-end spurs are only allowed inside a dense block.
4. Result: `RegionMap.GetStreets(regionId)` gives `Nodes`, `Edges` (internal edges plus the link edges that cross borders), `ArcadeNodes`,
   `LinkNodes`, `SubwayNodes`. A street's id is `StableId.ForStreet(a, b)`, identical from both sides, so a link street is the same street
   in both regions' lists (checked).

Everything depends only on (seed, region id), so any region can be built first, in any order (checked, and pinned with `GoldenStreetsHash`).

## What the checks prove (every region in a 7x7 window, 3 seeds)
Nothing unreachable; 2-5 arcades, none on the border, none closer than 3 nodes; every terminal is on a street; edges are canonical, between
adjacent nodes, internal edges stay inside the region and link edges cross exactly one border and appear in both regions; each network is
connected; border nodes are much less likely to carry a street than interior ones; dead ends are at most 15% of nodes; query order does not matter.

## Typical numbers
About 58% of a region's nodes carry a street, ~116 internal streets, ~33 loops, ~7 dead ends (9%), 3.6 arcades per region.

## Tuning
Constants are at the top of `RegionStreetBuilder` (cost numbers, percentages, arcade count) and in `Constants.cs` for the region size.
Changing them reshuffles every seed and the pinned hashes.

## Not done yet
Turning a node-to-node edge into an actual street (length in chunks, StreetRequest, which streets get the arcade/pizza/tower/bank,
gates), walking from one street to the next at intersections (Phase 8), and biome from the nearest arcade (Phase 9).
