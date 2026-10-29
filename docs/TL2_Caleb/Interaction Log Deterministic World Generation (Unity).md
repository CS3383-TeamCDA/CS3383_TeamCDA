# Interaction Log: Deterministic World Generation (Unity)

Project TL2 · Log created October 29, 2026 · Covers the full conversation to date

## Summary

Starting from a world-generation design document (2D action-shooter, infinite chunk-based, deterministic), this conversation covered how to implement it in Unity with C# OOP, the math behind it (hashing, RNG, noise), a build order, automated tests, and the first hands-on steps: writing `Hash` and `Rng` and getting their tests running in Unity's Test Runner.

**Current status:** `Hash` and `Rng` are written and reviewed. Test files for them exist. The tests currently appear under Play Mode instead of Edit Mode, and that is the open issue.

## Timeline

1. **Implementation overview.** Asked what to know for Unity (C# OOP) and mathematically. Answer: generation as a pure function `Chunk = f(seed, chunkX, chunkY)`, custom integer hash and RNG (never `UnityEngine.Random`, `string.GetHashCode()` or shared RNG state), salts per system, integer-only decisions with noise for shape only, three patterns for cross-chunk features (region grid with jitter, column functions for shafts, shared edge hashing for doors), data-driven biomes, a pass-based architecture separating data from presentation, a static vs. dynamic layer split for progression, delta-only saves, and a determinism testing plan.
2. **Hashing a chunk and automated tests.** Explained chunk hashing (streaming hash over tile IDs, dimensions included, static and dynamic layers hashed separately). Delivered six files: `Contracts`, `ChunkHasher`, `TraversalAnalyzer`, `TestHarness`, `DeterminismTests`, `TraversabilityTests`. Not compiled in the sandbox.
3. **Build order.** Delivered a phased build order, then a second version with a short explanation of each step.
4. **Region size vs. chunk size.** Chunks are the unit of loading; regions are the unit of feature placement and must be a whole number of chunks. Starting point: 4×4 chunks per region for rooms, padded placement, one candidate feature per region.
5. **Ordering bug found.** Noticed the tests would have nothing to run on before the generator existed. Fixed Phase 1: Hash and Rng, then data types, then a placeholder generator that uses the hash, then the hasher and tests.
6. **Test harness explained** (twice, simpler the second time, no code at request). It is the one settings file that tells generic tests how to build your generator and read your tile flags.
7. **Teammate exchange.** Drafted a plain-language reply. Clarified that jumping exists but is not relied on, ground may vary, and infinite horizontal traversal is what matters. Revised the reply and added a zero-jump traversal rule and a horizontal-spanning test.
8. **Hash and Rng skeletons and test plan.** Gave signatures and a test list. Flagged a bug in the first snippet: the golden-ratio constant was added twice.
9. **Struct vs. class.** Decision: `Rng` is a struct, created fresh per use from `Hash.Combine`, passed with `ref`.
10. **Floats.** `Value()` was explained, then dropped in favor of integer-only randomness (integer chance as numerator/denominator, integer weights, fixed-point positions).
11. **Rng implemented.** Provided `NextU64` and `Range`, then explained the golden-ratio increment, what a pure scrambler is, what `Combine` does, and where each constant comes from.
12. **Review of the user's Hash and Rng.** Both correct. Fix: wrap `Combine` in `unchecked`. Note: `Mix(0)` is `0`, which is harmless.
13. **Hash and Rng tests written.** Delivered `HashTests`, `RngTests` and `StatHelpers`. Known vectors and statistical tolerances were verified with a Python port.
14. **Running tests in Unity.** Covered asmdef setup, references, namespaces, and Edit Mode vs. Play Mode troubleshooting.

## Key decisions

- Generation is a pure function of seed, chunk coordinates and (for the dynamic layer only) progression tier.
- Terrain and structures never depend on progression tier. Spawns and loot do.
- `Rng` is a struct. All world-affecting randomness is integer-only. Floats are for cosmetics only.
- `Hash.Combine` mixes the seed on its own first, so seed and salt do not commute.
- Each system gets its own salt, and salts are never renumbered after release.
- Gate barriers are an overlay or delta, not part of the static tile array.
- Region size is a whole number of chunks. Starting value: 4×4 chunks for rooms.
- Traversal tests assume walking and elevators only. Jumping may only add shortcuts.
- Saves store deltas only (seed, tier, bosses defeated, per-chunk edits).

## Code delivered

- **User-written:** `Hash` (in assembly `TL2_Hashing`) and `Rng` (in assembly `TL2_Rng`).
- **Runtime support files** (not yet in the project): `Contracts.cs`, `ChunkHasher.cs`, `TraversalAnalyzer.cs`.
- **Tests in use now:** `HashTests.cs`, `RngTests.cs`, `StatHelpers.cs`.
- **Tests waiting for a generator:** `TestHarness.cs`, `DeterminismTests.cs`, `TraversabilityTests.cs`.

## Corrections and caveats

- The first `Rng`/`Mix` snippet added the golden-ratio constant twice. Fixed so `Mix` is a pure scrambler and `Rng` adds the constant once.
- The first `Combine` did `Mix(seed ^ salt)`, which made seed and salt interchangeable. Fixed by mixing the seed first.
- Claim that nearby seeds give shifted copies of the same stream was overstated. It holds only for seeds differing by a multiple of the golden-ratio constant. Using `Combine` is still the right habit.
- The phase 1 order originally put tests before the generator. Corrected.
- The traversal and determinism files have not been compiled, so expect small fixes on first import.

## Verified results

- `Mix` of 1, 2 and 3 times the golden-ratio constant gives `E220A8397B1DCDAF`, `6E789E6AA1B965F4`, `06C45D188009454F`, and `new Rng(0)` yields the same three in order.
- Mix avalanche averaged about 31.98 bits, and neighboring-chunk XOR averaged about 32.0 bits in both directions.

## Open issue

**Tests show under Play Mode, not Edit Mode.** The `TL2_Tests` asmdef looks correct (Editor-only, NUnit override, `UNITY_INCLUDE_TESTS`, Test Runner references). Suspected causes, in order:

1. The reference `TL2_RNG` in the test asmdef must match the Rng assembly's `name` exactly, and earlier it was described as `TL2_Rng`. A mismatch breaks the assembly build.
2. The test scripts may be compiling into a different assembly. Check Assembly Information on `HashTests.cs`.
3. A duplicate copy of the tests may exist in an older Play Mode test assembly.
4. Stale Test Runner state. Reimport the asmdef and reopen the window, or clear `Library/ScriptAssemblies`.

## Pending decisions

- Movement model: can the player step or hop over 2-high cover (`MaxStep`)?
- Chunk size and axis convention (for example 32×32, Y up).
- A world-gen version constant for save compatibility.
- Namespace for the project's own code (the test files still say `WorldGen.Tests` and should be renamed).

## Next steps

1. Resolve the Edit Mode issue (check the `TL2_Rng` name match first).
2. Apply the `unchecked` fix to `Combine`.
3. Run `HashTests` and `RngTests` until green.
4. Move on to Phase 1: data types, a placeholder generator that uses `Hash` and `Rng`, then `ChunkHasher` and the determinism tests.
