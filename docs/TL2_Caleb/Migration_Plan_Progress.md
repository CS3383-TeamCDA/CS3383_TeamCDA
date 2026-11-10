# TL2 Migration Plan — Progress

**Updated:** 2026-11-06  
**Project:** Escape the 90s, generated-world system

## Implemented so far

- **M0–M3:** Preserved the baseline determinism checks, organized named test fixtures, moved diagnostics out of Core, and added layer namespaces and assemblies.
- **M4–M5:** Consolidated the world query entry point and split region, link, and building generation into smaller stages while preserving generation order and pinned hashes.
- **M6:** Added chunk kinds and terrain stamps, chunk generation metadata, pooling logic, difficulty progression, and immutable chunk lifecycle events under shared Contracts.
- **M7:** Added Unity chunk types, terrain rendering/colliders, pooling, player-centered streaming, and chunk load/recycle events.
- **M8:** Split demo bootstrap, player setup, endpoint-specific route state, route UI, camera, map overlay, and chunk-driven enemy/pickup loading. Cross-team reflection now lives in the WorldGen Integration assembly, which acts as the Composition boundary. Team-owned source files were not changed.
- **D8:** Team agreed that shared APIs belong in `Assets/src/Contracts` with its own asmdef. The relevant Contracts assembly and references are in place.

The user confirmed the current project compiles and the playable demo works in Play Mode.

## M9 review and remaining plan work

- **M9 implementation/review:** Shared injection helpers now live in `Assets/src/Infrastructure/Unity`; the typed event bus is in `Assets/src/Infrastructure/Messaging`; game-specific service construction and the TL1 state adapter live in `Assets/src/Composition`. `IEventBus` is the new frontend for lifecycle and player messages; deprecated publisher interfaces remain adapted for existing consumers. Camera and player-spawn notifier use the general bus; WorldGen uses `IGameplaySessionState` and `DependencyScope`, not TL1 or Composition. The session-state adapter is in Composition, leaving TL1's `GameStateManager` unchanged. Removed `Assets/_Archive`, archive-only tests, six unreferenced assets from the retired tile system, and serialized components that referenced deleted archive scripts. Current usage notes are in `Assets/src/Infrastructure/README.md`; module/dependency trees and responsibility notes are in `M9_Dependency_Review.md`. Unity compile and Play Mode confirmation remain.
- Confirm the open plan decisions around plaza-style intersections, player capability APIs, TL3/TL4 event adoption versus adapters, and TL5 save ownership.
- Keep remaining acceptance work focused on the complete playable game and its end-to-end run loop; “demo” assembly names are historical.
- Complete any remaining planned stress runs and acceptance evidence after the team confirms the required scope.
